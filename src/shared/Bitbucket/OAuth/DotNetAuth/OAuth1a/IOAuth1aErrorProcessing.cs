using System;
using DotNetAuth.OAuth1a.Framework;

namespace DotNetAuth.OAuth1a
{
    /// <summary>
    /// To provide custom error handling for an OAuth provider.
    /// </summary>
    public interface IOAuth1aErrorProcessing
    {
        /// <summary>
        /// When the response of an OAuth request token is not confirmed, this method will be called allowing to invesitgate the response and 
        /// augment error details.
        /// </summary>
        /// <param name="responseBody">The response body recieved after request token.</param>
        /// <param name="responseParameters">The parsed list of parameters in response.</param>
        /// <returns></returns>
        RequestTokenNotConfirmedException ProcessRequestTokenNotConfirmedResponse(string responseBody, ParameterSet responseParameters);
    }

    /// <summary>
    /// Retrieves requests for error processing and delivers them to the relevant <see cref="IOAuth1aErrorProcessing"/> instance.
    /// </summary>
    public static class ErrorProcessing
    {
        /// <summary>
        /// Invokes the <see cref="IOAuth1aErrorProcessing.ProcessRequestTokenNotConfirmedResponse"/> of <paramref name="definition"/> if it implements the interface, otherwise returns a default exception instance.
        /// </summary>
        /// <param name="definition">The defintion of OAuth provider.</param>
        /// <param name="responseBody">The response body recieved after request token.</param>
        /// <param name="responseParameters">The parsed list of parameters in response.</param>
        /// <returns></returns>
        public static RequestTokenNotConfirmedException ProcessRequestTokenNotConfirmedResponse(OAuth1aProviderDefinition definition, string responseBody, ParameterSet responseParameters)
        {
            var customErrorProcessing = definition as IOAuth1aErrorProcessing;
            if (customErrorProcessing != null)
            {
                return customErrorProcessing.ProcessRequestTokenNotConfirmedResponse(responseBody, responseParameters);
            }
            return new RequestTokenNotConfirmedException("Provider did not provide request token. Check your application registration, credentials, callback to be accurate and valid.", responseParameters);
        }
    }

    /// <summary>
    /// An exception indicating that RequestToken was not confirmed. 
    /// </summary>
    /// <remarks>
    /// Unless the request token request is confirmed you will not recieve a 'request token'. The failure could be for any reason. Providers will
    /// add causes and detail to the response body in their own dedicated format.
    /// </remarks>
    public class RequestTokenNotConfirmedException : Exception
    {
        /// <summary>
        /// Constructs and instance of <see cref="RequestTokenNotConfirmedException"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The list of parameters in response to request token request.</param>
        public RequestTokenNotConfirmedException(string message, ParameterSet parameters)
                : base(message) { ResponseParameters = parameters; }

        /// <summary>
        /// The parameters 
        /// </summary>
        public ParameterSet ResponseParameters { get; set; }
    }
}