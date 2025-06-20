using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace OnlineAuction.Services
{
    public class CardPrice
    {
        [JsonPropertyName("usd")]
        public string? Usd { get; set; }

        [JsonPropertyName("eur")]
        public string? Eur { get; set; }

        [JsonPropertyName("tix")]
        public string? Tix { get; set; }
    }

    public class CardData
    {
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public CardPrice? Prices { get; set; }
        public string? SetName { get; set; }
        public string? CardNumber { get; set; }
    }

    public interface ICardPriceService
    {
        Task<CardData> GetCardPriceAsync(string cardName, string category);
    }

    public class CardPriceService : ICardPriceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CardPriceService> _logger;

        public CardPriceService(HttpClient httpClient, ILogger<CardPriceService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<CardData> GetCardPriceAsync(string cardName, string category)
        {
            try
            {
                switch (category.ToLower())
                {
                    case "magic: the gathering":
                        return await GetMagicCardDataAsync(cardName);
                    case "pokémon tcg":
                        return await GetPokemonCardDataAsync(cardName);
                    case "yu-gi-oh!":
                        return await GetYuGiOhCardDataAsync(cardName);
                    default:
                        return new CardData { Name = cardName };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching card data for {CardName} in category {Category}", cardName, category);
                return new CardData { Name = cardName };
            }
        }

        private async Task<CardData> GetMagicCardDataAsync(string cardName)
        {
            var response = await _httpClient.GetAsync($"https://api.scryfall.com/cards/named?fuzzy={Uri.EscapeDataString(cardName)}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var card = JsonSerializer.Deserialize<JsonElement>(content);

            return new CardData
            {
                Name = card.GetProperty("name").GetString() ?? cardName,
                ImageUrl = card.GetProperty("image_uris").GetProperty("normal").GetString() ?? string.Empty,
                Prices = new CardPrice
                {
                    Usd = card.GetProperty("prices").GetProperty("usd").GetString(),
                    Eur = card.GetProperty("prices").GetProperty("eur").GetString(),
                    Tix = card.GetProperty("prices").GetProperty("tix").GetString()
                },
                SetName = card.GetProperty("set_name").GetString(),
                CardNumber = card.GetProperty("collector_number").GetString()
            };
        }

        private async Task<CardData> GetPokemonCardDataAsync(string cardName)
        {
            var response = await _httpClient.GetAsync($"https://api.pokemontcg.io/v2/cards?q=name:\"{Uri.EscapeDataString(cardName)}\"");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            var cards = result.GetProperty("data");
            
            if (cards.GetArrayLength() == 0)
            {
                return new CardData { Name = cardName };
            }

            var card = cards[0];
            var prices = card.GetProperty("cardmarket").GetProperty("prices");

            return new CardData
            {
                Name = card.GetProperty("name").GetString() ?? cardName,
                ImageUrl = card.GetProperty("images").GetProperty("large").GetString() ?? string.Empty,
                Prices = new CardPrice
                {
                    Usd = prices.GetProperty("averageSellPrice").GetDecimal().ToString("0.00"),
                    Eur = prices.GetProperty("trendPrice").GetDecimal().ToString("0.00")
                },
                SetName = card.GetProperty("set").GetProperty("name").GetString(),
                CardNumber = card.GetProperty("number").GetString()
            };
        }

        private async Task<CardData> GetYuGiOhCardDataAsync(string cardName)
        {
            var response = await _httpClient.GetAsync($"https://db.ygoprodeck.com/api/v7/cardinfo.php?name={Uri.EscapeDataString(cardName)}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            var cards = result.GetProperty("data");
            
            if (cards.GetArrayLength() == 0)
            {
                return new CardData { Name = cardName };
            }

            var card = cards[0];
            var cardSets = card.GetProperty("card_sets");
            var firstSet = cardSets[0];

            return new CardData
            {
                Name = card.GetProperty("name").GetString() ?? cardName,
                ImageUrl = card.GetProperty("card_images")[0].GetProperty("image_url").GetString() ?? string.Empty,
                SetName = firstSet.GetProperty("set_name").GetString(),
                CardNumber = firstSet.GetProperty("set_code").GetString()
            };
        }
    }
} 