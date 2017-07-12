using System;

namespace Microsoft.ContentModerator.BusinessEntities.CustomExceptions
{
    public class ReviewApiException : Exception
    {
        /// <summary>
        /// Gets or Sets the  Asset Identifier 
        /// </summary>
        public string AssetIdentifier { get; set; }

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
