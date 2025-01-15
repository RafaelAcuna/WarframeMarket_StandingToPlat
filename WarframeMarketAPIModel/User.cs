public class User
{
    public string Id { get; set; } = string.Empty;
    public string IngameName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int Reputation { get; set; }
    public string Platform { get; set; } = string.Empty;
    public bool Crossplay { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Activity? Activity { get; set; }
    public DateTime LastSeen { get; set; }
}