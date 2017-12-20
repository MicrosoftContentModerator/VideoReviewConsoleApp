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

        public double AdultFrameThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["AdultFrameThreshold"]);
        
        public double RacyFrameThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["RacyFrameThreshold"]);

        public double OffensiveTextThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["OffensiveTextThreshold"]);

        public double RacyTextThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["RacyTextThreshold"]);

        public double AdultTextThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["AdultTextThreshold"]);

        #endregion

        #region FFMPEG Configurations

        public int FrameBatchSize = 500;

        #endregion

        #region ReviewAPI Configurations
        public static string ContentModeraotrApiEndpoint = ConfigurationManager.AppSettings["ContentModeratorApiEndpoint"];

        public string TeamName = ConfigurationManager.AppSettings["ContentModeratorTeamId"];

        public static string ReviewApiSubscriptionKey = ConfigurationManager.AppSettings["ContentModeratorReviewApiSubscriptionKey"];

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
                )
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
