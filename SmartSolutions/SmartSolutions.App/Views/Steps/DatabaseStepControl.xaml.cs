using System.Windows.Controls;
using SmartSolutions.App.ViewModels;

namespace SmartSolutions.App.Views.Steps;

public partial class DatabaseStepControl : UserControl
{
    public DatabaseStepControl() => InitializeComponent();

    private void SqlPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm)
            vm.SqlPassword = SqlPasswordBox.Password;
    }
}
