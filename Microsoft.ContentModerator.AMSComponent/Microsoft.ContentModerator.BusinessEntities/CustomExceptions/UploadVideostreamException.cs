using System;

namespace Microsoft.ContentModerator.BusinessEntities.CustomExceptions
{
	/// <summary>
	/// Custom UploadVideostreamException Classs
	/// </summary>
	public class UploadVideostreamException : Exception
    {
		/// <summary>
		/// Instantiates an UploadVideostreamException  class. 
		/// </summary>
		/// 
		public UploadVideostreamException(bool status, string failureReason)
        {
	        Status = status;
	        ErrorReason = failureReason;
        }

        /// <summary>
        /// Gets or Sets the  status of the entire processing 
        /// </summary>
        public bool Status{ get; set; }

        /// <summary>
        /// Gets or Sets the Error Reason for the occured exception
        /// </summary>
        public string ErrorReason { get; set; }

    }
}
