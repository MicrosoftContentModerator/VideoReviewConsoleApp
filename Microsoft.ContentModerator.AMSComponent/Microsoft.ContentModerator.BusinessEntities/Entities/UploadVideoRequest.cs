using System;

namespace Microsoft.ContentModerator.BusinessEntities.Entities
{
    /// <summary>
    /// Represents a Upload Video Request entity.
    /// </summary>
    public class UploadVideoRequest
    {

        /// <summary>
        /// VideoName used for uploded video name
        /// </summary>
        public string VideoName { get; set; }

        /// <summary>
        /// Gets or Sets a EncryptRequest.
        /// </summary>
        public EncryptRequest EncryptRequest { get; set; }

        /// <summary>
        /// Gets or Sets a EncodingRequest.
        /// </summary>
        public EncodingRequest EncodingRequest { get; set; }

        /// <summary>
        /// Gets or Sets a AssetDuration
        /// </summary>
        public TimeSpan AssetDuration { get; set; }

    }
}
