using System.Windows.Controls;

namespace NailManager.Screen;

public partial class AdminScreen : UserControl
{
    public AdminScreen()
    {
        InitializeComponent();
        DatePickerInputFrom.SelectedDate = DateTime.Today;
        DatePickerInputTo.SelectedDate = DateTime.Today;
    }
}