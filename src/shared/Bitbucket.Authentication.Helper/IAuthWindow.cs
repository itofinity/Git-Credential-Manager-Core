namespace Atlassian_Authentication_Helper_App
{
    public interface IAuthWindow
    {
        bool Success{ get; }

        string Response { get; }
    }
}