using System.ComponentModel;

namespace NailManager.Models;

public class Product : INotifyPropertyChanged
{
    private bool _isChecked;
    public int product_id { get; set; }
    public string product_name { get; set; }
    public int price { get; set; }
    public int branch_id { get; set; }
    public int Quantity { get; set; }
    public string url_image { get; set; } 
    public bool IsChecked
    {
        get { return _isChecked; }
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}