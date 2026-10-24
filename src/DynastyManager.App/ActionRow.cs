using System.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;

namespace DynastyManager.App;

/// <summary>Wrapping action groups retain a visible gap, including after responsive layout updates.</summary>
public sealed class ActionRow : FlexLayout
{
    public ActionRow() { Direction = FlexDirection.Row; Wrap = FlexWrap.Wrap; }

    protected override void OnChildAdded(Element child)
    {
        base.OnChildAdded(child);
        if (child is not Button && child is not Picker) return;
        child.PropertyChanged += OnActionChanged;
        Space((View)child);
    }

    protected override void OnChildRemoved(Element child, int oldLogicalIndex)
    {
        child.PropertyChanged -= OnActionChanged;
        base.OnChildRemoved(child, oldLogicalIndex);
    }

    private static void OnActionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == View.MarginProperty.PropertyName && sender is View view) Space(view);
    }

    private static void Space(View view)
    {
        var margin = view.Margin;
        if (margin.Right < 8 || margin.Bottom < 8)
            view.Margin = new Thickness(margin.Left, margin.Top, Math.Max(8, margin.Right), Math.Max(8, margin.Bottom));
    }
}
