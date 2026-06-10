using System.Windows;
using SmartSolutions.App.ViewModels;

namespace SmartSolutions.App.Views;

public partial class SetupWizardWindow : Window
{
    public SetupWizardWindow()
    {
        InitializeComponent();
        var vm = new SetupWizardViewModel();
        vm.SetupCompleted += () => DialogResult = true;
        DataContext = vm;
    }

    // Click handler (not Command) because it must call ClearPins() on a named control — only code-behind can reach it without breaking MVVM.
    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        AdminPinStep.ClearPins();
        if (DataContext is SetupWizardViewModel vm)
            vm.FinishWithSkip();
    }
}
