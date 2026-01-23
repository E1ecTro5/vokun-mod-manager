using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using VokunModManager.Models;

namespace VokunModManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // JUST TESTING THE UI
    [ObservableProperty]
    private ObservableCollection<PathFile> pathFiles;

    public MainWindowViewModel()
    {
        PathFiles = new ObservableCollection<PathFile>
        {
            new() { FileName = "TEST ONE" },
            new() { FileName = "TEST TWO" },
            new() { FileName = "TEST THREE" }
        };
    }
}