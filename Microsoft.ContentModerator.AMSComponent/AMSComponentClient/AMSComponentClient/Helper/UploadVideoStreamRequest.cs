namespace Microsoft.ContentModerator.AMSComponentClient
{
    /// <summary>
    /// Represents a UploadVideoStream request.
    /// </summary>
    public class UploadVideoStreamRequest : UploadVideoRequest
    {
        /// <summary>
        /// VideoStream used for getting byte[] of uploded video
        /// </summary>               
        public byte[] VideoStream { get; set; }
        public string VideoFilePath { get; set; }
    }
}
