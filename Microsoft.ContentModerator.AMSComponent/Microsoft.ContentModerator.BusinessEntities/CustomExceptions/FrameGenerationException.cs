using System;

namespace Microsoft.ContentModerator.BusinessEntities.CustomExceptions
{
    /// <summary>
    /// Represents a FrameException
    /// </summary>
    public class FrameGenerationException : Exception
    {
        /// <summary>
        /// Instantiates an instance of frame exception.
        /// </summary>
        public FrameGenerationException()
        {

        }

        /// <summary>
        /// Gets or Sets the AssetId
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// Gets or Sets the frameId
        /// </summary>
        public string ReviewId { get; set; }

        /// <summary>
        /// Gets or Sets the Framename.
        /// </summary>
        public string VideoName { get; set; }

        /// <summary>
        /// Gets or Sets the Error Title 
        /// </summary>
        public string ErrorTitle { get; set; }

        /// <summary>
        /// Gets or Sets the Error Reason for the occured exception
        /// </summary>
        public string ErrorReason { get; set; }
    }

}
