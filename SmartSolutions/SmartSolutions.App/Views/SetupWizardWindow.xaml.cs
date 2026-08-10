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

    // AdminPinStepControl stays in the visual tree for the whole wizard (visibility toggled by
    // CurrentStep, not swapped in via a DataTemplate), so Window.Loaded only fires once at
    // startup — before the user ever reaches step 3. IsVisibleChanged fires each time the step
    // is actually shown, which is when the PIN box should receive focus.
    private void AdminPinStep_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
            AdminPinStep.FocusPinInput();
    }
}
