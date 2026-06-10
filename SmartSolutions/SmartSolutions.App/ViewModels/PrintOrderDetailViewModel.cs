// SmartSolutions.App/ViewModels/PrintOrderDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutions.App.Helpers;
using SmartSolutions.Core.Interfaces;
using SmartSolutions.Data.Entities;
using System.Collections.ObjectModel;

namespace SmartSolutions.App.ViewModels;

public partial class PrintOrderDetailViewModel(
    IPrintOrderService orders,
    ICustomerService customers,
    ILookupService lookup,
    IInvoiceService invoiceService,
    IServiceProvider services) : ObservableObject
{
    // ── Order Header ─────────────────────────────────────────────────────
    [ObservableProperty] private int _orderId;
    [ObservableProperty] private bool _isNew;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _customerSearch = "";
    [ObservableProperty] private ObservableCollection<Customer> _customerSuggestions = [];
    [ObservableProperty] private DateTime _orderDate = DateTime.Today;
    [ObservableProperty] private PrintOrderStatus _orderStatus = PrintOrderStatus.Draft;
    [ObservableProperty] private decimal? _transportationCharges;
    [ObservableProperty] private string _orderNotes = "";

    // ── Line Items ────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<PrintOrderLine> _lines = [];
    [ObservableProperty] private ObservableCollection<ItemCategory> _itemCategories = [];
    [ObservableProperty] private ObservableCollection<ItemName> _itemNames = [];

    [ObservableProperty] private ItemCategory? _newLineCategory;
    [ObservableProperty] private ItemName? _newLineItemName;
    [ObservableProperty] private RateType _newLineRateType = RateType.PerSqft;
    [ObservableProperty] private DimensionUnit _newLineUnit = DimensionUnit.Feet;
    [ObservableProperty] private decimal? _newLineHeight;
    [ObservableProperty] private decimal? _newLineWidth;
    [ObservableProperty] private int _newLineQuantity = 1;
    [ObservableProperty] private decimal _newLineRate;
    [ObservableProperty] private decimal _newLineComputedTotal;
    [ObservableProperty] private bool _newLineDimensionsVisible = true;

    // ── Vendor Assignment ─────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<Vendor> _vendors = [];
    [ObservableProperty] private Vendor? _selectedVendor;
    [ObservableProperty] private DateTime _vendorSentDate = DateTime.Today;
    [ObservableProperty] private DateTime? _vendorExpectedDate;
    [ObservableProperty] private decimal _vendorCost;
    [ObservableProperty] private PrintOrderVendorAssignment? _vendorAssignment;

    // ── Payments ──────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<PrintOrderPayment> _payments = [];
    [ObservableProperty] private ObservableCollection<PaymentChannel> _paymentChannels = [];
    [ObservableProperty] private decimal _newPaymentAmount;
    [ObservableProperty] private PaymentChannel? _newPaymentChannel;
    [ObservableProperty] private DateTime _newPaymentDate = DateTime.Today;
    [ObservableProperty] private string _newPaymentNotes = "";
    [ObservableProperty] private string _duplicatePaymentWarning = "";

    // ── Summary ───────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _orderTotal;
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private decimal _balance;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private bool _isBusy;

    // Computed visibility helpers for XAML BoolToVisibilityConverter
    public bool HasError => ErrorMessage.Length > 0;
    public bool HasDuplicateWarning => DuplicatePaymentWarning.Length > 0;
    public bool HasCustomerSuggestions => CustomerSuggestions.Count > 0;

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnDuplicatePaymentWarningChanged(string value) => OnPropertyChanged(nameof(HasDuplicateWarning));
    partial void OnCustomerSuggestionsChanged(ObservableCollection<Customer> value) => OnPropertyChanged(nameof(HasCustomerSuggestions));

    public void InitNew()
    {
        IsNew = true;
        OrderId = 0;
        OrderDate = DateTime.Today;
        OrderStatus = PrintOrderStatus.Draft;
        _ = LoadLookupsAsync();
    }

    public void InitEdit(int orderId)
    {
        IsNew = false;
        OrderId = orderId;
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            await LoadLookupsAsync();
            if (OrderId > 0)
            {
                var order = await orders.GetOrderWithDetailsAsync(OrderId);
                SelectedCustomer = order.Customer;
                CustomerSearch = order.Customer.Name;
                OrderDate = order.Date.ToLocalTime();
                OrderStatus = order.Status;
                TransportationCharges = order.TransportationCharges;
                OrderNotes = order.Notes ?? "";
                Lines = new(order.Lines);
                Payments = new(order.Payments);
                VendorAssignment = order.VendorAssignments.FirstOrDefault();
                if (VendorAssignment is not null)
                {
                    SelectedVendor = VendorAssignment.Vendor;
                    VendorSentDate = VendorAssignment.SentDate.ToLocalTime();
                    VendorExpectedDate = VendorAssignment.ExpectedDate?.ToLocalTime();
                    VendorCost = VendorAssignment.VendorCost;
                }
                RefreshSummary();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load order: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadLookupsAsync()
    {
        ItemCategories  = new(await lookup.GetItemCategoriesAsync());
        Vendors         = new(await lookup.GetVendorsAsync());
        PaymentChannels = new(await lookup.GetPaymentChannelsAsync());
    }

    // ── Customer search ───────────────────────────────────────────────────

    partial void OnCustomerSearchChanged(string value)
    {
        _ = SearchCustomersAsync(value);
    }

    private async Task SearchCustomersAsync(string query)
    {
        if (query.Length < 2) { CustomerSuggestions = []; return; }
        var results = await customers.SearchCustomersAsync(query);
        CustomerSuggestions = new(results);
    }

    [RelayCommand]
    private void SelectCustomer(Customer customer)
    {
        SelectedCustomer = customer;
        CustomerSearch = customer.Name;
        CustomerSuggestions = [];
    }

    [RelayCommand]
    private async Task CreateCustomerInlineAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerSearch)) return;
        var newCustomer = await customers.AddCustomerAsync(CustomerSearch.Trim(), null, null, null);
        SelectedCustomer = newCustomer;
        CustomerSearch = newCustomer.Name;
        CustomerSuggestions = [];
    }

    // ── New line entry ────────────────────────────────────────────────────

    partial void OnNewLineCategoryChanged(ItemCategory? value)
    {
        if (value is not null) _ = LoadItemNamesForCategoryAsync(value.Id);
    }

    private async Task LoadItemNamesForCategoryAsync(int categoryId)
    {
        ItemNames = new(await lookup.GetItemNamesAsync(categoryId));
        NewLineItemName = null;
    }

    partial void OnNewLineRateTypeChanged(RateType value)
    {
        NewLineDimensionsVisible = value == RateType.PerSqft;
        if (value == RateType.PerPiece) { NewLineHeight = NewLineWidth = null; }
        RecalcNewLineTotal();
    }

    partial void OnNewLineHeightChanged(decimal? value)    => RecalcNewLineTotal();
    partial void OnNewLineWidthChanged(decimal? value)     => RecalcNewLineTotal();
    partial void OnNewLineQuantityChanged(int value)       => RecalcNewLineTotal();
    partial void OnNewLineRateChanged(decimal value)       => RecalcNewLineTotal();
    partial void OnNewLineUnitChanged(DimensionUnit value) => RecalcNewLineTotal();

    private void RecalcNewLineTotal()
    {
        var stub = new PrintOrderLine
        {
            RateType = NewLineRateType, Unit = NewLineUnit,
            Height = NewLineHeight, Width = NewLineWidth,
            Quantity = NewLineQuantity, Rate = NewLineRate
        };
        NewLineComputedTotal = stub.ComputeTotal();
    }

    [RelayCommand]
    private async Task AddLineAsync()
    {
        if (NewLineItemName is null || NewLineQuantity <= 0 || NewLineRate <= 0)
        {
            ErrorMessage = "Item name, quantity > 0, and rate > 0 are required.";
            return;
        }
        if (NewLineRateType == RateType.PerSqft && (NewLineHeight is null or <= 0 || NewLineWidth is null or <= 0))
        {
            ErrorMessage = "Height and width must be > 0 for sqft rate type.";
            return;
        }
        ErrorMessage = "";

        try
        {
            if (OrderId == 0)
            {
                if (SelectedCustomer is null) { ErrorMessage = "Select a customer first."; return; }
                var newOrder = await orders.CreateOrderAsync(
                    SelectedCustomer.Id, OrderDate.ToUniversalTime(), OrderNotes);
                OrderId = newOrder.Id;
                IsNew = false;
            }

            var line = await orders.AddLineAsync(OrderId, NewLineItemName.Id, NewLineRateType,
                NewLineUnit, NewLineHeight, NewLineWidth, NewLineQuantity, NewLineRate);
            line.ItemName = NewLineItemName;
            Lines.Add(line);
            ClearNewLineForm();
            RefreshSummary();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteLineAsync(PrintOrderLine line)
    {
        if (!DialogHelper.Confirm("Delete this line item?")) return;
        try
        {
            await orders.DeleteLineAsync(line.Id);
            Lines.Remove(line);
            RefreshSummary();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void ClearNewLineForm()
    {
        NewLineItemName = null; NewLineHeight = NewLineWidth = null;
        NewLineQuantity = 1; NewLineRate = 0; NewLineComputedTotal = 0;
    }

    // ── Vendor assignment ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveVendorAssignmentAsync()
    {
        if (SelectedVendor is null || OrderId == 0) return;
        try
        {
            VendorAssignment = await orders.SetVendorAssignmentAsync(OrderId, SelectedVendor.Id,
                VendorSentDate.ToUniversalTime(), VendorExpectedDate?.ToUniversalTime(), VendorCost);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task MarkVendorPaidAsync()
    {
        if (VendorAssignment is null) return;
        try
        {
            await orders.MarkVendorPaidAsync(VendorAssignment.Id, DateTime.UtcNow);
            VendorAssignment.VendorPaid = true;
            OnPropertyChanged(nameof(VendorAssignment));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // ── Payments ──────────────────────────────────────────────────────────

    partial void OnNewPaymentAmountChanged(decimal value)
    {
        _ = CheckDuplicateAsync();
    }

    partial void OnNewPaymentDateChanged(DateTime value) => _ = CheckDuplicateAsync();

    private async Task CheckDuplicateAsync()
    {
        if (OrderId == 0 || NewPaymentAmount <= 0) { DuplicatePaymentWarning = ""; return; }
        var isDuplicate = await orders.PaymentDuplicateExistsAsync(
            OrderId, NewPaymentAmount, NewPaymentDate);
        DuplicatePaymentWarning = isDuplicate
            ? "Warning: a payment with this amount already exists for this date."
            : "";
    }

    [RelayCommand]
    private async Task AddPaymentAsync()
    {
        if (NewPaymentAmount <= 0)    { ErrorMessage = "Payment amount must be > 0."; return; }
        if (NewPaymentChannel is null) { ErrorMessage = "Select a payment channel.";  return; }
        if (OrderId == 0)              { ErrorMessage = "Save the order header first."; return; }
        ErrorMessage = "";

        try
        {
            var payment = await orders.AddPaymentAsync(OrderId, NewPaymentAmount,
                NewPaymentChannel.Id, NewPaymentDate.ToUniversalTime(), NewPaymentNotes);
            payment.Channel = NewPaymentChannel;
            Payments.Add(payment);
            NewPaymentAmount = 0; NewPaymentNotes = "";
            DuplicatePaymentWarning = "";
            RefreshSummary();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeletePaymentAsync(PrintOrderPayment payment)
    {
        if (!DialogHelper.Confirm($"Delete payment of PKR {payment.Amount:#,##0.00}?")) return;
        try
        {
            await orders.DeletePaymentAsync(payment.Id);
            Payments.Remove(payment);
            RefreshSummary();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // ── Save order header ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveOrderAsync()
    {
        if (SelectedCustomer is null) { ErrorMessage = "Customer is required."; return; }
        if (OrderStatus == PrintOrderStatus.SentToVendor && VendorAssignment is null)
        {
            ErrorMessage = "Cannot set status to 'Sent to Vendor' without a vendor assignment.";
            return;
        }
        ErrorMessage = "";
        try
        {
            if (OrderId == 0)
            {
                var newOrder = await orders.CreateOrderAsync(
                    SelectedCustomer.Id, OrderDate.ToUniversalTime(), OrderNotes);
                OrderId = newOrder.Id;
                IsNew = false;
            }
            else
            {
                await orders.UpdateOrderHeaderAsync(OrderId, SelectedCustomer.Id,
                    OrderDate.ToUniversalTime(), OrderStatus, TransportationCharges, OrderNotes);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    // ── Navigation back ───────────────────────────────────────────────────

    [RelayCommand]
    private void GoBack()
    {
        var listVm = services.GetRequiredService<PrintOrdersViewModel>();
        _ = listVm.LoadAsync();
        var main = services.GetRequiredService<MainViewModel>();
        main.CurrentSection = "Print Orders";
        main.CurrentView = listVm;
    }

    // ── Print Invoice ─────────────────────────────────────────────────────

    [RelayCommand]
    private async Task PrintInvoiceAsync()
    {
        if (OrderId == 0) { ErrorMessage = "Save the order before printing."; return; }
        ErrorMessage = "";
        try
        {
            await invoiceService.PrintInvoiceAsync(OrderId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Print failed: {ex.Message}";
        }
    }

    // ── Summary ───────────────────────────────────────────────────────────

    private void RefreshSummary()
    {
        OrderTotal = Lines.Sum(l => l.ComputeTotal()) + (TransportationCharges ?? 0);
        TotalPaid  = Payments.Sum(p => p.Amount);
        Balance    = OrderTotal - TotalPaid;
    }
}
