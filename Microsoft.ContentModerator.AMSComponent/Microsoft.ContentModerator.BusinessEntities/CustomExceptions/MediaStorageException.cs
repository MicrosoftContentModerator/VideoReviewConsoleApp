using System;

namespace Microsoft.ContentModerator.BusinessEntities.CustomExceptions
{
    public class MediaStorageException : Exception
    {

        /// <summary>
        /// Instantiates an Asset Exception class. 
        /// </summary>
        /// 
        public MediaStorageException()
        {

        }

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
