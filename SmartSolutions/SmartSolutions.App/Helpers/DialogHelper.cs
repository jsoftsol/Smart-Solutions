// SmartSolutions.App/Helpers/DialogHelper.cs
using System.Windows;

namespace SmartSolutions.App.Helpers;

public static class DialogHelper
{
    public static bool Confirm(string message, string title = "Confirm Delete") =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
            == MessageBoxResult.Yes;
}
