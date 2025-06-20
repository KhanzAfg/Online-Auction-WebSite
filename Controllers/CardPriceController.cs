using Microsoft.AspNetCore.Mvc;
using OnlineAuction.Services;

namespace OnlineAuction.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CardPriceController : ControllerBase
    {
        private readonly ICardPriceService _cardPriceService;
        private readonly ILogger<CardPriceController> _logger;

        public CardPriceController(ICardPriceService cardPriceService, ILogger<CardPriceController> logger)
        {
            _cardPriceService = cardPriceService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCardPrice([FromQuery] string cardName, [FromQuery] string category)
        {
            try
            {
                var cardData = await _cardPriceService.GetCardPriceAsync(cardName, category);
                return Ok(cardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting card price for {CardName} in category {Category}", cardName, category);
                return StatusCode(500, "Error fetching card data");
            }
        }
    }
} 