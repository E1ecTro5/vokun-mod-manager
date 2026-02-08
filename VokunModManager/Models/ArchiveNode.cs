using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace VokunModManager.Models;

public class ArchiveNode
{
    public bool IsSelected { get; set; } = false;
    public string Name { get; set; }
    public bool IsFolder { get; set; }

    // public ArchiveNode Parent? <- for checkboxing all/some in the treeview?
    public ObservableCollection<ArchiveNode> Children { get; } = new ObservableCollection<ArchiveNode>();
}
