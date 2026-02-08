using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VokunModManager.Models;

public partial class ArchiveNode : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public bool IsFolder { get; set; }

    public ArchiveNode? Parent { get; set; }
    public ObservableCollection<ArchiveNode> Children { get; } = new();

    [ObservableProperty] private bool _isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        UpdateChildren(value);
    }

    private void UpdateChildren(bool value)
    {
        foreach (var child in Children)
        {
            child.IsSelected = value;
        }
    }
}