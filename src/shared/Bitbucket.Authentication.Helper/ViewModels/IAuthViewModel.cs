using System;
using System.Collections.Generic;


namespace Atlassian_Authentication_Helper_App.ViewModels
{
    public interface IAuthViewModel
    {
        event EventHandler ExitEvent;

        void Exit();

        Dictionary<string,string> Output { get; }

        bool Success { get; }
    }
}