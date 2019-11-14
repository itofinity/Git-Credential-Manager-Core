namespace Bitbucket
{
    public interface IBitbucket
    {
        bool IsCloud { get; }

        string BbSConsumerKey { get; }

        string BbSConsumerSecret { get; }
    }
}