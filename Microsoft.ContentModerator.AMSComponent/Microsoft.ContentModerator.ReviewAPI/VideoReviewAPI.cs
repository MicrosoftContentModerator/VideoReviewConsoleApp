using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Newtonsoft.Json;
using Microsoft.ContentModerator.ReviewAPI;
using Microsoft.ContentModerator.Services;
using Microsoft.ContentModerator.BusinessEntities;
using Microsoft.ContentModerator.BusinessEntities.Entities;

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


		public string ProcessReviewAPI(UploadAssetResult assetinfo, List<FrameEventDetails> frameEntityList, string reviewId)
		{
			bool isSuccess = false;
			bool isTranscript = false;
			string reviewVideoRequestJson = string.Empty;
			try
			{
				while (frameEntityList.Count > 0)
				{
					List<FrameEventDetails> tempList = new List<FrameEventDetails>();
					tempList = FetchFrameEvents(frameEntityList, this._amsConfig.FrameBatchSize);
					isSuccess = SubmitAddFramesReview(tempList, reviewId);
				}

				string path = this._amsConfig.FfmpegFramesOutputPath + Path.GetFileNameWithoutExtension(assetinfo.VideoName) + "_aud_SpReco.vtt";
				if (File.Exists(path))
				{
					if (ValidateVtt(path))
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
				if (!isSuccess)
				{
					Console.WriteLine("PUBLISH REVIEW FAILED!!");
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.DarkGreen;
					Console.WriteLine("\n Review Created Successfully and the review Id {0}", reviewId);
				}

				return reviewId;
			}
			catch (Exception e)
			{
				Console.WriteLine("An exception had occured at {0} API, for the Review ID : {1}", MethodBase.GetCurrentMethod().Name, reviewId);
				throw;
			}

		}

        
		/// <summary>
		/// Validate vtt file
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private bool  ValidateVtt(string path)
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
                    resultJson = await response.Content.ReadAsStringAsync();
                }
                return resultJson;
            }
            catch (Exception e)
            {
                //TODO Logging
                Console.WriteLine("An exception had occured at {0} API, for the ReviewObject is : {1} ", MethodBase.GetCurrentMethod().Name, reviewVideoObj);
                Console.WriteLine("The response from the Api is : \n {0}", resultJson);
                throw;
            }

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
				reviewVideoObj.TimeScale = frameEvents.Count != 0 ? frameEvents.Select(a => a?.TimeScale).FirstOrDefault().ToString() : "0";
				reviewVideoObj.Metadata = frameEvents.Count != 0? GenerateMetadata(frameEvents):null;
				reviewVideoObj.Status = Constants.PublishedStatus;
				reviewVideoObj.VideoFrames = null; 
				reviewList.Add(reviewVideoObj);
				return JsonConvert.SerializeObject(reviewList);
			}
			catch (Exception e)
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
			FrameEventDetails adultScore = frameEvents.OrderByDescending(a => a?.AdultConfidence).FirstOrDefault();
			if (UploadAssetResult.V2JSONPath != null)
			{
				FrameEventDetails racyScore = frameEvents.OrderByDescending(a => a?.RacyConfidence).FirstOrDefault();
				metadata = new List<Metadata>()
				{
					new Metadata() {Key = "adultScore", Value = adultScore.AdultConfidence},
					new Metadata() {Key = "a", Value = adultScore.IsAdultContent.ToString()},
					new Metadata() {Key = "racyScore", Value = racyScore.RacyConfidence},
					new Metadata() {Key = "r", Value = racyScore.IsRacyContent.ToString()}

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

			var uri = string.Format(this._amsConfig.AddFramesUrl, this._amsConfig.TeamName, reviewId);
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
                throw;
            }

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
			client.DefaultRequestHeaders.Add(Constants.SubscriptionKey, _amsConfig.ReviewApiSubscriptionKey);

			var uri = string.Format(this._amsConfig.TranscriptModerationUrl);
			HttpResponseMessage response;

			byte[] byteArray = File.ReadAllBytes(filepath);
			string vttData = Encoding.UTF8.GetString(byteArray);


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
                throw;
            }
        }

		#endregion

	}
}
