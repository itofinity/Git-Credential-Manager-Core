using System;
using System.IO;

namespace DotNetAuth.OAuth1a.Framework
{
    /// <summary>
    /// Provide a simple method to sign an string using a signing key.    
    /// </summary>
    internal class HMACSHA1
    {
        /// <summary>
        /// Sign a string using the given signing key.
        /// </summary>
        /// <param name="signatureBaseString">The signature base string.</param>
        /// <param name="signingKey">The signing key.</param>
        /// <returns>The signature calculated by applying signing key to signature base string.</returns>
        public static string Sign(string signatureBaseString, string signingKey)
        {
            var keyBytes = System.Text.Encoding.ASCII.GetBytes(signingKey);
            using (var myhmacsha1 = new System.Security.Cryptography.HMACSHA1(keyBytes)) {
                byte[] byteArray = System.Text.Encoding.ASCII.GetBytes(signatureBaseString);
                var stream = new MemoryStream(byteArray);
                var signedValue = myhmacsha1.ComputeHash(stream);
                var result = Convert.ToBase64String(signedValue, Base64FormattingOptions.None);
                return result;
            }
        }
    }
}
