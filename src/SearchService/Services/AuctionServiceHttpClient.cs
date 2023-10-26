using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Services;

public class AuctionServiceHttpClient
{
  private readonly HttpClient _client;
  private readonly IConfiguration _config;

  public AuctionServiceHttpClient(HttpClient client, IConfiguration config)
  {
    _client = client;
    _config = config;
  }

  public async Task<List<Item>> GetItemsForSearchDB()
  {
    var lastUpdated = await DB.Find<Item, string>()
      .Sort(x => x.Descending(x => x.UpdatedAt))
      .Project(x => x.UpdatedAt.ToString())
      .ExecuteFirstAsync();

    return await _client.GetFromJsonAsync<List<Item>>
      (_config["AuctionServiceUrl"] + "/api/auctions?date=" + lastUpdated);
  }
}
