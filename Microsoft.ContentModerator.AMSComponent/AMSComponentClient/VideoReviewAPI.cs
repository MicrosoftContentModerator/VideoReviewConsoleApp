using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.CognitiveServices.ContentModerator;
using Microsoft.CognitiveServices.ContentModerator.Models;
using Microsoft.ContentModerator.ReviewAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.ContentModerator.AMSComponentClient
{

    /// <summary>
    /// Represents a Video Review object.
    /// </summary>
    public class VideoReviewApi
    {
        private AmsConfigurations _amsConfig;
        private FrameGenerator framegenerator;
        /// <summary>
        /// Instantiates an instance of VideoReviewAPI
        /// </summary>
        /// <param name="config">AMSConfigurations</param>
        public VideoReviewApi(AmsConfigurations config)
        {
            _amsConfig = config;
            framegenerator = new FrameGenerator(_amsConfig);
        }
        private readonly ContentModeratorClient CMClient = new ContentModeratorClient(new ApiKeyServiceClientCredentials(AmsConfigurations.ReviewApiSubscriptionKey)) { BaseUrl = AmsConfigurations.ContentModeraotrApiEndpoint };

        #region Create and Submit Frames

        public async Task<string> CreateVideoReviewInContentModerator(UploadAssetResult uploadAssetResult)
        {
            string reviewId = string.Empty;
            List<ProcessedFrameDetails> frameEntityList = framegenerator.CreateVideoFrames(uploadAssetResult);
            string path = uploadAssetResult.GenerateVTT == true ? this._amsConfig.FfmpegFramesOutputPath + Path.GetFileNameWithoutExtension(uploadAssetResult.VideoName) + "_aud_SpReco.vtt" : "";
            TranscriptScreenTextResult screenTextResult = new TranscriptScreenTextResult();
            if (File.Exists(path))
            {
                screenTextResult = await GenerateTextScreenProfanity(reviewId, path, frameEntityList);
                uploadAssetResult.Category1TextScore = screenTextResult.Category1Score;
                uploadAssetResult.Category2TextScore = screenTextResult.Category2Score;
                uploadAssetResult.Category3TextScore = screenTextResult.Category3Score;
                uploadAssetResult.Category1TextTag = screenTextResult.Category1Tag;
                uploadAssetResult.Category2TextTag = screenTextResult.Category2Tag;
                uploadAssetResult.Category3TextTag = screenTextResult.Category3Tag;
            }
            var reviewVideoRequestJson = CreateReviewRequestObject(uploadAssetResult, frameEntityList);
            if (string.IsNullOrWhiteSpace(reviewVideoRequestJson))
            {
                throw new Exception("Video review process failed in CreateVideoReviewInContentModerator");
            }
            reviewId = JsonConvert.DeserializeObject<List<string>>(ExecuteCreateReviewApi(reviewVideoRequestJson).Result).FirstOrDefault();
            frameEntityList = framegenerator.GenerateFrameImages(frameEntityList, uploadAssetResult, reviewId);
            await CreateAndPublishReviewInContentModerator(uploadAssetResult, frameEntityList, reviewId, path, screenTextResult);

            return reviewId;
        }

        public async Task<string> CreateAndPublishReviewInContentModerator(UploadAssetResult assetinfo, List<ProcessedFrameDetails> frameEntityList, string reviewId, string path, TranscriptScreenTextResult screenTextResult)
        {
            bool isSuccess = false;
            bool isTranscript = false;
            isSuccess = await SubmitAddFramesReview(frameEntityList, reviewId, assetinfo);
            if (!isSuccess)
            {
                Console.WriteLine("Add Frame API call failed.");
                throw new Exception();
            }

            if (File.Exists(path))
            {
                if (ValidateVtt(path))
                {
                    isTranscript = await SubmitTranscript(reviewId, path);
                }
                if (isTranscript)
                {
                    isSuccess = await UploadScreenTextResult(reviewId, JsonConvert.SerializeObject(screenTextResult?.TranscriptProfanity));
                    if (!isSuccess)
                    {
                        Console.WriteLine("ScreenTextResult API call failed.");
                        throw new Exception();
                    }
                }
                else
                {
                    Console.WriteLine("Upload vtt failed.");
                    throw new Exception();
                }
                try
                {
                    File.Delete(path);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            isSuccess = PublishReview(reviewId);
            if (!isSuccess)
            {
                throw new Exception("Publish review failed.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("\nReview Created Successfully and the review Id {0}", reviewId);
            }
            CleanUp(reviewId);
            return reviewId;
        }
        private void CleanUp(string reviewId)
        {
            try
            {
                string path = this._amsConfig.FfmpegFramesOutputPath + reviewId;
                Directory.Delete(path, true);
                Directory.Delete($"{path}_zip", true);
            }
            catch (Exception)
            {
                Console.WriteLine("Cleanup failed.");
            }
        }

        /// <summary>
        /// Validate vtt file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool ValidateVtt(string path)
        {
            string filepath = File.ReadAllText(path);
            double errorCount = VttValidator.ValidateVTT(filepath);
            return !(errorCount > 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reviewId"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private async Task<TranscriptScreenTextResult> GenerateTextScreenProfanity(string reviewId, string filepath, List<ProcessedFrameDetails> frameEntityList)
        {
            var textScreenResult = await TextScreen(filepath, frameEntityList);
            return textScreenResult;
        }
        private async Task<bool> UploadScreenTextResult(string reviewId, string transcriptProfanity)
        {
            HttpResponseMessage response;
            int retry = 3;
            bool isComplete = false;
            while (!isComplete && retry > 0)
            {
                response = await ExecuteAddTranscriptSupportFile(reviewId, transcriptProfanity);
                isComplete = response.IsSuccessStatusCode;
                retry--;
            }
            return isComplete;
        }

        /// <summary>
        /// Post transcript support file to the review API
        /// </summary>
        /// <param name="reviewId"></param>
        /// <param name="profanity"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> ExecuteAddTranscriptSupportFile(string reviewId, string profanity)
        {
            string resultJson = string.Empty;
            try
            {
                HttpResponseMessage response;
                List<TranscriptModerationBodyItem> modResult = JsonConvert.DeserializeObject<List<TranscriptModerationBodyItem>>(profanity);
                var res = await CMClient.Reviews.AddVideoTranscriptModerationResultWithHttpMessagesAsync("application/json", this._amsConfig.TeamName, reviewId, modResult);
                response = res.Response;
                resultJson = await response.Content.ReadAsStringAsync();
                return response;
            }
            catch (Exception e)
            {
                //TODO Logging
                Console.WriteLine("An exception had occured at {0} API, for the Review ID : {1}",
                    MethodBase.GetCurrentMethod().Name, reviewId);
                Console.WriteLine("The response from the Api is : \n {0}", resultJson);
                Console.WriteLine(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Posts the Review Video object and returns a result.
        /// </summary>
        /// <param name="reviewVideoObj">Reviewvideo requestJson</param>
        /// <returns>Review Id</returns>
        public async Task<string> ExecuteCreateReviewApi(string reviewVideoObj)
        {
            ContentModeratorClient CMClient = new ContentModeratorClient(new ApiKeyServiceClientCredentials(AmsConfigurations.ReviewApiSubscriptionKey)) { BaseUrl = AmsConfigurations.ContentModeraotrApiEndpoint };

            string resultJson = string.Empty;
            try
            {
                List<CreateVideoReviewsBodyItem> review = JsonConvert.DeserializeObject<List<CreateVideoReviewsBodyItem>>(reviewVideoObj);
                HttpResponseMessage response;

                var oResponse = await CMClient.Reviews.CreateVideoReviewsWithHttpMessagesAsync("application/json", _amsConfig.TeamName, review);
                response = oResponse.Response;
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"ExecuteCreateReviewApi has failed to get a review. Code: {response.StatusCode}");
                }
                resultJson = await response.Content.ReadAsStringAsync();

                return resultJson;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

        }

        /// <summary>
        /// Fetch frames from the source frames list.
        /// </summary>
        /// <param name="tempFrameEventsList">List of FrameEventBlobEntity</param>
        /// <param name="batchSize">Batch Size</param>
        private List<List<ProcessedFrameDetails>> FetchFrameEvents(List<ProcessedFrameDetails> frameEventList, int batchSize)
        {
            List<List<ProcessedFrameDetails>> batchFrames = new List<List<ProcessedFrameDetails>>();
            while (frameEventList.Count > 0)
            {
                if (batchSize < frameEventList.Count)
                {
                    batchFrames.Add(frameEventList.Take(batchSize).ToList());
                    frameEventList.RemoveRange(0, batchSize);
                }
                else
                {
                    batchFrames.Add(frameEventList.Take(frameEventList.Count).ToList());
                    frameEventList.Clear();
                }
            }
            return batchFrames;
        }

        /// <summary>
        /// Creates a review video request object.
        /// </summary>
        /// <param name="assetInfo">UploadAssetResult</param>
        /// <param name="frameEvents">List of FrameEventBlobEntity</param>
        /// <returns>Reviewvideo</returns>
        public string CreateReviewRequestObject(UploadAssetResult assetInfo, List<ProcessedFrameDetails> frameEvents)
        {

            List<ReviewVideo> reviewList = new List<ReviewVideo>();
            ReviewVideo reviewVideoObj = new ReviewVideo();
            try
            {
                reviewVideoObj.Type = Constants.VideoEntityType;
                reviewVideoObj.Content = assetInfo.StreamingUrlDetails.SmoothUrl;
                reviewVideoObj.ContentId = assetInfo.VideoName;
                reviewVideoObj.CallbackEndpoint = this._amsConfig.ReviewCallBackUrl;
                reviewVideoObj.Metadata = frameEvents.Count != 0 ? GenerateMetadata(frameEvents, assetInfo) : null;
                reviewVideoObj.Status = Constants.PublishedStatus;
                reviewVideoObj.VideoFrames = null;
                reviewList.Add(reviewVideoObj);
                return JsonConvert.SerializeObject(reviewList);
            }
            catch (Exception e)
            {
                Console.WriteLine("An exception had occured at {0} ", MethodBase.GetCurrentMethod().Name);
                Console.WriteLine(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Geneartes Video Frames
        /// </summary>
        /// <param name="frameEvents">List of FrameEventBlobEntity</param>
        /// <returns>List of Video frames</returns>
        private List<VideoFrame> GenerateVideoFrames(List<ProcessedFrameDetails> frameEvents, UploadAssetResult uploadResult)
        {
            List<VideoFrame> videoFrames = new List<VideoFrame>();
            foreach (ProcessedFrameDetails frameEvent in frameEvents)
            {
                VideoFrame videoFrameObj = PopulateVideoFrame(frameEvent, uploadResult);
                videoFrames.Add(videoFrameObj);
            }
            return videoFrames;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frameEvents"></param>
        /// <returns></returns>
        private List<Metadata> GenerateMetadata(List<ProcessedFrameDetails> frameEvents, UploadAssetResult uploadResult)
        {
            List<Metadata> metadata = new List<Metadata>();
            var adultScore = frameEvents.OrderByDescending(a => Double.Parse(a.AdultScore)).FirstOrDefault().AdultScore;
            var racyScore = frameEvents.OrderByDescending(a => Double.Parse(a.RacyScore)).FirstOrDefault().RacyScore;
            var isAdult = double.Parse(adultScore) > _amsConfig.Category1FrameThreshold ? true : false;
            var isRacy = double.Parse(racyScore) > _amsConfig.Category2FrameThreshold ? true : false;
            var reviewRecommended = frameEvents.Any(frame => frame.ReviewRecommended);
            //if (!isAdult && !isRacy && !reviewRecommended && !uploadResult.AdultTextTag && !uploadResult.RacyTextTag && !uploadResult.OffensiveTextTag)
            //{
            //    using (var sw = new StreamWriter(AmsConfigurations.logFilePath, true))
            //    {
            //        sw.WriteLine($"Review Not Created base on video moderation outputs. Video Name: {0}",uploadResult.VideoName);
            //    }
            //    throw new Exception("The video submitted and the threshold set suggests review not to be created.");
            //}

            metadata = new List<Metadata>()
            {
                new Metadata() {Key = "ReviewRecommended", Value = reviewRecommended.ToString()},
                new Metadata() {Key = "AdultScore", Value = adultScore},
                new Metadata() {Key = "a", Value = isAdult.ToString() },
                new Metadata() {Key = "RacyScore", Value = racyScore},
                new Metadata() {Key = "r", Value = isRacy.ToString() }
            };

            if (uploadResult.GenerateVTT)
            {
                metadata.AddRange(new List<Metadata>()
                {
                    new Metadata() { Key = "AdultTextScore", Value = uploadResult.Category1TextScore.ToString() },
                    new Metadata() { Key = "at", Value = uploadResult.Category1TextTag.ToString() },
                    new Metadata() { Key = "RacyTextScore", Value = uploadResult.Category2TextScore.ToString() },
                    new Metadata() { Key = "rt", Value = uploadResult.Category2TextTag.ToString() },
                    new Metadata() { Key = "OffensiveTextScore", Value = uploadResult.Category3TextScore.ToString() },
                    new Metadata() { Key = "ot", Value = uploadResult.Category3TextTag.ToString() }
                });
            }
            return metadata;
        }

        /// <summary>
        /// Populates a Video frame object
        /// </summary>
        /// <param name="frameEvent">FrameEventBlobEntity</param>
        /// <returns>Video Frame Object</returns>
        private VideoFrame PopulateVideoFrame(ProcessedFrameDetails frameEvent, UploadAssetResult uploadResult)
        {
            VideoFrame frameobj = new VideoFrame()
            {
                FrameImage = frameEvent.FrameName,
                Timestamp = Convert.ToString(frameEvent.TimeStamp),
                ReviewerResultTags = new List<ReviewResultTag>(),
                Metadata = new List<Metadata>()
                    {
                        new Metadata() {Key = "Review Recommended", Value = frameEvent.ReviewRecommended.ToString()},
                        new Metadata() {Key = "Adult Score", Value = frameEvent.AdultScore},
                        new Metadata() {Key = "a", Value = frameEvent.IsAdultContent.ToString()},
                        new Metadata() {Key = "Racy Score", Value = frameEvent.RacyScore},
                        new Metadata() {Key = "r", Value = frameEvent.IsRacyContent.ToString()},
                        new Metadata() {Key = "ExternalId", Value = frameEvent.FrameName},
                        new Metadata() {Key = "at",Value = frameEvent.IsAdultTextContent.ToString() },
                        new Metadata() {Key = "rt",Value = frameEvent.IsRacyTextContent.ToString() },
                        new Metadata() {Key = "ot",Value = frameEvent.IsOffensiveTextContent.ToString() }
                    },
            };
            return frameobj;
        }

        #endregion

        #region AddFrames

        /// <summary>
        ///  Add frames and publishes it.
        /// </summary>
        /// <param name="frameEvents">List of Frame Events.</param>
        /// <param name="reviewId">Reviewid</param>
        /// <returns>Indicates Add frames operation success result.</returns>
        public async Task<bool> SubmitAddFramesReview(List<ProcessedFrameDetails> frameEvents, string reviewId, UploadAssetResult uploadResult)
        {
            bool isSuccess = true;
            int batchSize = _amsConfig.FrameBatchSize;
            var batchFrames = FetchFrameEvents(frameEvents, batchSize);
            List<string> frameRequest = new List<string>();
            foreach (var batchFrame in batchFrames)
            {
                frameRequest.Add(CreateAddFramesReviewRequestObject(batchFrame, uploadResult));
            }
            //string inputRequestObj = CreateAddFramesReviewRequestObject(frameEvents, uploadResult);
            string frameZipPath = $"{this._amsConfig.FfmpegFramesOutputPath}{reviewId}_zip";

            DirectoryInfo di = new DirectoryInfo(frameZipPath);
            FileInfo[] zipFiles = di.GetFiles();
            if (frameRequest.Count != zipFiles.Length)
            {
                Console.WriteLine("Something went wrong.");
                throw new Exception();
            }
            for (int i = 0; i < frameRequest.Count; i++)
            {
                int retry = 3;
                bool isComplete = false;
                HttpResponseMessage response;
                while (!isComplete && retry > 0)
                {
                    response = await ExecuteAddFramesReviewApi(reviewId, frameRequest[i], zipFiles[i].FullName);
                    isComplete = response.IsSuccessStatusCode;
                    retry--;
                    if (retry == 0 && !isComplete)
                        isSuccess = false;
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reviewId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<bool> SubmitTranscript(string reviewId, string path)
        {
            bool isComplete = false;
            int retry = 3;
            HttpResponseMessage response = new HttpResponseMessage();
            while (!isComplete && retry > 0)
            {
                var res = await ExecuteAddTranscriptApi(reviewId, path);
                isComplete = res.IsSuccessStatusCode;
                retry--;
            }
            return isComplete;
        }

        /// <summary>
        /// Posts frames to add it to the video.
        /// </summary>
        /// <param name="reviewId">reviewID</param>
        /// <param name="reviewFrameList">reviewFrameList</param>
        /// <returns>Response of AddFrames Api call</returns>
        private async Task<HttpResponseMessage> ExecuteAddFramesReviewApi(string reviewId, string reviewFrameList, string frameZipPath)
        {
            Stream frameZip = new FileStream(frameZipPath, FileMode.Open, FileAccess.Read);
            var ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping("frameZip.zip"));
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var res = await CMClient.Reviews.AddVideoFrameStreamWithHttpMessagesAsync(ContentType.ToString(), _amsConfig.TeamName, reviewId, frameZip, reviewFrameList);
                response = res.Response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return response;
        }

        /// <summary>
        /// Forms the AddFrames request object.
        /// </summary>
        /// <param name="frameEvents">List of Frame events</param>
        /// <returns>Json representation of video frames collection.</returns>
        private string CreateAddFramesReviewRequestObject(List<ProcessedFrameDetails> frameEvents, UploadAssetResult uploadResult)
        {
            List<VideoFrame> videoFrames = GenerateVideoFrames(frameEvents, uploadResult);
            return JsonConvert.SerializeObject(videoFrames);
        }

        #endregion

        /// <summary>
        /// Posting vtt file to the API
        /// </summary>
        /// <param name="reviewId"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> ExecuteAddTranscriptApi(string reviewId, string filepath)
        {
            string resultJson = string.Empty;
            try
            {
                HttpResponseMessage response;
                byte[] byteData = File.ReadAllBytes(filepath);
                var oRes = await CMClient.Reviews.AddVideoTranscriptWithHttpMessagesAsync(_amsConfig.TeamName, reviewId, new MemoryStream(byteData));
                response = oRes.Response;
                resultJson = await response.Content.ReadAsStringAsync();
                return response;
            }
            catch (Exception e)
            {
                //TODO Logging
                Console.WriteLine("An exception had occured at {0} Api, for the Review ID : {1} and the FilePath is : {2}", MethodBase.GetCurrentMethod().Name, reviewId, filepath);
                Console.WriteLine("The response from the Api : \n {0}", resultJson);
                Console.WriteLine(e.Message);
                throw;
            }

        }

        /// <summary>
        /// Identifying profanity words 
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private async Task<TranscriptScreenTextResult> TextScreen(string filepath, List<ProcessedFrameDetails> frameEntityList)
        {
            List<TranscriptProfanity> profanityList = new List<TranscriptProfanity>();
            string responseContent = string.Empty;
            HttpResponseMessage response;
            bool category1Tag = false;
            bool category2Tag = false;
            bool category3Tag = false;
            double category1Score = 0;
            double category2Score = 0;
            double category3Score = 0;
            List<string> vttLines = File.ReadAllLines(filepath).Where(line => !line.Contains("NOTE Confidence:") && line.Length > 0).ToList();
            StringBuilder sb = new StringBuilder();
            List<CaptionScreentextResult> csrList = new List<CaptionScreentextResult>();
            CaptionScreentextResult captionScreentextResult = new CaptionScreentextResult() { Captions = new List<string>() };
            foreach (var line in vttLines.Skip(1))
            {
                if (line.Contains("-->"))
                {
                    if (sb.Length > 0)
                    {
                        captionScreentextResult.Captions.Add(sb.ToString());
                        sb.Clear();
                    }
                    if (captionScreentextResult.Captions.Count > 0)
                    {
                        csrList.Add(captionScreentextResult);
                        captionScreentextResult = new CaptionScreentextResult() { Captions = new List<string>() };
                    }
                    string[] times = line.Split(new string[] { "-->" }, StringSplitOptions.RemoveEmptyEntries);
                    string startTimeString = times[0].Trim();
                    string endTimeString = times[1].Trim();
                    int startTime = (int)TimeSpan.ParseExact(startTimeString, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture).TotalMilliseconds;
                    int endTime = (int)TimeSpan.ParseExact(endTimeString, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture).TotalMilliseconds;
                    captionScreentextResult.StartTime = startTime;
                    captionScreentextResult.EndTime = endTime;
                }
                else
                {
                    sb.Append(line);
                }
                if (sb.Length + line.Length > 1024)
                {
                    captionScreentextResult.Captions.Add(sb.ToString());
                    sb.Clear();
                }
            }
            if (sb.Length > 0)
            {
                captionScreentextResult.Captions.Add(sb.ToString());
            }
            if (captionScreentextResult.Captions.Count > 0)
            {
                csrList.Add(captionScreentextResult);
            }
            int waitTime = 1000;
            foreach (var csr in csrList)
            {
                bool captionAdultTextTag = false;
                bool captionRacyTextTag = false;
                bool captionOffensiveTextTag = false;
                bool retry = true;

                foreach (var caption in csr.Captions)
                {
                    while (retry)
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(waitTime);
                            var lang = await CMClient.TextModeration.DetectLanguageAsync("text/plain", caption);
                            var oRes = await CMClient.TextModeration.ScreenTextWithHttpMessagesAsync(lang.DetectedLanguageProperty, "text/plain", caption, null, null, null, true);
                            response = oRes.Response;
                            responseContent = await response.Content.ReadAsStringAsync();
                            retry = false;
                        }
                        catch (Exception e)
                        {
                            if (e.Message.Contains("429"))
                            {
                                Console.WriteLine($"Moderation API call failed. Message: {e.Message}");
                                waitTime = (int)(waitTime * 1.5);
                                Console.WriteLine($"wait time: {waitTime}");
                            }
                            else
                            {
                                retry = false;
                                Console.WriteLine($"Moderation API call failed. Message: {e.Message}");
                            }
                        }
                    }
                    var jsonTextScreen = JsonConvert.DeserializeObject<TextScreen>(responseContent);
                    if (jsonTextScreen != null)
                    {
                        TranscriptProfanity transcriptProfanity = new TranscriptProfanity();
                        transcriptProfanity.TimeStamp = "";
                        List<Terms> transcriptTerm = new List<Terms>();
                        if (jsonTextScreen.Terms != null)
                        {
                            foreach (var term in jsonTextScreen.Terms)
                            {
                                var profanityobject = new Terms
                                {
                                    Term = term.Term,
                                    Index = term.Index
                                };
                                transcriptTerm.Add(profanityobject);
                            }
                            transcriptProfanity.Terms = transcriptTerm;
                            profanityList.Add(transcriptProfanity);
                        }
                        if (jsonTextScreen.Classification.Category1.Score > _amsConfig.Category1TextThreshold) captionAdultTextTag = true;
                        if (jsonTextScreen.Classification.Category2.Score > _amsConfig.Category2TextThreshold) captionRacyTextTag = true;
                        if (jsonTextScreen.Classification.Category3.Score > _amsConfig.Category3TextThreshold) captionOffensiveTextTag = true;
                        if (jsonTextScreen.Classification.Category1.Score > _amsConfig.Category1TextThreshold) category1Tag = true;
                        if (jsonTextScreen.Classification.Category2.Score > _amsConfig.Category2TextThreshold) category2Tag = true;
                        if (jsonTextScreen.Classification.Category3.Score > _amsConfig.Category3TextThreshold) category3Tag = true;
                        category1Score = jsonTextScreen.Classification.Category1.Score > category1Score ? jsonTextScreen.Classification.Category1.Score : category1Score;
                        category2Score = jsonTextScreen.Classification.Category2.Score > category2Score ? jsonTextScreen.Classification.Category2.Score : category2Score;
                        category3Score = jsonTextScreen.Classification.Category3.Score > category3Score ? jsonTextScreen.Classification.Category3.Score : category3Score;
                    }
                    foreach (var frame in frameEntityList.Where(x => x.TimeStamp >= csr.StartTime && x.TimeStamp <= csr.EndTime))
                    {
                        frame.IsAdultTextContent = captionAdultTextTag;
                        frame.IsRacyTextContent = captionRacyTextTag;
                        frame.IsOffensiveTextContent = captionOffensiveTextTag;
                    }
                }
            }
            TranscriptScreenTextResult screenTextResult = new TranscriptScreenTextResult()
            {
                TranscriptProfanity = profanityList,
                Category1Tag = category1Tag,
                Category2Tag = category2Tag,
                Category3Tag = category3Tag,
                Category1Score = category1Score,
                Category2Score = category2Score,
                Category3Score = category3Score
            };
            return screenTextResult;
        }

        #region Publish  - Review
        /// <summary>
        /// Publishes a review object.
        /// </summary>
        /// <param name="reviewId">reviewID</param>
        /// <returns>returns publish review api call response status</returns>
        public bool PublishReview(string reviewId)
        {
            bool isComplete = false;
            int retry = 3;
            HttpResponseMessage response = new HttpResponseMessage();
            while (!isComplete && retry > 0)
            {
                isComplete = ExecutePublishReviewApi(reviewId).Result.IsSuccessStatusCode;
                retry--;
            }
            return isComplete;
        }

        /// <summary>
        ///  Posts a request to Publih review api with provieded params.
        /// </summary>
        /// <param name="reviewId">reviewId</param>
        /// <returns>Returns response of publish review.</returns>
        private async Task<HttpResponseMessage> ExecutePublishReviewApi(string reviewId)
        {
            var resultJson = string.Empty;
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                var oRes = await CMClient.Reviews.PublishVideoReviewWithHttpMessagesAsync(_amsConfig.TeamName, reviewId);
                response = oRes.Response;
                return response;
            }
            catch (Exception e)
            {
                //TODO Logging
                Console.WriteLine("An exception had occured at {0} Api, for the Review ID : {1}", MethodBase.GetCurrentMethod().Name, reviewId);
                Console.WriteLine("THE response from the Api is : \n {0}", resultJson);
                Console.WriteLine(e.Message);
                throw;
            }
        }
        #endregion
    }
}