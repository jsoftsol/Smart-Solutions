// SmartSolutions.App/Views/UsersView.xaml.cs
using SmartSolutions.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SmartSolutions.App.Views;

public partial class UsersView : UserControl
{
    public UsersView() => InitializeComponent();

    private void OnConfirmAddUserClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsersViewModel vm)
            _ = vm.ConfirmAddUserAsync(vm.NewUsername, AddPinBox.Password);
    }

    private void OnConfirmResetPinClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsersViewModel vm)
            _ = vm.ConfirmResetPinAsync(ResetPinBox.Password);
    }
}
