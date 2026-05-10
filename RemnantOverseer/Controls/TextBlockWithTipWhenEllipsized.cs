using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Linq;

namespace RemnantOverseer.Controls;
public class TextBlockWithTipWhenEllipsized : TextBlock
{
    static TextBlockWithTipWhenEllipsized()
    {
        TextProperty.Changed.AddClassHandler<TextBlockWithTipWhenEllipsized>(OnTextChanged);
        TextTrimmingProperty.Changed.AddClassHandler<TextBlockWithTipWhenEllipsized>(OnTextTrimmingChanged);
    }

    public TextBlockWithTipWhenEllipsized() { }

    private static void OnTextChanged(TextBlockWithTipWhenEllipsized tb, AvaloniaPropertyChangedEventArgs e)
    {
        tb.UpdateTip();
    }

    private static void OnTextTrimmingChanged(TextBlockWithTipWhenEllipsized tb, AvaloniaPropertyChangedEventArgs e)
    {
        tb.UpdateTip();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LayoutUpdated += OnLayoutUpdated;
        UpdateTip();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        LayoutUpdated -= OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) => UpdateTip();

    private void UpdateTip()
    {
        var tip = IsEllipsized(this) ? Text : null;
        if (Equals(ToolTip.GetTip(this), tip))
        {
            return;
        }
        ToolTip.SetTip(this, tip);
    }

    private static bool IsEllipsized(TextBlock textBlock)
    {
        if (textBlock.TextTrimming == TextTrimming.None)
        {
            return false;
        }

        var layout = textBlock.TextLayout;
        if (layout == null)
            return false;

        return layout.TextLines.Any(line => line.HasCollapsed);
    }
}
