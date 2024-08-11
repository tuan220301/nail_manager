namespace NailManager.Models;

public class Menu
{
    private int _quantity;
    public string Image { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1; // Mặc định là 1
}