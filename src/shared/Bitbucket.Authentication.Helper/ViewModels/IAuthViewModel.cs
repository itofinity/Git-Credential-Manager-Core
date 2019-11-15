namespace Atlassian_Authentication_Helper_App.ViewModels
{
    public interface IAuthViewModel
    {
        string Response { get; }

        bool Success { get; }
    }
}