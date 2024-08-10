using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NailManager.Models;

namespace NailManager.Screen;

public partial class TabBillList : UserControl
{
    public ObservableCollection<Bill> Bill { get; set; }
    public ICollectionView FilteredBill { get; set; }
    
    public TabBillList()
    {
        InitializeComponent();

        DatePickerInput.SelectedDate = DateTime.Today;
        
        Bill = new ObservableCollection<Bill>
        {
            new Bill()
            {
                bill_id = 1,
                user_id = 1,
                status = 1,
                customer_name = "Nguyen Van A",
                customer_phone = "0123456789",
                name = "Nail",
                total_price = 30,
            },
            new Bill()
            {
                bill_id = 2,
                user_id = 2,
                status = 0,
                customer_name = "Nguyen Van B",
                customer_phone = "0123456789",
                name = "Nail 2",
                total_price = 25,
            }
        };
        FilteredBill = CollectionViewSource.GetDefaultView(Bill);
        DataContext = this;
    }
}