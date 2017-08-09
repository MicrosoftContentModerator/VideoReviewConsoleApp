using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ContentModerator.AMSComponentClient
{

    /// <summary>
    /// Represents a ReviewVideo object.
    /// </summary>
    [DataContract]
    public class ReviewVideo
    {
        /// <summary>
        /// Gets or Sets the VideoFrames
        /// </summary>
        [DataMember]
        public List<VideoFrame> VideoFrames { get; set; }

        /// <summary>
        /// Gets or Sets the Metadata
        /// </summary>
        [DataMember]
        public List<Metadata> Metadata { get; set; }

        /// <summary>
        /// Gets or Sets the Entity type 
        /// </summary>
        [DataMember]
        public string Type { get; set; }

        /// <summary>
        /// Gets or Sets the Content
        /// </summary>
        [DataMember]
        public string Content { get; set; }

        /// <summary>
        /// Gets or Sets the ContentId
        /// </summary>
        [DataMember]
        public string ContentId { get; set; }

        /// <summary>
        /// Gets or Sets the Status
        /// </summary>
        [DataMember]
        public string Status { get; set; }


        /// <summary>
        /// Gets or Sets the CallbackEndpoint
        /// </summary>
        [DataMember]
        public string CallbackEndpoint { get; set; }
        /// <summary>
        /// 
        /// </summary>
       [DataMember]
        public string TimeScale { get; set; }

    }

    /// <summary>
    /// Represents a video frame object.
    /// </summary>
    [DataContract]
    public class VideoFrame
    {
        /// <summary>
        /// Gets or sets the Timestamp
        /// </summary>
        [DataMember]
        public string Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the FrameImage
        /// </summary>
        [DataMember]
        public string FrameImage { get; set; }

        /// <summary>
        /// Gets or sets the Metadata
        /// </summary>
        [DataMember]
        public List<Metadata> Metadata { get; set; }

        /// <summary>
        /// Gets or sets the ReviewerResultTags
        /// </summary>
        [DataMember]
        public List<ReviewResultTag> ReviewerResultTags { get; set; }

        [DataMember]
        public byte[] FrameImageBytes { get; set; }
    }

    /// <summary>
    /// Represents Metadata  
    /// </summary>
    [DataContract]
    public class Metadata
    {
        /// <summary>
        /// Gets or Sets the Key
        /// </summary>
        [DataMember]
        public string Key { get; set; }

        /// <summary>
        /// Gets or Sets the Value
        /// </summary>
        [DataMember]
        public string Value { get; set; }
    }

    /// <summary>
    /// Represents  ReviewResultTag 
    /// </summary>
    public class ReviewResultTag
    {
        /// <summary>
        /// Gets or Sets the key
        /// </summary>
        [DataMember]
        public string Key { get; set; }

        /// <summary>
        /// Gets or Sets the value of the key
        /// </summary>
        [DataMember]
        public string Value { get; set; }
    }

}
