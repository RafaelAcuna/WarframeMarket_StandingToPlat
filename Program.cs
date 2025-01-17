using System.Net.Http.Json;
using System.Text.RegularExpressions;

class Program
{
    private static string API_URL = "https://api.warframe.market/v2/orders/item/";
    private static int MAX_ORDERS_PER_MOD = 5;  // Max standing w/each syndicate is 132,000 and each mod costs 25,000 so there's no point in fetching more than 5 orders per mod.
    private static int DELAY_BETWEEN_REQUESTS = 1000;   // 1 second delay between each request to avoid DDoSing the Warframe Market lol

    private static string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
    private static string modsFolderPath = Path.Combine(appDirectory, "WarframeSyndicateMods");

    static async Task Main(string[] args)
    {
        // Dynamically fetch the list of syndicates from the WarframeSyndicateMods folder.
        List<(string Name, string ModsFile)> syndicates = GetSyndicates();
        if (syndicates.Count == 0)
        {
            Console.WriteLine("No files found in the WarframeSyndicateMods folder. Exiting.");
            return;
        }

        // Prompt the user to select a syndicate.
        Console.WriteLine("Select the syndicate you have standing with:");
        for (int i = 0; i < syndicates.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {syndicates[i].Name}");
        }

        Console.Write("Enter the number of your choice: ");
        if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > syndicates.Count)
        {
            Console.WriteLine("Invalid selection. Exiting.");
            return;
        }

        var selectedSyndicate = syndicates[choice - 1];
        Console.WriteLine($"You selected: {selectedSyndicate.Name}");

        // Read the file and fetch the list of mods for the selected syndicate.
        if (!File.Exists(selectedSyndicate.ModsFile))
        {
            Console.WriteLine($"File {selectedSyndicate.ModsFile} not found. Exiting.");
            return;
        }

        var mods = File.ReadAllLines(Path.Combine(modsFolderPath, Path.GetFileName(selectedSyndicate.ModsFile)));
        if (mods.Length == 0)
        {
            Console.WriteLine($"No mods found in {selectedSyndicate.ModsFile}. Exiting.");
            return;
        }

        Console.WriteLine($"Found {mods.Length} mod(s) to search for in {selectedSyndicate.ModsFile}.");

        // Fetch the orders for the mods from the Warframe Market.
        var orders = await FetchOrders(mods);

        // Print the found orders as messages ready to paste in the ingame chat.
        PrintOrders(orders);

        Console.WriteLine("\nPress Enter to exit...");
        Console.ReadLine();
    }

    // Method to get the list of syndicates dynamically from the WarframeSyndicateMods folder.
    private static List<(string, string)> GetSyndicates()
    {
        var syndicates = new List<(string Name, string ModsFile)>();

        string[] modFiles = Directory.GetFiles(modsFolderPath, "*.txt");

        foreach (var file in modFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            
            // Regex to separate each word from the file name. (Ex. RedVeil -> Red Veil)
            string syndicateName = Regex.Replace(fileName, "(\\B[A-Z])", " $1");

            syndicates.Add((syndicateName, file));
        }

        return syndicates;
    }

    // Method to fetch the buy orders from the Warframe Market.
    private static async Task<List<Order>> FetchOrders(string[] mods)
    {
        var orders = new List<Order>();

        using HttpClient client = new();

        foreach (var mod in mods)
        {
            var url = API_URL + mod.Trim();
            Console.WriteLine($"\nFetching data for mod: {mod}");

            try
            {
                var response = await client.GetFromJsonAsync<WarframeMarketResponse>(url);

                if (response?.Data != null)
                {
                    // Only fetch visible buy orders from online-ingame users who seek to buy rank 0 mods for convenience.
                    var sortedOrders = response.Data
                        .Where(order =>
                            order.Visible &&
                            order.Type == "buy" &&
                            order.User.Status == "ingame" &&
                            order.Rank == 0)
                        .OrderByDescending(order => order.Platinum)
                        .Take(MAX_ORDERS_PER_MOD)
                        .Select(order =>
                        {
                            order.ItemName = FormatItemName(mod);
                            return order;
                        })
                        .ToList();

                    orders.AddRange(sortedOrders);
                }
                else
                {
                    Console.WriteLine($"No data available for mod: {mod}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching data for mod {mod}: {ex.Message}");
            }

            await Task.Delay(DELAY_BETWEEN_REQUESTS);
        }

        return orders;
    }

    // Method to print the list of orders sorted by platinum DESC as messages ready to paste into the ingame chat.
    private static void PrintOrders(List<Order> orders)
    {
        var sortedOrders = orders.OrderByDescending(order => order.Platinum).ToList();

        if (orders.Count > 0)
        {
            Console.WriteLine("\nOrders fetched successfully:");
            foreach (var order in sortedOrders)
            {
                Console.WriteLine($"/w {order.User.IngameName} Hi! I want to sell: \"{order.ItemName} (rank {order.Rank})\" for {order.Platinum} platinum. (warframe.market)");
            }
        }
        else
        {
            Console.WriteLine("\nNo orders found for the selected syndicate.");
        }
    }

    // Method to format the id of the item into the actual name of the item. (Ex. accumulating_whipclaw -> Accumulating Whipclaw)
    public static string FormatItemName(string itemName)
    {
        var formattedName = itemName.Replace("_", " ");

        var words = formattedName.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrEmpty(words[i]))
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }

        return string.Join(" ", words);
    }
}