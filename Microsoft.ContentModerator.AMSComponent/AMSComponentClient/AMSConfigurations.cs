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

        public readonly string AzureAdTenentName = ConfigurationManager.AppSettings["AzureAdTenantName"];
        public readonly string ClientId = ConfigurationManager.AppSettings["ClientId"];
        public readonly string ClientSecret = ConfigurationManager.AppSettings["ClientSecret"];
        public readonly string MediaServiceRestApiEndpoint = ConfigurationManager.AppSettings["AzureMediaServiceRestApiEndpoint"];
        public readonly string MediaProcessor = "Media Encoder Standard";//"Azure Media Encoder";
        public readonly string ModerationProcessor = "Azure Media Content Moderator";
        public readonly string MediaIndexer2MediaProcessor = "Azure Media Indexer 2 Preview";
        public readonly string ModerationConfigurationJson = @"..\..\Lib\Config.json";
        public readonly string MediaIndexerConfigurationJson = @"..\..\Lib\MediaIndexerConfig.json";
        public readonly double StreamingUrlActiveDays = Convert.ToInt32(ConfigurationManager.AppSettings["StreamingUrlActiveDays"]);
        public readonly string BlobContainerForUploadbyUrl = "UploadByUrl";
        public readonly string DefaulVideopName = "VideoByUrl";
        public readonly double AdultFrameThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["AdultFrameThreshold"]);
        public readonly double RacyFrameThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["RacyFrameThreshold"]);
        public readonly double Category1TextThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["Category1TextThreshold"]);
        public readonly double Category2TextThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["Category2TextThreshold"]);
        public readonly double Category3TextThreshold = Convert.ToDouble(ConfigurationManager.AppSettings["Category3TextThreshold"]);

        #endregion

        #region FFMPEG Configurations

        public int FrameBatchSize = 500;

        #endregion

        #region ReviewAPI Configurations
        public readonly static string ContentModeraotrApiEndpoint = ConfigurationManager.AppSettings["ContentModeratorApiEndpoint"];

        public readonly static string ReviewApiSubscriptionKey = ConfigurationManager.AppSettings["ContentModeratorReviewApiSubscriptionKey"];

        public readonly string TeamName = ConfigurationManager.AppSettings["ContentModeratorTeamId"];

        public readonly static string DemoVideoContainerUrl = ConfigurationManager.AppSettings["DemoVideoContainerUrl"];

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
            if (!string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret)
                && !string.IsNullOrEmpty(TeamName)
                && !string.IsNullOrEmpty(ReviewApiSubscriptionKey)
                && !string.IsNullOrEmpty(MediaServiceRestApiEndpoint)
                && !string.IsNullOrEmpty(AzureAdTenentName)

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
