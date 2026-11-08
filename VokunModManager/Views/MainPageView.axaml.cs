using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using VokunModManager.Models;

namespace VokunModManager.Views;

// need to make more informative comments here...
public partial class MainPageView : UserControl
{
    private Point? _startPoint; // point of first mouse touch
    private ModPlugin? _draggedItem; // selected plugin (if dragging)

    public MainPageView()
    {
        InitializeComponent();
        
        // subscribe to events
        PluginsListBox.AddHandler(DragDrop.DropEvent, ListBox_Drop);
        PluginsListBox.AddHandler(DragDrop.DragOverEvent, ListBox_DragOver);
    }
    
    private void Row_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.DataContext is ModPlugin plugin)
        {
            _draggedItem = plugin;
            _startPoint = e.GetPosition(PluginsListBox);
        }
    }

    private async void ListBox_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_startPoint == null || _draggedItem == null || sender is not ListBox listBox)
            return;

        var currentPoint = e.GetPosition(listBox);
        var diff = currentPoint - _startPoint.Value;

        if (Math.Abs(diff.X) > 3 || Math.Abs(diff.Y) > 3)
        {
            _startPoint = null;

            var dataObject = new DataObject();
            dataObject.Set("ModPluginFormat", _draggedItem);

            await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Move);
        }
    }
    
    // after mouse release
    private void Row_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _startPoint = null;
    }

    // works as long as you drag the object
    private void ListBox_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("ModPluginFormat"))
        {
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        }
    }

    // works only when object has been dropped (released the mouse button)
    private void ListBox_Drop(object? sender, DragEventArgs e)
    {
        // make sure 
        if (!e.Data.Contains("ModPluginFormat")) return;
        
        var targetVisual = e.Source as Visual;
        var targetContainer = targetVisual?.FindAncestorOfType<ListBoxItem>();

        if (targetContainer?.DataContext is not ModPlugin targetItem) return;
        
        if (!e.Data.Contains("ModPluginFormat") || sender is not ListBox listBox) return;

        // get the collection and move the items
        if (listBox.ItemsSource is System.Collections.ObjectModel.ObservableCollection<ModPlugin> collection)
        {
            var sourceIndex = collection.IndexOf(_draggedItem!);
            var targetIndex = collection.IndexOf(targetItem);

            // if not the same index, then change
            if (sourceIndex >= 0 && targetIndex >= 0 && sourceIndex != targetIndex)
            {
                collection.Move(sourceIndex, targetIndex);
            }
        }

        // end the event and clear the variables
        e.Handled = true;
        _draggedItem = null;
        _startPoint = null;
    }
}