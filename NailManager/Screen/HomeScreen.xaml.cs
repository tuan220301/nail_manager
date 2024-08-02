using System.Windows;
using System.Windows.Controls;
using NailManager.Layout;

namespace NailManager.Screen;

public partial class HomeScreen : UserControl
{
    public HomeScreen()
    {
        InitializeComponent();
    }
   
    private void ShowToastButton_Click(object sender, RoutedEventArgs e)
    {
        MainLayout.Instance.ShowToast("This is a toast from Page1");
    }

    private void ShowLoadingButton_Click(object sender, RoutedEventArgs e)
    {
        MainLayout.Instance.ShowLoading(true);
    }
}