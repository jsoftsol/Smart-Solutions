// SmartSolutions.App/ViewModels/HaierJobsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class HaierJobsViewModel(
    IHaierJobService jobs,
    IServiceProvider services) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<HaierJob> _jobList = [];
    [ObservableProperty] private HaierJobStatus? _filterStatus;
    [ObservableProperty] private HaierJobType?   _filterJobType;
    [ObservableProperty] private bool _isBusy;

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var results = await jobs.GetJobsAsync(FilterStatus, FilterJobType);
            JobList = new(results);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand] private async Task ApplyFiltersAsync() => await LoadAsync();

    [RelayCommand]
    private void OpenNewJob()
    {
        var vm = services.GetRequiredService<HaierJobDetailViewModel>();
        vm.InitNew();
        var main = services.GetRequiredService<MainViewModel>();
        main.CurrentSection = "Haier Jobs";
        main.CurrentView = vm;
    }

    [RelayCommand]
    private void OpenJob(HaierJob job)
    {
        var vm = services.GetRequiredService<HaierJobDetailViewModel>();
        vm.InitEdit(job.Id);
        var main = services.GetRequiredService<MainViewModel>();
        main.CurrentSection = "Haier Jobs";
        main.CurrentView = vm;
    }

    [RelayCommand]
    private async Task DeleteJobAsync(HaierJob job)
    {
        if (!DialogHelper.Confirm($"Delete Haier job #{job.Id}?")) return;
        await jobs.DeleteJobAsync(job.Id);
        JobList.Remove(job);
    }
}
