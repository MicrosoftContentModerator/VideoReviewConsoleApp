using System.ComponentModel;

namespace Microsoft.ContentModerator.AMSComponentClient
{
    /// <summary>
    /// Encoding used for different types of Bitrate for Encoding
    /// </summary>
    public enum AmsEncoding
    {
        /// <summary>
        /// H264 Multiple Bitrate 720p
        /// </summary>
        [Description("H264 Multiple Bitrate 720p")]
        H264Multiplebitrate720P,
        /// <summary>
        /// H264 Broadband 1080p Bitrate
        /// </summary>
        [Description("H264 Broadband 1080p")]
        H264Broadband1080P,
        /// <summary>
        /// H264 Adaptive Bitrate MP4 Set 1080p
        /// </summary>
        [Description("H264 Adaptive Bitrate MP4 Set 1080p")]
        H264AdaptiveBitrateMp4Set1080P,
        /// <summary>
        /// H264 Smooth Streaming 1080p
        /// </summary>
        [Description("H264 Smooth Streaming 1080p")]
        H264SmoothStreaming1080P,

        /// <summary>
        /// Adaptive Streaming
        /// </summary>
        [Description("Adaptive Streaming")]
        AdaptiveStreaming
    }
}
