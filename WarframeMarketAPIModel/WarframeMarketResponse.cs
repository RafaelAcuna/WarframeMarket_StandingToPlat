public class WarframeMarketResponse
{
    public string ApiVersion { get; set; } = string.Empty;
    public Order[]? Data { get; set; }
    public object? Error { get; set; }
}