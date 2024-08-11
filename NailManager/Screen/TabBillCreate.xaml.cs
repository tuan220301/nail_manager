using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpVectors.Dom.Svg;
using SharpVectors.Renderers.Utils;
using SharpVectors.Renderers.Wpf;
using Menu = NailManager.Models.Menu;

namespace NailManager.Screen;

public partial class TabBillCreate : UserControl
{
    public ObservableCollection<Menu> MenuItems { get; set; }
    public ObservableCollection<Menu> SelectedItems { get; set; }
    private WpfSvgWindow _wpfWindow;
    public TabBillCreate()
    {
        InitializeComponent();
        MenuItems = new ObservableCollection<Menu>
        {
            new Menu { Image = "https://i.pinimg.com/564x/57/45/d1/5745d18ea9ab045fc08f1b4db0ef6f80.jpg", Price = "10.00", Name = "Cut Nail Cut Nail Cut Nail" },
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
        SelectedItems = new ObservableCollection<Menu>();
        // Thiết lập DataContext
        var wpfSettings = new WpfDrawingSettings();
        var wpfRenderer = new WpfDrawingRenderer(wpfSettings);
        _wpfWindow = new WpfSvgWindow(500, 500, wpfRenderer);
        DataContext = this;
    }
    private void OnAddItemClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var selectedItem = button?.DataContext as Menu;
        if (selectedItem != null)
        {
            var existingItem = SelectedItems.FirstOrDefault(item => item.Name == selectedItem.Name);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                selectedItem.Quantity = 1;  // Số lượng mặc định là 1
                SelectedItems.Add(selectedItem);
            }
            TotalPriceChanged();
            RefreshSelectedItems();
        }
    }

    private void OnRemoveItemClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var selectedItem = button?.DataContext as Menu;
        if (selectedItem != null)
        {
            var existingItem = SelectedItems.FirstOrDefault(item => item.Name == selectedItem.Name);
            if (existingItem != null)
            {
                existingItem.Quantity--;
                if (existingItem.Quantity == 0)
                {
                    SelectedItems.Remove(existingItem);
                }
            }
            TotalPriceChanged();
            RefreshSelectedItems();
        }
    }

    private void RefreshSelectedItems()
    {
        ItemsControl itemsControl = FindName("SelectedItemsControl") as ItemsControl;
        if (itemsControl != null)
        {
            itemsControl.Items.Refresh();
        }
    }

    private void TotalPriceChanged()
    {
        var total = 0.0;
        foreach (var item in SelectedItems)
        {
            total += double.Parse(item.Price) * item.Quantity;
        }
        TotalPrice.Text = total + " $";
    }
   
     // Method to create the FlowDocument
    

     private DrawingGroup RenderSvg(string svgFile)
     {
         try
         {
             var wpfSettings = new WpfDrawingSettings();
             var wpfRenderer = new WpfDrawingRenderer(wpfSettings);

             var drawingDocument = new WpfDrawingDocument();
             wpfRenderer.BeginRender(drawingDocument);
             _wpfWindow.LoadDocument(svgFile, wpfSettings);
             var svgDocument = _wpfWindow.Document as SvgDocument;
             wpfRenderer.Render(svgDocument);
             var drawing = wpfRenderer.Drawing;
             wpfRenderer.EndRender();

             return drawing;
         }
         catch (Exception ex)
         {
             MessageBox.Show($"Error loading SVG: {ex.Message}");
             return null;
         }
     }
     
private FlowDocument CreateBillDocument()
{
    string customerName = CustomerNameTextBox.Text;
    string phoneNumber = PhoneNumberTextBox.Text;
    ComboBoxItem selectedStaffItem = StaffComboBox.SelectedItem as ComboBoxItem;
    string staff = selectedStaffItem?.Content.ToString();
    var selectedItems = SelectedItemsControl.ItemsSource as ObservableCollection<Menu>;
    string totalPriceText = TotalPrice.Text.Replace("$", "").Trim();
    int totalPrice = string.IsNullOrWhiteSpace(totalPriceText) ? 0 : int.Parse(totalPriceText);
    // FlowDocument document = new FlowDocument();
    FlowDocument document = new FlowDocument
    {
        PageWidth = 275, // 72mm approximately equals 283 pixels at 96 DPI
        PagePadding = new Thickness(10),
        ColumnWidth = double.PositiveInfinity
    };

    // Add Image (SVG)
    var drawing = RenderSvg("Images/Svgs/logo.svg");
    if (drawing != null)
    {
        var drawingImage = new DrawingImage(drawing);
        Image svgImage = new Image
        {
            Source = drawingImage,
            Width = 100,  // Set the desired width
            Height = 50,  // Set the desired height
            Stretch = Stretch.Uniform
        };

        // Wrap the image in a BlockUIContainer
        BlockUIContainer imageContainer = new BlockUIContainer(svgImage);
        document.Blocks.Add(imageContainer);
    }

    // Title
    Paragraph title = new Paragraph(new Run("Bill Invoice"))
    {
        FontSize = 16,
        FontWeight = FontWeights.Bold,
        TextAlignment = TextAlignment.Center,
        FontFamily = new FontFamily("Roboto"),
    };
    document.Blocks.Add(title);

    // Bill Info
    Paragraph billInfo = new Paragraph(new Run($"InvoiceID: #12345\nTable: A1\nStaff: {staff}\nFrom: {DateTime.Now:dd/MM/yyyy} {DateTime.Now:HH:mm}"))
    {
        FontSize = 12,
        TextAlignment = TextAlignment.Left,
        FontFamily = new FontFamily("Roboto"),
    };
    document.Blocks.Add(billInfo);

    // Table Header
    document.Blocks.Add(CreateHeaderRow(new[] { "NO", "Name", "QTY", "Price", "Total" }));

    // Table Rows
    int index = 1;
    foreach (var item in selectedItems)
    {
        double price = double.Parse(item.Price);
        double total = price * item.Quantity;
        string formattedPrice = price % 1 == 0 ? price.ToString("N0") : price.ToString("N2");
        string formattedTotal = total % 1 == 0 ? total.ToString("N0") : total.ToString("N2");

        document.Blocks.Add(CreateLineRow(new[] 
        { 
            index.ToString(), 
            item.Name, 
            item.Quantity.ToString(), 
            formattedPrice, 
            formattedTotal 
        }));
        index++;
    }

    // Total
    Paragraph totalParagraph = new Paragraph(new Run($"\nTotal: {totalPrice:N0} $"))
    {
        FontSize = 14,
        FontWeight = FontWeights.Bold,
        TextAlignment = TextAlignment.Right,
        FontFamily = new FontFamily("Roboto"),
    };
    document.Blocks.Add(totalParagraph);

    // Store Info
    Paragraph storeInfo = new Paragraph(new Run("CA ZONE - BÌNH THẠNH\nAddress: 32/6 Hẻm 36 Nguyễn Gia Trí, P25, Quận Bình Thạnh, TP.HCM\nHotline: 0325483193"))
    {
        FontSize = 10,
        TextAlignment = TextAlignment.Center,
        FontFamily = new FontFamily("Roboto"),
    };
    document.Blocks.Add(storeInfo);

    return document;
}

