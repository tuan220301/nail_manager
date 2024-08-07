using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NailManager.Models;

namespace NailManager.Screen;

public partial class ProductsScreen : UserControl
{
    public ObservableCollection<Product> Products { get; set; }
    public ICollectionView FilteredProducts { get; set; }

    public ProductsScreen()
    {
        InitializeComponent();
        Products = new ObservableCollection<Product>
        {
            new Product { ProductId = "Product-123", Name = "Sample product 1", Price = "$20.5", IsChecked = false },
            new Product { ProductId = "Product-124", Name = "Sample product 2", Price = "$25.0", IsChecked = false },
            // Thêm các sản phẩm khác tại đây
        };
        FilteredProducts = CollectionViewSource.GetDefaultView(Products);
        DataContext = this;
    }


    private void OnSearch(object sender, RoutedEventArgs e)
    {
        string searchText = ((Grid)((Button)sender).Parent).Children.OfType<TextBox>().FirstOrDefault()?.Text;
        if (!string.IsNullOrEmpty(searchText))
        {
            FilteredProducts.Filter = product =>
            {
                var p = product as Product;
                return p != null && (p.ProductId.Contains(searchText) || p.Name.Contains(searchText) || p.Price.Contains(searchText));
            };
        }
        else
        {
            FilteredProducts.Filter = null;
        }
        FilteredProducts.Refresh();
    }
}
