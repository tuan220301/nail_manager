using System.Windows;
using System.Windows.Controls;
using NailManager.Screen;

namespace NailManager.Layout;

public partial class MainLayout 
{
    public static MainLayout? Instance { get; private set; }

    public MainLayout()
    {
        InitializeComponent();
        Instance = this;
        SetBodyContent(new LoginScreen());
    }

    public void ShowToast(string message)
    {
        ToastNotification.Visibility = Visibility.Visible;
        // Set the toast message here if needed
    }

    public void ShowLoading(bool show)
    {
        LoadingIndicator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    public void SetBodyContent(UserControl control)
    {
        BodyContent.Content = control;
    }

    private void NavigateBtnClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button != null)
        {
            var pageName = button.Tag.ToString();
            NavigateToPage(pageName);
        }
    }

    private void NavigateToPage(string? pageName)
    {
        UserControl page = null!;
        switch (pageName)
        {
            case "Login":
                page = new LoginScreen();
                break;
            case "Home":
                page = new HomeScreen();
                break;
            default:
                // Handle unknown page case if necessary
                break;
        }

        if (page != null)
        {
            SetBodyContent(page);
        }
    }
}