private BlockUIContainer CreateHeaderRow(string[] columns)
{
    Border rowBorder = new Border
    {
        BorderBrush = Brushes.Black,
        BorderThickness = new Thickness(1, 1, 1, 1), // No bottom border
        Padding = new Thickness(0),
        Margin = new Thickness(0)
    };

    Grid grid = new Grid();

    // Define specific widths for each column
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });  // Column 0 width
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });  // Column 1 width
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });  // Column 2 width
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });  // Column 3 width
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });  // Column 4 width

    for (int i = 0; i < columns.Length; i++)
    {
        // Create a TextBlock for each header cell
        TextBlock textBlock = new TextBlock
        {
            Text = columns[i],
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(1),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            FontFamily = new FontFamily("Roboto")
        };

        // Determine if a left border should be applied based on column index
        Border cellBorder = new Border
        {
            BorderBrush = Brushes.Black,
            Width = (double)grid.ColumnDefinitions[i].Width.Value,
            BorderThickness = new Thickness(
                left: (i == 1 || i == 2 || i == 3 || i == 4) ? 1 : 0, // Apply left border for columns 1, 2, 3, and 4
                top: 0,
                right: 0,
                bottom: 0), // No bottom border
            Child = textBlock
        };

        Grid.SetColumn(cellBorder, i);
        grid.Children.Add(cellBorder);
    }

    rowBorder.Child = grid;
    return new BlockUIContainer(rowBorder);
}

private BlockUIContainer CreateLineRow(string[] columns)
{
    Border rowBorder = new Border
    {
        BorderBrush = Brushes.Black,
        BorderThickness = new Thickness(1, 0, 1, 1), // No top border
        Padding = new Thickness(0),
        Margin = new Thickness(0)
    };

    Grid grid = new Grid();

    // Define specific widths for each column
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25) });  // Column 0 width
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });  // Column 1 width
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });  // Column 2 width
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });  // Column 3 width
    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });  // Column 4 width

    for (int i = 0; i < columns.Length; i++)
    {
        // Create a TextBlock for each line cell
        TextBlock textBlock = new TextBlock
        {
            Text = columns[i],
            FontWeight = FontWeights.Normal,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(1),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            FontFamily = new FontFamily("Roboto")
        };

        // Determine if a left border should be applied based on column index
        Border cellBorder = new Border
        {
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(
                left: (i == 1 || i == 2 || i == 3 || i == 4) ? 1 : 0, // Apply left border for columns 1, 2, 3, and 4
                top: 0, // No top border
                right: 0,
                bottom: 0), // Bottom border
            Child = textBlock
        };

        Grid.SetColumn(cellBorder, i);
        grid.Children.Add(cellBorder);
    }

    rowBorder.Child = grid;
    return new BlockUIContainer(rowBorder);
}



    // Method to preview the FlowDocument
    private void PreviewBill(FlowDocument document)
    {
        Window previewWindow = new Window
        {
            Title = "Bill Preview",
            Width = 300,
            Height = 400,
            Content = new FlowDocumentScrollViewer
            {
                Document = document,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible
            }
        };
        previewWindow.ShowDialog();
    }

    // Event handler for the Preview button
    private void PreviewBill_Click(object sender, RoutedEventArgs e)
    {
        FlowDocument document = CreateBillDocument();
        PreviewBill(document);
    }

    // Event handler for the Print button
    private void PrintBill_Click(object sender, RoutedEventArgs e)
    {
        FlowDocument document = CreateBillDocument();

        PrintDialog printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            IDocumentPaginatorSource idpSource = document;
            printDialog.PrintDocument(idpSource.DocumentPaginator, "Bill Print");
        }
    }

}