using Microsoft.WindowsAzure.MediaServices.Client;

namespace Microsoft.ContentModerator.BusinessEntities.Entities
{
    /// <summary>
    /// UploadVideoResponse used for getting information related to uploded video after uploding to  AMS
    /// </summary>
    public class UploadAssetResult
    {
		public static string V2JSONPath { get; set; }

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
        /// AssetId used for Asset Id after uploding video to AMS
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// ModeratedJson used for getting json result after video moderation
        /// </summary>
        public string ModeratedJson { get; set; }


        public IAsset Asset { get; set; }

        public string VideoFilePath { get; set; }


        public static string VideoPath { get; set; }

    }
}
