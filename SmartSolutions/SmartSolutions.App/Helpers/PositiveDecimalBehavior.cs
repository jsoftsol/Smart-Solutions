// SmartSolutions.App/Helpers/PositiveDecimalBehavior.cs
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartSolutions.App.Helpers;

public static class PositiveDecimalBehavior
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached("Enabled", typeof(bool),
            typeof(PositiveDecimalBehavior),
            new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject obj) => (bool)obj.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject obj, bool value) => obj.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;
        if ((bool)e.NewValue)
        {
            tb.PreviewTextInput += OnPreviewTextInput;
            DataObject.AddPastingHandler(tb, OnPaste);
        }
        else
        {
            tb.PreviewTextInput -= OnPreviewTextInput;
            DataObject.RemovePastingHandler(tb, OnPaste);
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var tb       = (TextBox)sender;
        var proposed = tb.Text.Insert(tb.SelectionStart, e.Text);
        e.Handled = !Regex.IsMatch(proposed, @"^\d*\.?\d*$");
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string))) { e.CancelCommand(); return; }
        var text = (string)e.DataObject.GetData(typeof(string))!;
        if (!Regex.IsMatch(text, @"^\d*\.?\d*$")) e.CancelCommand();
    }
}
