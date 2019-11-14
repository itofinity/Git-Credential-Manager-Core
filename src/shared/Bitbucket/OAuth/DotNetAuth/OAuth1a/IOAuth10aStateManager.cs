using System;

namespace DotNetAuth.OAuth1a
{
    /// <summary>
    /// An interface which allows saving state through OAuth 1.0a authentication process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// OAuth 1.0a requires two tokens to be saved temporarily. The set of tokens helps the process keep integrated.
    /// This interface provides two methods. One method is called before redirection and will receive two keys, one is
    /// named requestToken and the other one is token secret.
    /// </para>
    /// <para>
    /// <h4>OAuth 2.0</h4>
    /// OAuth 2 also supports state management. However the process is different. The <see cref="DotNetAuth.OAuth2.IOAuth20StateManager"/> does 
    /// the same thing for OAuth 2.
    /// </para>
    /// </remarks>
    public interface IOAuth10aStateManager
    {
        /// <summary>
        /// The implementation should save the oauth_token_secret value. The requestToken argument can be used as key. 
        /// However Sometime you may ignore requestToken, for example when you are using session.
        /// </summary>
        /// <remarks>
        /// If you are using session, the session may expire and you may lost oauth_token_secret.
        /// </remarks>
        /// <param name="requestToken"></param>
        /// <param name="oauthTokenSecret"></param>
        void SaveTemporaryTokenSecret(string requestToken, string oauthTokenSecret);
        /// <summary>
        /// The implementation should return the value of oauth_token_secret which supposedly is previously saved by you when 
        /// <see cref="SaveTemporaryTokenSecret"/> was called. You may use requestToken argument as a key or you may use
        /// other means(like session) to retrieve back the oauth_token_secret.
        /// </summary>
        /// <param name="requestToken"></param>
        /// <returns>The oauth_token_secret previously saved by SaveTemporaryTokenSecret.</returns>
        string LoadTemporaryTokenSecret(string requestToken);
    }
    /// <summary>
    /// A default implementation of <see cref="IOAuth10aStateManager"/> which lets implementing the interface in-place.
    /// </summary>
    public class OAuth10aStateManager : IOAuth10aStateManager
    {
        readonly Action<string, string> save;
        readonly Func<string, string> load;
        /// <summary>
        /// Constructs an instance of state manager by receiving the methods to be executed for save or load actions.
        /// </summary>
        /// <remarks>
        /// You don't have to pass both methods, 
        /// if you are making a call to <see cref="OAuth1aProcess.RequestToken"/> only <paramref name="saveTemporaryTokenSecret"/> is required. And
        /// if you are making a call to <see cref="OAuth1aProcess.ProcessUserResponse"/> only the <paramref name="loadTemporaryTokenSecret"/> is required.
        /// </remarks>
        /// <param name="saveTemporaryTokenSecret">The implementation should save the oauth_token_secret value(second argument). The requestToken argument(first one) can be used as key.</param>
        /// <param name="loadTemporaryTokenSecret">The implementation should return the value of oauth_token_secret which supposedly is previously saved somewhere. You may use requestToken argument(the only argument) as a key.</param>
        public OAuth10aStateManager(Action<string, string> saveTemporaryTokenSecret, Func<string, string> loadTemporaryTokenSecret)
        {
            save = saveTemporaryTokenSecret;
            load = loadTemporaryTokenSecret;
        }
        /// <summary>
        /// The implementation of <see cref="IOAuth10aStateManager.SaveTemporaryTokenSecret"/> which simply delegates the task to method passed to constructor of this object.
        /// </summary>
        /// <param name="requestToken">The request token.</param>
        /// <param name="oauthTokenSecret">The oauth token secret.</param>
        public void SaveTemporaryTokenSecret(string requestToken, string oauthTokenSecret)
        {
            save(requestToken, oauthTokenSecret);
        }
        /// <summary>
        /// The implementation of <see cref="IOAuth10aStateManager.LoadTemporaryTokenSecret"/> which simply delegates the task to method passed to constructor of this object.
        /// </summary>
        /// <param name="requestToken">The request token.</param>
        /// <returns>The loaded oauth token secret.</returns>
        public string LoadTemporaryTokenSecret(string requestToken)
        {
            return load(requestToken);
        }
    }
}
