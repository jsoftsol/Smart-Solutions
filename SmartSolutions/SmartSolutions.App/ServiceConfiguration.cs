// SmartSolutions.App/ServiceConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutions.App.Helpers;
using SmartSolutions.App.Services;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Core.Services;
using SmartSolutions.Data;

namespace SmartSolutions.App;

public static class ServiceConfiguration
{
    public static IHostBuilder ConfigureSmartSolutions(this IHostBuilder builder) =>
        builder
            .ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddJsonFile(SettingsManager.SettingsFilePath, optional: false, reloadOnChange: false);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContextFactory<AppDbContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("Default")));

                services.AddSingleton<IAuthService,       AuthService>();
                services.AddSingleton<ISessionService,    SessionService>();
                services.AddSingleton<ILookupService,     LookupService>();
                services.AddSingleton<ICustomerService,   CustomerService>();
                services.AddSingleton<IPrintOrderService, PrintOrderService>();
                services.AddSingleton<IHaierJobService,   HaierJobService>();
                services.AddSingleton<IExpenseService,    ExpenseService>();
                services.AddSingleton<IDashboardService,  DashboardService>();
                services.AddSingleton<IInvoiceService,    InvoiceService>();

                services.AddSingleton<ViewModels.MainViewModel>();
                services.AddTransient<ViewModels.DashboardViewModel>();
                services.AddTransient<ViewModels.PrintOrdersViewModel>();
                services.AddTransient<ViewModels.PrintOrderDetailViewModel>();
                services.AddTransient<ViewModels.HaierJobsViewModel>();
                services.AddTransient<ViewModels.HaierJobDetailViewModel>();
                services.AddTransient<ViewModels.ExpensesViewModel>();
                services.AddTransient<ViewModels.SettingsViewModel>();
                services.AddTransient<ViewModels.ReportsViewModel>();
                services.AddTransient<ViewModels.ItemsViewModel>();
                services.AddTransient<ViewModels.CustomersViewModel>();
                services.AddTransient<ViewModels.VendorsViewModel>();
                services.AddTransient<ViewModels.TechniciansViewModel>();
                services.AddTransient<ViewModels.ExpenseCategoriesViewModel>();
                services.AddTransient<ViewModels.PaymentChannelsViewModel>();
                services.AddTransient<ViewModels.UsersViewModel>();

                services.AddSingleton<MainWindow>();
                services.AddTransient<ViewModels.LoginViewModel>();
                services.AddTransient<Views.LoginWindow>();
            });
}
