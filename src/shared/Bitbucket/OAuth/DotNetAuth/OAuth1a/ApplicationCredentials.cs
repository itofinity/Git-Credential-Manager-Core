namespace DotNetAuth.OAuth1a
{
    /// <summary>
    /// Encapsulates a set of keys which is required to identify your application. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// For each OAuth 1.0a provider you need to define an application through their developers dashboard.
    /// Once you define your application you will be provided with a set of Consumer key and Consumer secret values. 
    /// This class encapsulates those values.    
    /// </para>
    /// <para>
    /// You should not reveal these values publicly. To be able to change them you probably will put these values
    /// into a configuration file. It is suggested to encrypt these values while saving in a configuration file to avoid
    /// those values being accessible to public.
    /// </para>
    /// <para>
    /// <h4>OAuth 2.0</h4>
    /// The type <see cref="DotNetAuth.OAuth2.ApplicationCredentials"/> plays the same rule for OAuth 2.0.
    /// </para>
    /// </remarks>
    public class ApplicationCredentials
    {
        /// <summary>
        /// Gets or sets the consumer key.
        /// </summary>
        public string ConsumerKey { get; set; }
        /// <summary>
        /// Gets or sets the consumer secret.
        /// </summary>
        public string ConsumerSecret { get; set; }
    }
}
