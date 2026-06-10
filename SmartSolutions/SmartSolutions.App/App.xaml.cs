// SmartSolutions.App/App.xaml.cs
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutions.App.Helpers;
using SmartSolutions.App.ViewModels;
using SmartSolutions.App.Views;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data;
using SmartSolutions.Data.Entities;

namespace SmartSolutions.App;

public partial class App : Application
{
    private const string AdminUsername = "admin";
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Step 1: Set shutdown mode before showing any window
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Step 2: First-run wizard if setup is required
        if (SettingsManager.IsSetupRequired())
        {
            var wizard = new SetupWizardWindow();
            bool completed = wizard.ShowDialog() == true;
            if (!completed)
            {
                Application.Current.Shutdown();
                return;
            }
        }

        // Steps 3-6: Build host, migrate, seed
        try
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureSmartSolutions()
                .Build();

            await _host.StartAsync();

            await using (var scope = _host.Services.CreateAsyncScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                await using var db = await dbFactory.CreateDbContextAsync();

                await db.Database.MigrateAsync();

                var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

                if (!await auth.AnyUserExistsAsync())
                    await auth.CreateAsync(AdminUsername, "0000");

                var settings = SettingsManager.Load();
                if (settings.FirstRunData is { } frd)
                {
                    if (!await db.BusinessInfos.AnyAsync())
                    {
                        db.BusinessInfos.Add(new BusinessInfo
                        {
                            Id      = 1,
                            Name    = frd.BusinessName,
                            Ntn     = frd.Ntn,
                            Address = frd.Address,
                            Phone1  = frd.Phone1,
                            Phone2  = frd.Phone2,
                            Email   = frd.Email
                        });
                        await db.SaveChangesAsync();
                    }

                    if (!string.IsNullOrWhiteSpace(frd.AdminPin))
                    {
                        var users = await auth.GetAllAsync();
                        var admin = users.FirstOrDefault(u => u.Username == AdminUsername);
                        if (admin is not null)
                            await auth.UpdatePinAsync(admin.Id, frd.AdminPin);
                    }

                    SettingsManager.ClearFirstRunData();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to connect to the database:\n\n{ex.Message}\n\nCheck your connection settings and try again. To reconfigure, delete:\n{SettingsManager.SettingsFilePath}",
                "Smart Solutions — Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown();
            return;
        }

        // Step 7: Show LoginWindow
        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        var vm = (LoginViewModel)loginWindow.DataContext;

        bool loginSucceeded = false;

        vm.LoginSucceeded += user =>
        {
            loginSucceeded = true;
            var session = _host.Services.GetRequiredService<ISessionService>();
            session.Login(user);
            loginWindow.Close();

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.WindowState = WindowState.Maximized;
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
        };

        loginWindow.Closing += (_, _) =>
        {
            if (!loginSucceeded)
                Application.Current.Shutdown();
        };

        loginWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
