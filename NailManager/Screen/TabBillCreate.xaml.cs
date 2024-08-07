using System.Collections.ObjectModel;
using System.Windows.Controls;
using NailManager.Models;
using Menu = NailManager.Models.Menu;

namespace NailManager.Screen;

public partial class TabBillCreate : UserControl
{
    public ObservableCollection<Menu> MenuItems { get; set; }
    public TabBillCreate()
    {
        InitializeComponent();
        MenuItems = new ObservableCollection<Menu>
        {
            new Menu { Image = "https://i.pinimg.com/564x/57/45/d1/5745d18ea9ab045fc08f1b4db0ef6f80.jpg", Price = "10.00", Name = "Cut Nail" },
            new Menu { Image = "https://i.pinimg.com/564x/57/45/d1/5745d18ea9ab045fc08f1b4db0ef6f80.jpg", Price = "15.00", Name = "Sown nails" },
            new Menu { Image = "https://i.pinimg.com/564x/57/45/d1/5745d18ea9ab045fc08f1b4db0ef6f80.jpg", Price = "20.00", Name = "Manicure" },
            new Menu { Image = "https://i.pinimg.com/564x/57/45/d1/5745d18ea9ab045fc08f1b4db0ef6f80.jpg", Price = "25.00", Name = "Pedicure" },
            new Menu { Image = "https://i.pinimg.com/564x/57/45/d1/5745d18ea9ab045fc08f1b4db0ef6f80.jpg", Price = "30.00", Name = "Spa" },
            new Menu { Image = "https://i.pinimg.com/564x/57/45/d1/5745d18ea9ab045fc08f1b4db0ef6f80.jpg", Price = "35.00", Name = "Massage" },
            new Menu { Image = "https://i.pinimg.com/564x/57/45/d1/5745d18ea9ab045fc08f1b4db0ef6f80.jpg", Price = "40.00", Name = "Facial" },
            new Menu { Image = "https://i.pinimg.com/564x/57/45/d1/5745d18ea9ab045fc08f1b4db0ef6f80.jpg", Price = "45.00", Name = "Waxing" },
            new Menu { Image = "https://i.pinimg.com/564x/57/45/d1/5745d18ea9ab045fc08f1b4db0ef6f80.jpg", Price = "50.00", Name = "Haircut" },
            new Menu { Image = "https://i.pinimg.com/564x/57/45/d1/5745d18ea9ab045fc08f1b4db0ef6f80.jpg", Price = "55.00", Name = "Coloring" }
        };

        // Thiết lập DataContext
        DataContext = this;
    }
}