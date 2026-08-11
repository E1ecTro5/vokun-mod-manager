using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VokunModManager.ViewModels;

namespace VokunModManager.Views;

public partial class InstallWindow : Window
{
    public InstallWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (DataContext is InstallWindowViewModel vm)
        {
            vm.HandleWindowClosed();
        }
    }
}