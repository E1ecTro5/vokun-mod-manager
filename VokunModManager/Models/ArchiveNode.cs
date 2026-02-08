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

    [ObservableProperty] private bool? _isSelected;

    partial void OnIsSelectedChanged(bool? value)
    {
        UpdateChildren(value);
        UpdateParent();
    }

    private void UpdateChildren(bool? value)
    {
        if (value == null)
            return;

        foreach (var child in Children)
            child.IsSelected = value;
    }

    private void UpdateParent()
    {
        if (Parent == null)
            return;

        if (Parent.Children.All(c => c.IsSelected == true))
            Parent._isSelected = true;
        else if (Parent.Children.All(c => c.IsSelected == false))
            Parent._isSelected = false;
        else
            Parent._isSelected = null;

        Parent.OnPropertyChanged(nameof(IsSelected));
        Parent.UpdateParent();
    }
}