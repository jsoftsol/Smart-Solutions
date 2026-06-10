using System.Windows.Controls;
using SmartSolutions.App.ViewModels;

namespace SmartSolutions.App.Views.Steps;

public partial class AdminPinStepControl : UserControl
{
    public AdminPinStepControl() => InitializeComponent();

    private void PinBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm)
            vm.AdminPin = PinBox.Password;
    }

    private void ConfirmPinBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm)
            vm.AdminPinConfirm = ConfirmPinBox.Password;
    }

    public void ClearPins()
    {
        PinBox.Password        = "";
        ConfirmPinBox.Password = "";
    }
}
