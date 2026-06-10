// SmartSolutions.App/Views/LoginWindow.xaml.cs
using System.Windows;
using SmartSolutions.App.ViewModels;

namespace SmartSolutions.App.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private async void OnLoginClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm) return;
        vm.Pin = PinBox.Password;
        await vm.LoginCommand.ExecuteAsync(null);
        PinBox.Clear();
    }
}
