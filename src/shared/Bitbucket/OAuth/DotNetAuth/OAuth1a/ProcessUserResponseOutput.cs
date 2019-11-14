using DotNetAuth.OAuth1a.Framework;

namespace DotNetAuth.OAuth1a
{
    /// <summary>
    /// A class encapsulating output of <see cref="OAuth1aProcess.ProcessUserResponse"/> 
    /// </summary>
    public class ProcessUserResponseOutput
    {
        /// <summary>
        /// If true then indicates user has authorized your application.
        /// </summary>
        public bool Accepted { get; set; }
        /// <summary>
        /// If true then indicates user has rejected to authorize your application.
        /// </summary>
        public bool Rejected { get; set; }
        /// <summary>
        /// If true then indicates an error happened in getting user response.
        /// </summary>
        public bool Error { get; set; }
        /// <summary>
        /// This is the original request token you received after calling <see cref="OAuth1aProcess.RequestToken"/>. You may use this to manage user state(user has
        /// left your site and now he is back in your site, you can use this request token to remember them).
        /// </summary>
        /// <remarks>
        /// At step 1 you made a call to RequestToken and you received a temporary token which is called request token.
        /// At step 2 you you redirected user to service providers authorization endpoint.
        /// At step 3 user is redirected back to your site. After processing the user response 
        /// </remarks>
        public string RequestToken { get; set; }
        /// <summary>
        /// Gets the whole set of parameters in the response.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property holds all the parameters in output. Only some of those parameters is accessible by properties of <see cref="ProcessUserResponseOutput"/>, so this propery gives complete
        /// access to the list of all parameters provided in output.
        /// </para>
        /// <para>
        /// There are shortcut properties to retrive the common standard properties directly. <see cref="AccessToken"/> will give you the access token. <see cref="Error"/>, <see cref="Accepted"/> and <see cref="Rejected"/> will tell you if
        /// an error happened during the authorization process.
        /// </para>
        /// </remarks>
        public ParameterSet AllParameters { get; set; }

        /// <summary>
        /// Returns the access token.
        /// </summary>
        /// <remarks>
        /// Having this value means that your application has been authorized by user. 
        /// This is the golden key you need to have in order to make authorized requests.
        /// </remarks>
        public string AccessToken { get { return AllParameters[Names.AccessTokenResponse.oauth_token]; } }

        /// <summary>
        /// Returns the access token secret.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public string AccessTokenSecret { get { return AllParameters[Names.AccessTokenResponse.oauth_token_secret]; } }
    }
}
