using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace RemnantOverseer.Utilities;

public class TextBlockTrimmedToolTip
{
    public static readonly AttachedProperty<string?> TipProperty =
        AvaloniaProperty.RegisterAttached<TextBlockTrimmedToolTip, TextBlock, string?>("Tip");

    private static readonly AttachedProperty<bool> IsMonitoringProperty =
        AvaloniaProperty.RegisterAttached<TextBlockTrimmedToolTip, TextBlock, bool>("IsMonitoring");

    static TextBlockTrimmedToolTip()
    {
        TipProperty.Changed.AddClassHandler<TextBlock>((textBlock, _) => OnTipChanged(textBlock));
    }

    public static void SetTip(TextBlock element, string? value)
    {
        element.SetValue(TipProperty, value);
    }

    public static string? GetTip(TextBlock element)
    {
        return element.GetValue(TipProperty);
    }

    private static void OnTipChanged(TextBlock textBlock)
    {
        if (string.IsNullOrEmpty(GetTip(textBlock)))
        {
            StopMonitoring(textBlock);
            ToolTip.SetTip(textBlock, null);
            return;
        }

        StartMonitoring(textBlock);
        QueueUpdate(textBlock);
    }

    private static void StartMonitoring(TextBlock textBlock)
    {
        if (textBlock.GetValue(IsMonitoringProperty))
        {
            return;
        }

        textBlock.SetValue(IsMonitoringProperty, true);
        textBlock.LayoutUpdated += TextBlock_LayoutUpdated;
    }

    private static void StopMonitoring(TextBlock textBlock)
    {
        if (!textBlock.GetValue(IsMonitoringProperty))
        {
            return;
        }

        textBlock.SetValue(IsMonitoringProperty, false);
        textBlock.LayoutUpdated -= TextBlock_LayoutUpdated;
    }

    private static void TextBlock_LayoutUpdated(object? sender, EventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            Update(textBlock);
        }
    }

    private static void QueueUpdate(TextBlock textBlock)
    {
        Dispatcher.UIThread.Post(() => Update(textBlock), DispatcherPriority.Loaded);
    }

    private static void Update(TextBlock textBlock)
    {
        var tip = GetTip(textBlock);
        ToolTip.SetTip(textBlock, !string.IsNullOrEmpty(tip) && IsEllipsized(textBlock) ? tip : null);
    }

    private static bool IsEllipsized(TextBlock textBlock)
    {
        if (textBlock.TextTrimming == TextTrimming.None)
        {
            return false;
        }

        return textBlock.TextLayout.TextLines.Any(line => line.HasCollapsed);
    }
}
