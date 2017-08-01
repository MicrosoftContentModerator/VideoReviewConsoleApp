using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ContentModerator.BusinessEntities;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.ContentModerator.BusinessEntities.Entities;

using System.IO;
using Microsoft.ContentModerator.ReviewAPI;
using Microsoft.ContentModerator.Services;

namespace Microsoft.ContentModerator.RESTUtilityServices
{
    /// <summary>
    /// Represents a Video Review object.
    /// </summary>
    public class VideoReviewApi
    {
        private AmsConfigurations _amsConfig;

        /// <summary>
        /// Instantiates an instance of VideoReviewAPI
        /// </summary>
        /// <param name="config">AMSConfigurations</param>
        public VideoReviewApi(AmsConfigurations config)
        {
            this._amsConfig = config;
        }

        #region Create and Submit Frames

        /// <summary>
        /// Creates and submits a Video Review object.
        /// </summary>
        /// <param name="assetinfo">UploadAssetResult</param>
        /// <param name="frameEntityList">FrameEventBlobEntity</param>
        /// <param name="isPublish">publishing status</param>
        /// <returns>ReviewId</returns>
        public string SubmitCreateReview(UploadAssetResult assetinfo, List<FrameEventDetails> frameEntityList)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n Review API call in progress ");
            bool isFirst = true;
            bool isSuccess = false;
            bool isTranscript = false;
            string reviewVideoRequestJson = string.Empty;
            bool isPublish = false;
            string reviewId = string.Empty;
            List<Metadata> metaData = GenerateMetadata(frameEntityList);

