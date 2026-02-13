using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VokunModManager.Models;

public class ArchiveNode : ObservableObject
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public bool IsDirectory { get; set; }

    public ObservableCollection<ArchiveNode> Children { get; } = new();

    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            SetProperty(ref _isChecked, value);
            
            foreach (var child in Children) child.IsChecked = value;
            Parent?.UpdateFromChildren();
        }
    }

    public ArchiveNode? Parent { get; set; }

    private void UpdateFromChildren()
    {
        if (Children.All(c => c.IsChecked)) _isChecked = true;
        else _isChecked = false;

        OnPropertyChanged(nameof(IsChecked));
        Parent?.UpdateFromChildren();
    }
}
