using System;
using System.Configuration;

namespace Microsoft.ContentModerator.AMSComponentClient
{
    public class AmsConfigurations
    {

        // Accountname

        // 1. MediaService Configurations

        // 2. FFMPEG configurations

        // 3. Review API configurations


        #region MediaService Configurations

        public string MediaServiceAccountKey = ConfigurationManager.AppSettings["AzureMediaServiceAccountKey"];

        public string MediaServiceAccountName = ConfigurationManager.AppSettings["AzureMediaServiceAccountName"];

        public string MediaProcessor = "Media Encoder Standard";//"Azure Media Encoder";

        public string ModerationProcessor = "Azure Media Content Moderator";

        public string MediaIndexer2MediaProcessor = "Azure Media Indexer 2 Preview";

        public string ModerationConfigurationJson = @"..\..\Lib\Config.json";

        public string MediaIndexerConfigurationJson = @"..\..\Lib\MediaIndexerConfig.json";

        public double StreamingUrlActiveDays = Convert.ToInt32(ConfigurationManager.AppSettings["StreamingUrlActiveDays"]);

        public string BlobContainerForUploadbyUrl = "UploadByUrl";

        public string DefaulVideopName = "VideoByUrl";

        public double OffensiveTextThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["OffensiveTextThreshold"]);

        public double RacyTextThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["RacyTextThreshold"]);

        public double AdultTextThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["AdultTextThreshold"]);

        #endregion

        #region FFMPEG Configurations

        public int FrameBatchSize = 500;

        #endregion

        #region ReviewAPI Configurations
        private static string ContentModeraotrApiEndpoint = ConfigurationManager.AppSettings["ContentModeratorApiEndpoint"];

        public string TeamName = ConfigurationManager.AppSettings["ContentModeratorTeamId"];

        public string ReviewApiSubscriptionKey = ConfigurationManager.AppSettings["ContentModeratorReviewApiSubscriptionKey"];

        /// <summary>
        /// These endpoints can change. Check to see if the endpoint changed when app is not working.
        /// </summary>
        public string ReviewCreationUrl = String.Concat(ContentModeraotrApiEndpoint, "/review/v1.0/teams/{0}/reviews");

        public string AddFramesUrl = String.Concat(ContentModeraotrApiEndpoint, "/review/v1.0/teams/{0}/reviews/{1}/frames");

        public string PublishReviewUrl = String.Concat(ContentModeraotrApiEndpoint, "/review/v1.0/teams/{0}/reviews/{1}/publish");

        public string AddTranscriptUrl = String.Concat(ContentModeraotrApiEndpoint, "/review/v1.0/teams/{0}/reviews/{1}/transcript");

        public string TranscriptModerationUrl = String.Concat(ContentModeraotrApiEndpoint, "/moderate/v1.0/ProcessText/Screen/?language=eng&classify=true");

        public string TextModerationResultUrl = String.Concat(ContentModeraotrApiEndpoint, "/review/v1.0/teams/{0}/reviews/{1}/transcriptsupport");

        public string ReviewCallBackUrl = "";
        #endregion


        #region

        public string ModeratedJsonOutputPath = @"..\..\Lib\";

        public string FfmpegFramesOutputPath = @"..\..\Lib\";

        public string FfmpegExecutablePath = @"..\..\Lib\ffmpeg.exe";

        public static string logFilePath = @"..\..\Lib\log.txt";

        public string BlobContainerForFfmpeg = "framegenerater";

        public string BlobFile = "ffmpeg.exe";
        #endregion


        public bool CheckValidations()
        {
            if (!string.IsNullOrEmpty(MediaServiceAccountKey) && !string.IsNullOrEmpty(MediaServiceAccountName)
                && !string.IsNullOrEmpty(TeamName)
                && !string.IsNullOrEmpty(ReviewApiSubscriptionKey)
                && !string.IsNullOrEmpty(ReviewCreationUrl)
                && !string.IsNullOrEmpty(AddFramesUrl)
                && !string.IsNullOrEmpty(PublishReviewUrl)
                && !string.IsNullOrEmpty(AddTranscriptUrl))
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