            while (frameEntityList.Count > 0)
            {
                List<FrameEventDetails> tempList = new List<FrameEventDetails>();
                tempList = FetchFrameEvents(frameEntityList, this._amsConfig.FrameBatchSize);
                if (isFirst)
                {
                    isPublish = frameEntityList.Count <= 0;
                    reviewVideoRequestJson = CreateReviewRequestObject(assetinfo, tempList, metaData);
                    reviewId = JsonConvert.DeserializeObject<List<string>>(ExecuteCreateReviewApi(reviewVideoRequestJson).Result)
                        .FirstOrDefault();
                    isFirst = false;
                }
                else
                {
                    isPublish = frameEntityList.Count <= 0;
                    if (reviewId != null)
                    {
                        isSuccess = SubmitAddFramesReview(tempList, reviewId);
                    }

                }
                if (isPublish)
                {
                    string path = this._amsConfig.FfmpegFramesOutputPath + Path.GetFileNameWithoutExtension(assetinfo.VideoName) + "_aud_SpReco.vtt";
                    if (File.Exists(path))
                    {
                        if (ValidateVttResponse(path))
                        {
                            isTranscript = SubmitTranscript(reviewId, path);
                        }
                        if (isTranscript)
                        {
                            var sts = GenerateTextScreenProfanity(reviewId, path);
                        }
                        try
                        {
                            System.IO.File.Delete(path);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    isSuccess = PublishReview(reviewId);

                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("\n Review Created Successfully ");
                    if (isSuccess)
                    {
                        Console.WriteLine("\n Video Published.");
                    }
                    else
                    {
                        Console.WriteLine("\n Video UnPublished.");
                    }
                    break;
                }
            }

            return reviewId;
        }

        /// <summary>
        /// Check Vtt Response
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool ValidateVttResponse(string path)
        {
            var response = ValidateVtt(path).Result;
            return response.IsSuccessStatusCode;

        }

        /// <summary>
        /// Validate vtt file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> ValidateVtt(string path)
        {
            string resultJson = string.Empty;
            var client = new HttpClient();

            var uri = this._amsConfig.ValidateVttUrl;

            HttpResponseMessage response;
            byte[] bytesData = System.IO.File.ReadAllBytes(path);
            using (var content = new ByteArrayContent(bytesData))
            {
                response = await client.PostAsync(uri, content);
                resultJson = await response.Content.ReadAsStringAsync();
            }
            return response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reviewId"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private bool GenerateTextScreenProfanity(string reviewId, string filepath)
        {
            var textScreenResult = TextScreen(filepath).Result;
            if (textScreenResult != null)
            {
                var response = ExecuteAddTranscriptSupportFile(reviewId, textScreenResult).Result;
                return response.IsSuccessStatusCode;
            }
            return true;
        }


        private string GenerateProfanityObject(string profanity)
        {
            List<TranscriptProfanity> profanityList = new List<TranscriptProfanity>();
            var jsonTextScreen = JsonConvert.DeserializeObject<TextScreen>(profanity);
            if (jsonTextScreen != null)
            {

                TranscriptProfanity transcriptProfanity = new TranscriptProfanity();
                transcriptProfanity.TimeStamp = "";
                List<Terms> transcriptTerm = new List<Terms>();
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
            return JsonConvert.SerializeObject(profanityList);
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
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add(Constants.SubscriptionKey, this._amsConfig.ReviewApiSubscriptionKey);

            var uri = string.Format(this._amsConfig.TextModerationResultUrl, this._amsConfig.TeamId, reviewId);// + queryString;

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

        /// <summary>
        /// Posts the Review Video object and returns a result.
        /// </summary>
        /// <param name="reviewVideoObj">Reviewvideo requestJson</param>
        /// <returns>Review Id</returns>
        private async Task<string> ExecuteCreateReviewApi(string reviewVideoObj)
        {
            string resultJson = string.Empty;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add(Constants.SubscriptionKey, this._amsConfig.ReviewApiSubscriptionKey);

            var uri = string.Format(this._amsConfig.ReviewCreationUrl, this._amsConfig.TeamId);// + queryString;

            HttpResponseMessage response;
            byte[] byteData = Encoding.UTF8.GetBytes(reviewVideoObj);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                resultJson = await response.Content.ReadAsStringAsync();
            }
            return resultJson;

        }

        /// <summary>
        /// Fetch frames from the source frames list.
        /// </summary>
        /// <param name="tempFrameEventsList">List of FrameEventBlobEntity</param>
        /// <param name="batchSize">Batch Size</param>
        private List<FrameEventDetails> FetchFrameEvents(List<FrameEventDetails> tempFrameEventsList, int batchSize)
        {
            List<FrameEventDetails> sample = new List<FrameEventDetails>();
            if (tempFrameEventsList.Count > 0)
            {
                if (batchSize < tempFrameEventsList.Count)
                {
                    sample.AddRange(tempFrameEventsList.Take(batchSize));
                    tempFrameEventsList.RemoveRange(0, batchSize);
                }
                else
                {
                    sample.AddRange(tempFrameEventsList.Take(tempFrameEventsList.Count));
                    tempFrameEventsList.Clear();
                }
                return sample;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a review video request object.
        /// </summary>
        /// <param name="assetInfo">UploadAssetResult</param>
        /// <param name="frameEvents">List of FrameEventBlobEntity</param>
        /// <returns>Reviewvideo</returns>
        private string CreateReviewRequestObject(UploadAssetResult assetInfo, List<FrameEventDetails> frameEvents, List<Metadata> metaData)
        {
            List<ReviewVideo> reviewList = new List<ReviewVideo>();
            ReviewVideo reviewVideoObj = new ReviewVideo();
            reviewVideoObj.Type = Constants.VideoEntityType;
            reviewVideoObj.Content = assetInfo.StreamingUrlDetails.SmoothUrl;
            reviewVideoObj.ContentId = assetInfo.VideoName;
            reviewVideoObj.CallbackEndpoint = this._amsConfig.ReviewCallBackUrl;
            reviewVideoObj.TimeScale = frameEvents.Select(a => a.TimeScale).First().ToString();
            reviewVideoObj.Metadata = metaData;
            //reviewVideoObj.Status = isPublish ? null : Constants.PublishedStatus;
            reviewVideoObj.Status = Constants.PublishedStatus;
            reviewVideoObj.VideoFrames = GenerateVideoFrames(frameEvents);
            reviewList.Add(reviewVideoObj);
            return JsonConvert.SerializeObject(reviewList);
        }

        /// <summary>
        /// Geneartes Video Frames
        /// </summary>
        /// <param name="frameEvents">List of FrameEventBlobEntity</param>
        /// <returns>List of Video frames</returns>
        private List<VideoFrame> GenerateVideoFrames(List<FrameEventDetails> frameEvents)
        {
            List<VideoFrame> videoFrames = new List<VideoFrame>();
            foreach (FrameEventDetails frameEvent in frameEvents)
            {
                VideoFrame videoFrameObj = PopulateVideoFrame(frameEvent);
                videoFrames.Add(videoFrameObj);
            }
            return videoFrames;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frameEvents"></param>
        /// <returns></returns>
        private List<Metadata> GenerateMetadata(List<FrameEventDetails> frameEvents)
        {
            List<Metadata> metadata = new List<Metadata>();
            FrameEventDetails adultScore = frameEvents.OrderByDescending(a => a.AdultConfidence).First();
            FrameEventDetails racyScore = frameEvents.OrderByDescending(a => a.RacyConfidence).First();
            bool isAdult = frameEvents.Any(x => x.IsAdultContent);
            bool isRacy = frameEvents.Any(x => x.IsRacyContent);
            if (UploadAssetResult.V2JSONPath != null)
            {
                metadata = new List<Metadata>()
                {
                    new Metadata() {Key = "adultScore", Value = adultScore.AdultConfidence},
                    new Metadata() {Key = "a", Value = isAdult.ToString()},
                    new Metadata() {Key = "racyScore", Value = racyScore.RacyConfidence},
                    new Metadata() {Key = "r", Value = isRacy.ToString()}

                };

            }
            else
            {
                metadata = new List<Metadata>()
                {
                    new Metadata() { Key= "adultScore",Value=adultScore.AdultConfidence},
                    new Metadata() {Key="a",Value=adultScore.IsAdultContent.ToString() },

                };

            }


            return metadata;
        }

        /// <summary>
        /// Populates a Video frame object
        /// </summary>
        /// <param name="frameEvent">FrameEventBlobEntity</param>
        /// <returns>Video Frame Object</returns>
        private VideoFrame PopulateVideoFrame(FrameEventDetails frameEvent)
        {
            if (UploadAssetResult.V2JSONPath != null)
            {
                VideoFrame frameobj = new VideoFrame()
                {
                    FrameImage = frameEvent.PrimaryUri,
                    Timestamp = Convert.ToString(frameEvent.TimeStamp),
                    ReviewerResultTags = new List<ReviewResultTag>(),
                    Metadata = new List<Metadata>()
                    {
                        new Metadata() {Key = "adultScore", Value = frameEvent.AdultConfidence},
                        new Metadata() {Key = "a", Value = frameEvent.IsAdultContent.ToString()},
                        new Metadata() {Key = "racyScore", Value = frameEvent.RacyConfidence},
                        new Metadata() {Key = "r", Value = frameEvent.IsRacyContent.ToString()},
                        new Metadata() {Key = "ExternalId", Value = frameEvent.FrameName}
                    }

                };
                return frameobj;
            }
            else
            {
                VideoFrame frameobj = new VideoFrame()
                {
                    FrameImage = frameEvent.PrimaryUri,
                    Timestamp = Convert.ToString(frameEvent.TimeStamp),
                    ReviewerResultTags = new List<ReviewResultTag>(),
                    Metadata = new List<Metadata>()
                    {
                        new Metadata() {Key = "adultScore", Value = frameEvent.AdultConfidence},
                        new Metadata() {Key = "A", Value = frameEvent.IsAdultContent.ToString()},
                        new Metadata() {Key = "ExternalId", Value = frameEvent.FrameName}
                    }

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
        public bool SubmitAddFramesReview(List<FrameEventDetails> frameEvents, string reviewId)
        {
            string inputRequestObj = CreateAddFramesReviewRequestObject(frameEvents);
            var response = ExecuteAddFramesReviewApi(reviewId, inputRequestObj).Result;
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reviewId"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool SubmitTranscript(string reviewId, string path)
        {

            var response = ExecuteAddTranscriptApi(reviewId, path).Result;
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Posts frames to add it to the video.
        /// </summary>
        /// <param name="reviewId">reviewID</param>
        /// <param name="reviewFrameList">reviewFrameList</param>
        /// <returns>Response of AddFrames Api call</returns>
        private async Task<HttpResponseMessage> ExecuteAddFramesReviewApi(string reviewId, string reviewFrameList)
        {
            var client = new HttpClient();
            // string resultJson = string.Empty;
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this._amsConfig.ReviewApiSubscriptionKey);

            var uri = string.Format(this._amsConfig.AddFramesUrl, this._amsConfig.TeamId, reviewId);
            HttpResponseMessage response;

            byte[] byteData = Encoding.UTF8.GetBytes(reviewFrameList);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
            }
            return response;
        }

        /// <summary>
        /// Forms the AddFrames request object.
        /// </summary>
        /// <param name="frameEvents">List of Frame events</param>
        /// <returns>Json representation of video frames collection.</returns>
        private string CreateAddFramesReviewRequestObject(List<FrameEventDetails> frameEvents)
        {
            List<VideoFrame> videoFrames = GenerateVideoFrames(frameEvents);
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

            client.DefaultRequestHeaders.Add(Constants.SubscriptionKey, this._amsConfig.ReviewApiSubscriptionKey);

            var uri = string.Format(this._amsConfig.AddTranscriptUrl, this._amsConfig.TeamId, reviewId);

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

        /// <summary>
        /// Identifying profanity words 
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private async Task<string> TextScreen(string filepath)
        {
            List<TranscriptProfanity> profanityList = new List<TranscriptProfanity>();
            string responseContent = string.Empty;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add(Constants.SubscriptionKey, this._amsConfig.ReviewApiSubscriptionKey);

            var uri = string.Format(this._amsConfig.TranscriptModerationUrl);// + queryString;
            HttpResponseMessage response;

            byte[] byteArray = System.IO.File.ReadAllBytes(filepath);
            string vttData = System.Text.Encoding.UTF8.GetString(byteArray);


            string[] vttList = splitVtt(vttData, 1023).ToArray();
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
                }
            }

            return JsonConvert.SerializeObject(profanityList);
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
            var response = ExecutePublishReviewApi(reviewId).Result;
            int retry = 3;
            while(!response.IsSuccessStatusCode && retry > 0)
            {
                response = ExecutePublishReviewApi(reviewId).Result;
                retry--;
            }
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        ///  Posts a request to Publih review api with provieded params.
        /// </summary>
        /// <param name="reviewId">reviewId</param>
        /// <returns>Returns response of publish review.</returns>
        private async Task<HttpResponseMessage> ExecutePublishReviewApi(string reviewId)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this._amsConfig.ReviewApiSubscriptionKey);
            var uri = string.Format(this._amsConfig.PublishReviewUrl, this._amsConfig.TeamId, reviewId);
            HttpResponseMessage response;
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

        #endregion

    }
}
