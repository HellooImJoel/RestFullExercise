using Microsoft.AspNetCore.Mvc;

namespace RestApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private static readonly Dictionary<string, int> Stock = new()
        {
            ["P001"] = 100,
            ["P002"] = 50
        };

        [HttpGet("check/{productId}/{quantity}")]
        public IActionResult CheckStock(string productId, int quantity)
        {
            var available = Stock.TryGetValue(productId, out var stockQty) && stockQty >= quantity;
            return Ok(new { ProductId = productId, Available = available });
        }

        [HttpPost("order")]
        public IActionResult CreateOrder([FromBody] OrderRequest request)
        {
            if (string.IsNullOrEmpty(request.ProductId))
            {
                return BadRequest(new { Success = false, Message = "ProductId is required." });
            }

            if (Stock.TryGetValue(request.ProductId, out var stockQty) && stockQty >= request.Quantity)
            {
                Stock[request.ProductId] -= request.Quantity;
                return Ok(new { Success = true, Message = "Order created." });
            }
            return BadRequest(new { Success = false, Message = "Insufficient stock." });
        }
    }

    public class OrderRequest
    {
        public string? ProductId { get; set; }
        public int Quantity { get; set; }
    }
}