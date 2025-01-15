public class Order
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Platinum { get; set; }
    public int Quantity { get; set; }
    public int Rank { get; set; }
    public bool Visible { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public User User { get; set; } = new User();
}