using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetAuth.OAuth1a.Framework
{
    /// <summary>
    /// A series of methods useful in handling OAuth 1.0 protocol special requirements.
    /// </summary>
    public static class OAuth1aUtil
    {
        /// <summary>
        /// Generates a Nonce, a value which is unique in every request made to a provider.
        /// </summary>
        /// <returns>A random value which can be used as nonce parameter.</returns>
        public static string GenerateNonce()
        {
            var bytes = new byte[32];
            var first = Guid.NewGuid().ToByteArray();
            var second = Guid.NewGuid().ToByteArray();
            for (var i = 0; i < 16; i++)
                bytes[i] = first[i];
            for (var i = 16; i < 32; i++)
                bytes[i] = second[i - 16];
            var result = Convert.ToBase64String(bytes, Base64FormattingOptions.None);
            result = new string(result.ToCharArray().Where(char.IsLetter).ToArray());
            return result;
        }

        /// <summary>
        /// Concatenate a list of parameters together and represent them as a single string value.
        /// </summary>
        /// <remarks>
        /// <b>Note:</b> You should not pass the URL encoded (or percent encoded) values to this function. This method does percent encoding.
        /// </remarks>
        /// <param name="parameters">Parameters to be appended together and form a parameter string, value should not be percent encoded as this method handles percent encoding.</param>
        /// <returns>A string representing all parameters concatenated using <c>&amp;</c>. </returns>
        public static string CalculateParameterString(KeyValuePair<string, string>[] parameters)
        {
            var q =
                // for each parameter
                from entry in parameters
                // percent encode key
                let encodedkey = PercentEncode.Encode(entry.Key)
                // percent encode value
                let encodedValue = PercentEncode.Encode(entry.Value)
                // generate 'key=value' string
                let encodedEntry = encodedkey + "=" + encodedValue
                // set 'key=value' as order key
                orderby encodedEntry
                // select 'key=value'
                select encodedEntry;
            // join 'key=value' entries using '&'
            var result = string.Join("&", q.ToArray());
            return result;
        }
        /// <summary>
        /// Calculates signature base string with the pattern <c>'verb&amp;baseUri&amp;parametersString'</c>.
        /// </summary>
        /// <param name="httpMethod">Http method in which the request is going to be made. Usually <c>GET</c> or <c>POST</c>.</param>
        /// <param name="baseUri">The URI of request to be made. Do no include query string.</param>
        /// <param name="parametersString">The calculated parameters string. <see cref="CalculateParameterString"/> can be used to generate this parameter.</param>
        /// <returns></returns>
        public static string CalcualteSignatureBaseString(string httpMethod, string baseUri, string parametersString)
        {
            return httpMethod.ToUpper() + "&" + PercentEncode.Encode(baseUri) + "&" + PercentEncode.Encode(parametersString);
        }
        /// <summary>
        /// Calculates a signing key with the pattern <c>'ConsumerSecret&amp;OAuthTokenSecret'</c>.
        /// </summary>
        /// <remarks>
        /// Singing key is ConsumerSecret followed by &amp; and then followed by token secret. token secret can be ignored.
        /// </remarks>
        /// <param name="consumerSecret"></param>
        /// <param name="oAuthTokenSecret"></param>
        /// <returns></returns>
        public static string GetSigningKey(string consumerSecret, string oAuthTokenSecret = null)
        {
            return consumerSecret + "&" + (oAuthTokenSecret ?? string.Empty);
        }
    }
}
