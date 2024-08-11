using System.ComponentModel;

namespace NailManager.Models;

public class Product : INotifyPropertyChanged
{
    private bool _isChecked;
    public string ProductId { get; set; }
    public string Name { get; set; }
    public string Price { get; set; }
    public int Quantity { get; set; }

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