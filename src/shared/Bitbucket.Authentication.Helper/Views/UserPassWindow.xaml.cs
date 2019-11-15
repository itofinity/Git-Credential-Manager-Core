using Atlassian_Authentication_Helper_App.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;

namespace Atlassian_Authentication_Helper_App.Views
{
    public class UserPassWindow : AbstractAuthWindow, IAuthWindow
    {
        public UserPassWindow()
        {
            InitializeComponent();
            this.DataContextChanged += (s,e) =>
                {
                    ViewModel.ExitEvent += (s,e) =>
                        {   
                            this.Close();
                        };
                };
        }

        public override bool Success => true;

        public override string Response => $"username=abcde{Environment.NewLine}password=123345";

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public IAuthViewModel ViewModel
        {
            get
            {
                return (IAuthViewModel)this.DataContext;
            }
        }
    }
}