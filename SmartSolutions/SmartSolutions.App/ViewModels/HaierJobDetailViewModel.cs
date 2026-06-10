// SmartSolutions.App/ViewModels/HaierJobDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class HaierJobDetailViewModel(
    IHaierJobService jobs,
    ICustomerService customers,
    ILookupService lookup,
    IServiceProvider services) : ObservableObject
{
    [ObservableProperty] private int _jobId;
    [ObservableProperty] private bool _isNew;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _customerSearch = "";
    [ObservableProperty] private ObservableCollection<Customer> _customerSuggestions = [];
    [ObservableProperty] private bool _hasCustomerSuggestions;
    [ObservableProperty] private string _acModel = "";
    [ObservableProperty] private string _acSerial = "";
    [ObservableProperty] private string _problemDescription = "";
    [ObservableProperty] private ObservableCollection<Technician> _technicians = [];
    [ObservableProperty] private Technician? _selectedTechnician;
    [ObservableProperty] private HaierJobType _jobType = HaierJobType.OutOfWarranty;
    [ObservableProperty] private HaierJobStatus _jobStatus = HaierJobStatus.Pending;
    [ObservableProperty] private string _claimReferenceNumber = "";
    [ObservableProperty] private bool _isWarrantyJob;
    [ObservableProperty] private string _partsUsed = "";
    [ObservableProperty] private decimal _partsCost;
    [ObservableProperty] private DateTime _jobDate = DateTime.Today;
    [ObservableProperty] private string _jobNotes = "";
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _hasError;

    // Payments
    [ObservableProperty] private ObservableCollection<HaierJobPayment> _payments = [];
    [ObservableProperty] private ObservableCollection<PaymentChannel> _paymentChannels = [];
    [ObservableProperty] private decimal _newPaymentAmount;
    [ObservableProperty] private PaymentChannel? _newPaymentChannel;
    [ObservableProperty] private DateTime _newPaymentDate = DateTime.Today;
    [ObservableProperty] private string _newPaymentNotes = "";
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private decimal _balance;
    [ObservableProperty] private bool _isBusy;

    public void InitNew()
    {
        IsNew = true; JobId = 0; JobDate = DateTime.Today;
        _ = LoadLookupsAsync();
    }

    public void InitEdit(int jobId)
    {
        IsNew = false; JobId = jobId;
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            await LoadLookupsAsync();
            if (JobId > 0)
            {
                var job = await jobs.GetJobWithDetailsAsync(JobId);
                SelectedCustomer = job.Customer;
                CustomerSearch = job.Customer.Name;
                AcModel = job.AcModel;
                AcSerial = job.AcSerial ?? "";
                ProblemDescription = job.ProblemDescription;
                SelectedTechnician = job.Technician;
                JobType = job.JobType;
                JobStatus = job.Status;
                IsWarrantyJob = job.JobType == HaierJobType.Warranty;
                ClaimReferenceNumber = job.ClaimReferenceNumber ?? "";
                PartsUsed = job.PartsUsed ?? "";
                PartsCost = job.PartsCost;
                JobDate = job.Date.ToLocalTime();
                JobNotes = job.Notes ?? "";
                Payments = new(job.Payments);
                RefreshSummary();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load job: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            Technicians     = new(await lookup.GetTechniciansAsync());
            PaymentChannels = new(await lookup.GetPaymentChannelsAsync());
        }
        catch (Exception ex)
        {
            SetError($"Failed to load options: {ex.Message}");
        }
    }

    partial void OnJobTypeChanged(HaierJobType value) =>
        IsWarrantyJob = value == HaierJobType.Warranty;

    partial void OnCustomerSearchChanged(string value)
    {
        if (value.Length >= 2) _ = SearchCustomersAsync(value);
        else CustomerSuggestions = [];
    }

    partial void OnCustomerSuggestionsChanged(ObservableCollection<Customer> value) =>
        HasCustomerSuggestions = value.Count > 0;

    private async Task SearchCustomersAsync(string query)
    {
        try
        {
            CustomerSuggestions = new(await customers.SearchCustomersAsync(query));
        }
        catch
        {
            CustomerSuggestions = [];
        }
    }

    [RelayCommand]
    private void SelectCustomer(Customer c)
    {
        SelectedCustomer = c;
        CustomerSearch = c.Name;
        CustomerSuggestions = [];
    }

    [RelayCommand]
    private async Task CreateCustomerInlineAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerSearch)) return;
        try
        {
            var c = await customers.AddCustomerAsync(CustomerSearch.Trim(), null, null, null);
            SelectedCustomer = c;
            CustomerSearch = c.Name;
            CustomerSuggestions = [];
        }
        catch (Exception ex)
        {
            SetError($"Failed to create customer: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveJobAsync()
    {
        if (SelectedCustomer is null)           { SetError("Customer is required."); return; }
        if (string.IsNullOrWhiteSpace(AcModel)) { SetError("AC model is required."); return; }
        if (SelectedTechnician is null)          { SetError("Technician is required."); return; }
        if (string.IsNullOrWhiteSpace(ProblemDescription)) { SetError("Problem description is required."); return; }
        ClearError();

        try
        {
            if (IsNew)
            {
                var newJob = await jobs.CreateJobAsync(SelectedCustomer.Id, AcModel.Trim(),
                    NullIfEmpty(AcSerial), ProblemDescription.Trim(), SelectedTechnician.Id,
                    JobType, NullIfEmpty(ClaimReferenceNumber), NullIfEmpty(PartsUsed),
                    PartsCost, JobDate.ToUniversalTime(), NullIfEmpty(JobNotes));
                JobId = newJob.Id;
                IsNew = false;
            }
            else
            {
                await jobs.UpdateJobAsync(JobId, SelectedCustomer.Id, AcModel.Trim(),
                    NullIfEmpty(AcSerial), ProblemDescription.Trim(), SelectedTechnician.Id,
                    JobType, JobStatus, NullIfEmpty(ClaimReferenceNumber), NullIfEmpty(PartsUsed),
                    PartsCost, JobDate.ToUniversalTime(), NullIfEmpty(JobNotes));
            }
        }
        catch (Exception ex)
        {
            SetError($"Save failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddPaymentAsync()
    {
        if (NewPaymentAmount <= 0)     { SetError("Amount must be > 0."); return; }
        if (NewPaymentChannel is null) { SetError("Select a channel.");   return; }
        if (JobId == 0)                { SetError("Save the job first."); return; }
        ClearError();

        try
        {
            var payment = await jobs.AddPaymentAsync(JobId, NewPaymentAmount,
                NewPaymentChannel.Id, NewPaymentDate.ToUniversalTime(), NullIfEmpty(NewPaymentNotes));
            payment.Channel = NewPaymentChannel;
            Payments.Add(payment);
            NewPaymentAmount = 0;
            NewPaymentNotes = "";
            RefreshSummary();
        }
        catch (Exception ex)
        {
            SetError($"Payment failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeletePaymentAsync(HaierJobPayment payment)
    {
        if (!DialogHelper.Confirm($"Delete payment of PKR {payment.Amount:#,##0.00}?")) return;
        await jobs.DeletePaymentAsync(payment.Id);
        Payments.Remove(payment);
        RefreshSummary();
    }

    [RelayCommand]
    private void GoBack()
    {
        var listVm = services.GetRequiredService<HaierJobsViewModel>();
        _ = listVm.LoadAsync();
        var main = services.GetRequiredService<MainViewModel>();
        main.CurrentSection = "Haier Jobs";
        main.CurrentView = listVm;
    }

    private void RefreshSummary()
    {
        TotalPaid = Payments.Sum(p => p.Amount);
        Balance   = PartsCost - TotalPaid;
    }

    private void SetError(string msg) { ErrorMessage = msg; HasError = true; }
    private void ClearError()         { ErrorMessage = ""; HasError = false; }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
