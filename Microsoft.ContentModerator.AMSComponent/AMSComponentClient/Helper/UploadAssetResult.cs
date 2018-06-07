using Microsoft.WindowsAzure.MediaServices.Client;

namespace Microsoft.ContentModerator.AMSComponentClient
{
    /// <summary>
    /// UploadVideoResponse used for getting information related to uploded video after uploding to  AMS
    /// </summary>
    public class UploadAssetResult
    {
        public bool GenerateVTT { get; set; }
		/// <summary>
		/// For Streaming Urls
		/// </summary>
		public PublishedUrlDetails StreamingUrlDetails { get; set; }

        /// <summary>
        /// Token used for getting token after encryption
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// VideoName used for AssetName after uploding video to AMS
        /// </summary>     
        public string VideoName { get; set; }
        /// <summary>
        /// ModeratedJson used for getting json result after video moderation
        /// </summary>
        public string ModeratedJson { get; set; }


        public string VideoFilePath { get; set; }

        public bool Category1TextTag { get; set; }
        public bool Category2TextTag { get; set; }
        public bool Category3TextTag { get; set; }
        public double Category1TextScore { get; set; }
        public double Category2TextScore { get; set; }
        public double Category3TextScore { get; set; }
    }
}
