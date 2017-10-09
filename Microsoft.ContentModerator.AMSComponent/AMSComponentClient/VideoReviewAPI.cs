using Microsoft.ContentModerator.ReviewAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public VideoReviewApi(AmsConfigurations config, string confidence)
        {
            _amsConfig = config;
            framegenerator = new FrameGenerator(_amsConfig, confidence);
        }

        #region Create and Submit Frames

        public async Task<string> CreateVideoReviewInContentModerator(UploadAssetResult uploadAssetResult)
        {
            string reviewId = string.Empty;
            List<FrameEventDetails> frameEntityList = framegenerator.CreateVideoFrames(uploadAssetResult);
            string path = this._amsConfig.FfmpegFramesOutputPath + Path.GetFileNameWithoutExtension(uploadAssetResult.VideoName) + "_aud_SpReco.vtt";
            TranscriptScreenTextResult screenTextResult = new TranscriptScreenTextResult();
            if (File.Exists(path))
            {
                screenTextResult = await GenerateTextScreenProfanity(reviewId, path);
                uploadAssetResult.RacyScore = screenTextResult.RacyScore;
                uploadAssetResult.OffensiveScore = screenTextResult.OffensiveScore;
                uploadAssetResult.AdultScore = screenTextResult.AdultScore;
                uploadAssetResult.RacyTag = screenTextResult.RacyTag;
                uploadAssetResult.OffensiveTag = screenTextResult.OffensiveTag;
                uploadAssetResult.AdultTag = screenTextResult.AdultTag;
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

        public async Task<string> CreateAndPublishReviewInContentModerator(UploadAssetResult assetinfo, List<FrameEventDetails> frameEntityList, string reviewId, string path, TranscriptScreenTextResult screenTextResult)
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
            string path = this._amsConfig.FfmpegFramesOutputPath + reviewId;
            Directory.Delete(path, true);
            Directory.Delete($"{path}_zip", true);
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
        private async Task<TranscriptScreenTextResult> GenerateTextScreenProfanity(string reviewId, string filepath)
        {
            var textScreenResult = await TextScreen(filepath);
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
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add(Constants.SubscriptionKey, this._amsConfig.ReviewApiSubscriptionKey);

                var uri = string.Format(this._amsConfig.TextModerationResultUrl, this._amsConfig.TeamName,
                    reviewId); // + queryString;

                HttpResponseMessage response;
                byte[] byteData = Encoding.UTF8.GetBytes(profanity);

                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PutAsync(uri, content);
                    resultJson = await response.Content.ReadAsStringAsync();
                }
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
            string resultJson = string.Empty;
            var client = new HttpClient();

            try
            {
                client.DefaultRequestHeaders.Add(Constants.SubscriptionKey, this._amsConfig.ReviewApiSubscriptionKey);

                var uri = string.Format(this._amsConfig.ReviewCreationUrl, this._amsConfig.TeamName);// + queryString;

                HttpResponseMessage response;
                byte[] byteData = Encoding.UTF8.GetBytes(reviewVideoObj);

                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PostAsync(uri, content);
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception("ExecuteCreateReviewApi is failed to get a review");
                    }
                    resultJson = await response.Content.ReadAsStringAsync();
                }
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
        private List<List<FrameEventDetails>> FetchFrameEvents(List<FrameEventDetails> frameEventList, int batchSize)
        {
            List<List<FrameEventDetails>> batchFrames = new List<List<FrameEventDetails>>();
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
        public string CreateReviewRequestObject(UploadAssetResult assetInfo, List<FrameEventDetails> frameEvents)
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
            catch (Exception)
            {
                Console.WriteLine("An exception had occured at {0} ", MethodBase.GetCurrentMethod().Name);
                throw;
            }
        }

        /// <summary>
        /// Geneartes Video Frames
        /// </summary>
        /// <param name="frameEvents">List of FrameEventBlobEntity</param>
        /// <returns>List of Video frames</returns>
        private List<VideoFrame> GenerateVideoFrames(List<FrameEventDetails> frameEvents, UploadAssetResult uploadResult)
        {
            List<VideoFrame> videoFrames = new List<VideoFrame>();
            foreach (FrameEventDetails frameEvent in frameEvents)
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
        private List<Metadata> GenerateMetadata(List<FrameEventDetails> frameEvents, UploadAssetResult uploadResult)
        {
            List<Metadata> metadata = new List<Metadata>();
            FrameEventDetails adultScore = frameEvents.OrderByDescending(a => Double.Parse(a.AdultConfidence)).FirstOrDefault();
            if (uploadResult.V2JSONPath != null)
            {
                FrameEventDetails racyScore = frameEvents.OrderByDescending(a => Double.Parse(a.RacyConfidence)).FirstOrDefault();
                metadata = new List<Metadata>()
                {
                    new Metadata() {Key = "adultScore", Value = adultScore.AdultConfidence},
                    new Metadata() {Key = "a", Value = frameEvents.Any(x=>x.IsAdultContent.Equals(true)).ToString()},
                    new Metadata() {Key = "racyScore", Value = racyScore.RacyConfidence},
                    new Metadata() {Key = "r", Value = frameEvents.Any(x=>x.IsRacyContent.Equals(true)).ToString()}
                };
            }
            else
            {
                metadata = new List<Metadata>()
                {
                    new Metadata() { Key= "adultScore",Value=adultScore.AdultConfidence},
                    new Metadata() {Key="a",Value=frameEvents.Any(x=>x.IsAdultContent.Equals(true)).ToString() },
                };
            }
            if (uploadResult.GenerateVTT)
            {
                metadata.AddRange(new List<Metadata>()
                {
                    new Metadata() { Key = "adultTextScore", Value = uploadResult.AdultScore.ToString() },
                    new Metadata() { Key = "at", Value = uploadResult.AdultTag.ToString() },
                    new Metadata() { Key = "racyTextScore", Value = uploadResult.RacyScore.ToString() },
                    new Metadata() { Key = "rt", Value = uploadResult.RacyTag.ToString() },
                    new Metadata() { Key = "offensiveTextScore", Value = uploadResult.OffensiveScore.ToString() },
                    new Metadata() { Key = "ot", Value = uploadResult.OffensiveTag.ToString() }
                });
            }
            return metadata;
        }

        /// <summary>
        /// Populates a Video frame object
        /// </summary>
        /// <param name="frameEvent">FrameEventBlobEntity</param>
        /// <returns>Video Frame Object</returns>
        private VideoFrame PopulateVideoFrame(FrameEventDetails frameEvent, UploadAssetResult uploadResult)
        {
            if (uploadResult.V2JSONPath != null)
            {
                VideoFrame frameobj = new VideoFrame()
                {
                    FrameImage = frameEvent.FrameName,
                    Timestamp = Convert.ToString(frameEvent.TimeStamp * 1000 / frameEvent.TimeScale),
                    ReviewerResultTags = new List<ReviewResultTag>(),
                    Metadata = new List<Metadata>()
                    {
                        new Metadata() {Key = "adultScore", Value = frameEvent.AdultConfidence},
                        new Metadata() {Key = "a", Value = frameEvent.IsAdultContent.ToString()},
                        new Metadata() {Key = "racyScore", Value = frameEvent.RacyConfidence},
                        new Metadata() {Key = "r", Value = frameEvent.IsRacyContent.ToString()},
                        new Metadata() {Key = "ExternalId", Value = frameEvent.FrameName}
                    },
                };
                return frameobj;
            }
            else
            {
                VideoFrame frameobj = new VideoFrame()
                {
                    FrameImage = frameEvent.FrameName,
                    Timestamp = Convert.ToString(frameEvent.TimeStamp),
                    ReviewerResultTags = new List<ReviewResultTag>(),
                    Metadata = new List<Metadata>()
                    {
                        new Metadata() {Key = "adultScore", Value = frameEvent.AdultConfidence},
                        new Metadata() {Key = "A", Value = frameEvent.IsAdultContent.ToString()},
                        new Metadata() {Key = "ExternalId", Value = frameEvent.FrameName}
                    },
                };
                return frameobj;
            }
        }

        #endregion

        #region AddFrames

        /// <summary>
        ///  Add frames and publishes it.
        /// </summary>
        /// <param name="frameEvents">List of Frame Events.</param>
        /// <param name="reviewId">Reviewid</param>
        /// <returns>Indicates Add frames operation success result.</returns>
        public async Task<bool> SubmitAddFramesReview(List<FrameEventDetails> frameEvents, string reviewId, UploadAssetResult uploadResult)
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
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            var uri = string.Format(this._amsConfig.AddFramesUrl, this._amsConfig.TeamName, reviewId);
            // string resultJson = string.Empty;
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this._amsConfig.ReviewApiSubscriptionKey);

            MultipartFormDataContent form = new MultipartFormDataContent();
            form.Add(new StringContent(reviewFrameList), "FrameMetadata");
            byte[] frameZip = File.ReadAllBytes(frameZipPath);
            var zipContent = new ByteArrayContent(frameZip, 0, frameZip.Length);
            zipContent.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping("frameZip.zip"));
            form.Add(zipContent, "FrameImageZip", "frameZip.zip");
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await client.PostAsync(uri, form);
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
        private string CreateAddFramesReviewRequestObject(List<FrameEventDetails> frameEvents, UploadAssetResult uploadResult)
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

            var client = new HttpClient();

            try
            {
                client.DefaultRequestHeaders.Add(Constants.SubscriptionKey, this._amsConfig.ReviewApiSubscriptionKey);

                var uri = string.Format(this._amsConfig.AddTranscriptUrl, this._amsConfig.TeamName, reviewId);

                HttpResponseMessage response;
                byte[] byteData = File.ReadAllBytes(filepath);


                using (var content = new ByteArrayContent(byteData))
                {
                    //  content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                    response = await client.PutAsync(uri, content);
                    resultJson = await response.Content.ReadAsStringAsync();
                }

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
        private async Task<TranscriptScreenTextResult> TextScreen(string filepath)
        {
            List<TranscriptProfanity> profanityList = new List<TranscriptProfanity>();
            string responseContent = string.Empty;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add(Constants.SubscriptionKey, _amsConfig.ReviewApiSubscriptionKey);
            var uri = string.Format(this._amsConfig.TranscriptModerationUrl);
            HttpResponseMessage response;
            bool racyTag = false;
            bool adultTag = false;
            bool offensiveTag = false;
            double racyScore = 0;
            double adultScore = 0;
            double offensiveScore = 0;
            List<string> vttLines = File.ReadAllLines(filepath).Where(line => !line.Contains("-->") && line.Any(char.IsLetter)).ToList();
            List<string> vttList = new List<string>();
            StringBuilder sb = new StringBuilder();
            foreach (var line in vttLines)
            {
                if (sb.Length + line.Length > 1024)
                {
                    vttList.Add(sb.ToString());
                    sb.Clear();
                }
                sb.Append(line);
            }
            if (sb.Length > 0)
            {
                vttList.Add(sb.ToString());
            }
            foreach (var vtt in vttList)
            {
                byte[] byteData = Encoding.UTF8.GetBytes(vtt);
                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                    response = await client.PostAsync(uri, content);
                    responseContent = await response.Content.ReadAsStringAsync();
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
                    if (jsonTextScreen.Classification.AdultScore > _amsConfig.AdultTextThreshold) adultTag = true;
                    if (jsonTextScreen.Classification.RacyScore > _amsConfig.RacyTextThreshold) racyTag = true;
                    if (jsonTextScreen.Classification.OffensiveScore > _amsConfig.OffensiveTextThreshold) offensiveTag = true;
                    offensiveScore = jsonTextScreen.Classification.OffensiveScore > offensiveScore ? jsonTextScreen.Classification.OffensiveScore : offensiveScore;
                    adultScore = jsonTextScreen.Classification.AdultScore > adultScore ? jsonTextScreen.Classification.AdultScore : adultScore;
                    racyScore = jsonTextScreen.Classification.RacyScore > racyScore ? jsonTextScreen.Classification.RacyScore : racyScore;
                }
            }
            TranscriptScreenTextResult screenTextResult = new TranscriptScreenTextResult()
            {
                AdultTag = adultTag,
                OffensiveTag = offensiveTag,
                TranscriptProfanity = profanityList,
                RacyTag = racyTag,
                RacyScore = racyScore,
                OffensiveScore = offensiveScore,
                AdultScore = adultScore
            };
            return screenTextResult;
        }

        public static IEnumerable<string> splitVtt(string input, int characterCount)
        {
            int index = 0;
            return from c in input
                   let itemIndex = index++
                   group c by itemIndex / characterCount
                into g
                   select new string(g.ToArray());
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

            var client = new HttpClient();
            var resultJson = string.Empty;
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this._amsConfig.ReviewApiSubscriptionKey);
                var uri = string.Format(this._amsConfig.PublishReviewUrl, this._amsConfig.TeamName, reviewId);

                var method = new HttpMethod("PATCH");
                var content = new StringContent("");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var request = new HttpRequestMessage(method, uri)
                {
                    Content = content
                };
                response = await client.SendAsync(request);
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