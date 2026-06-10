// SmartSolutions.App/MainWindow.xaml.cs
using System.Windows;

namespace SmartSolutions.App;

public partial class MainWindow : Window
{
    public MainWindow(ViewModels.MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
