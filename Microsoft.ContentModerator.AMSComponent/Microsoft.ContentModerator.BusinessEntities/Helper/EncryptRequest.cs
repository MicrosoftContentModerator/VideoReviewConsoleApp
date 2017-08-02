using Microsoft.WindowsAzure.MediaServices.Client.ContentKeyAuthorization;
using System;
using System.ComponentModel;

namespace Microsoft.ContentModerator.BusinessEntities.Entities
{
    /// <summary>
    /// Represents an Encrypt Request used for encrypting a video source.
    /// </summary>
    /// 
    public class EncryptRequest
    {
        /// <summary>
        /// Gets or sets the encryption type. Default value will be defined as SWT.
        /// </summary>
        public TokenType EncryptionType { get; set; }

        /// <summary>
        /// Gets or Sets the token duration.
        /// </summary>
        public DateTime? TokenDuration { get; set; }

        /// <summary>
        /// Gets or Sets the Audience Url
        /// </summary>
        [DefaultValue(" ")]
        public string  Audience { get; set; }

        /// <summary>
        /// Gets or Sets the Issuer Url
        /// </summary>
        [DefaultValue(" ")]
        public string Issuer { get; set; }
    }
}
