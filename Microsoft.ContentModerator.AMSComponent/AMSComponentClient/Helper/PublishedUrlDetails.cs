namespace Microsoft.ContentModerator.AMSComponentClient
{
    /// <summary>
    /// Represents a PublishedUrlDetails of an AMS asset.
    /// </summary>
    public class PublishedUrlDetails
    {
        /// <summary>
        /// SmoothUrl for Uploded Video
        /// </summary>
        public string SmoothUrl { get; set; }

        /// <summary>
        ///  MpegDashUri for Uploded Video
        /// </summary>
        public string MpegDashUri { get; set; }

        /// <summary>
        /// Hlsv3Uri for Uploded Video
        /// </summary>
        public string Hlsv3Uri { get; set; }

        /// <summary>
        /// Hlsv4Uri for Uploded Video
        /// </summary>
        public string Hlsv4Uri { get; set; }

        /// <summary>
        /// UrlWithOriginLocator for Uploded Video
        /// </summary>
        public string UrlWithOriginLocator { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string DownloadUri { get; set; }


    }
}
