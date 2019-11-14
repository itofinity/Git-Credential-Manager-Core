using System;

namespace DotNetAuth.OAuth1a.Framework
{
    /// <summary>
    /// A utility class for provision of timestamp value required in OAuth 1.0a protocol.
    /// </summary>
    /// <remarks>
    /// <para>
    /// According to OAuth1.0a specification we sometimes need to include a oauth_timestamp value in our
    /// request we made to provider. And this value is expressed in the number of seconds since January 1, 1970 00:00:00 GMT.
    /// The always pesky fact about this value is that it should be very accurate, if your host's clock is not accurate enough
    /// and surpasses the very small tolerance of server's implementation, then your requests will be rejected.
    /// The very simple solution to this problem is to adjust your host's clock to what is actually is.
    /// However if that is not possible you can set the value of static field <see cref="TimestampUtil.TimeDifferenceInSeconds"/> 
    /// to amount of difference between yours and global's clock expressed as seconds.
    /// </para>
    /// <para>
    /// <list type="number">
    /// <item><term>Tip 1:</term><description>You can search GMT now in Google and Google will tell what is it at moment.</description></item>
    /// <item><term>Tip 2:</term><description>You need to check DateTime.UtcNow to see your computer's clock expressed as GMT.</description></item>
    /// <item><term>Tip 3:</term><description>If those values you received in previous tips have a huge distance then you need to set <see cref="TimestampUtil.TimeDifferenceInSeconds"/></description></item>
    /// <item><term>Tip 4:</term><description>
    /// The oauth_timestamp in OAuth 1.0a is not intended to match server's time with consumer's time. It supposed to allow
    /// providers's check the request's order. So a newer request must have a bigger timestamp value. But it is like some providers
    /// have gone further.
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class TimestampUtil
    {
        /// <summary>
        /// Don't set a value for this property unless you know what your are doing.
        /// This property is here for adjusting your server's time with global time.
        /// <see cref="TimestampUtil"/> for more information.
        /// </summary>
        public static int TimeDifferenceInSeconds { get; set; }

        /// <summary>
        /// Get current time corrected by <see cref="TimeDifferenceInSeconds"/> .
        /// </summary>
        /// <returns>A date time value representing now.</returns>
        public static DateTime GetTimeStamp()
        {
            var result = DateTime.UtcNow.AddSeconds(TimeDifferenceInSeconds);
            return result;
        }

        /// <summary>
        /// Returns a time span from 1 January 1970 until now.
        /// </summary>
        /// <remarks>
        /// By default is called to get DateTime value suitable to pass as the value for an oauth_timestamp parameter.
        /// Basically it is DateTime.UtcNow, however it may add seconds <see cref="TimeDifferenceInSeconds"/> to 
        /// adjust time.
        /// </remarks>
        /// <returns></returns>
        public static TimeSpan GetTimeStampFrom1_1_1970()
        {
            return GetTimeStamp().Subtract(new DateTime(1970, 1, 1));
        }
    }
}
