
namespace Microsoft.ContentModerator.AMSComponentClient
{
    /// <summary>
    ///  Represents an Encoding Request used for encoding a video source.
    /// </summary>
    public class EncodingRequest
    {
        /// <summary>
        /// EncodingBitrate for choosing Bitrate from different Bitrate to perform  Video Encoding
        /// </summary>
        public AmsEncoding EncodingBitrate { get; set; }

    }
}
