using System;
using System.Configuration;

namespace Microsoft.ContentModerator.BusinessEntities
{
    public class AmsConfigurations
    {

	    // Accountname

	    // 1. MediaService Configurations

	    // 2. FFMPEG configurations

	    // 3. Review API configurations


		#region MediaService Configurations

		public string MediaServiceAccountKey = ConfigurationManager.AppSettings["MediaServiceAccountKey"];

		public string MediaServiceAccountName = ConfigurationManager.AppSettings["MediaServiceAccountName"];

		public string BlobConnectionString = ConfigurationManager.AppSettings["BlobConnectionString"];

		public string BlobContainerName = ConfigurationManager.AppSettings["BlobContainerName"];

		public string MediaProcessor = "Media Encoder Standard";//"Azure Media Encoder";

        public string ModerationProcessor = "Azure Media Content Moderator";

		public string MediaIndexer2MediaProcessor = "Azure Media Indexer 2 Preview";

		public string ModerationConfigurationJson = @"..\XmlConfiguration\Config.json";

	    public string MediaIndexerConfigurationJson = @"..\XmlConfiguration\MediaIndexerConfig.json";

		public double StreamingUrlActiveDays = Convert.ToInt32(ConfigurationManager.AppSettings["StreamingUrlActiveDays"]);
		
		public string BlobContainerForUploadbyUrl = "UploadByUrl";

        public string DefaulVideopName = "VideoByUrl";
		
		#endregion

		#region FFMPEG Configurations

		public int FrameBatchSize = 100;
	
		#endregion

		#region ReviewAPI Configurations
        private static string ContentModeraotrApiEndpoint = ConfigurationManager.AppSettings["ContentModeratorApiEndpoint"];
			
	    public string TeamId = ConfigurationManager.AppSettings["TeamId"];
		
	    public string ReviewApiSubscriptionKey = ConfigurationManager.AppSettings["ReviewApiSubscriptionKey"];

        /// <summary>
        /// These endpoints can change. Check to see if the endpoint changed when app is not working.
        /// </summary>
        public string ReviewCreationUrl = String.Concat(ContentModeraotrApiEndpoint, "/review/v1.0/teams/{0}/reviews");

		public string AddFramesUrl = String.Concat(ContentModeraotrApiEndpoint, "/review/v1.0/teams/{0}/reviews/{1}/frames");

		public string PublishReviewUrl = String.Concat(ContentModeraotrApiEndpoint, "/review/v1.0/teams/{0}/reviews/{1}/publish");

        public string AddTranscriptUrl = String.Concat(ContentModeraotrApiEndpoint, "/review/v1.0/teams/{0}/reviews/{1}/transcript");

        public string TranscriptModerationUrl = String.Concat(ContentModeraotrApiEndpoint, "/moderate/v1.0/ProcessText/Screen/?language=eng");

        public string TextModerationResultUrl = String.Concat(ContentModeraotrApiEndpoint, "/review/v1.0/teams/{0}/reviews/{1}/transcriptsupport");

        public string ValidateVttUrl = ConfigurationManager.AppSettings["ValidateVttUrl"];

		public string ReviewCallBackUrl = "";
        #endregion


        #region

        public string ModeratedJsonOutputPath = @"..\XmlConfiguration\";

        public string FfmpegFramesOutputPath = @"..\XmlConfiguration\";

        public string FfmpegExecutablePath = @"..\XmlConfiguration\ffmpeg.exe";

        public string BlobContainerForFfmpeg = "framegenerater";

        public string BlobFile = "ffmpeg.exe";
        #endregion


	    public bool CheckValidations()
	    {
		    if (!string.IsNullOrEmpty(MediaServiceAccountKey) && !string.IsNullOrEmpty(MediaServiceAccountName)
		        && !string.IsNullOrEmpty(BlobConnectionString) && !string.IsNullOrEmpty(BlobContainerName) &&
		        !string.IsNullOrEmpty(TeamId) && !string.IsNullOrEmpty(ReviewApiSubscriptionKey) && !string.IsNullOrEmpty(ReviewCreationUrl)
			    && !string.IsNullOrEmpty(AddFramesUrl) && !string.IsNullOrEmpty(PublishReviewUrl) &&
			    !string.IsNullOrEmpty(AddTranscriptUrl) && !string.IsNullOrEmpty(ValidateVttUrl))
		    {
			    return true;
		    }
		    else
		    {
			    return false;
		    }
	    }
    }
}
