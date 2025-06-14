using Microsoft.AspNetCore.Mvc;
using RedisCacheDemo.Services;

namespace RedisCacheDemo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController(ProductService service, DistributedLockService lockService) : ControllerBase
{
    private readonly ProductService _service = service;
    private readonly DistributedLockService _lockService = lockService;

    // GET /api/product/{id} - Returns product from cache or DB
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await _service.GetProductAsync(id);
        if (product is null) return NotFound();
        return Ok(product);
    }

    // POST /api/product?id=1&name=Product - Adds or updates product in cache and DB
    [HttpPost]
    public async Task<IActionResult> Post([FromQuery] int id, [FromQuery] string name)
    {
        await _service.AddOrUpdateProductAsync(id, name);
        return Ok();
    }

    // PUT: /api/product/{id}/lock-update
    [HttpPut("{id}/lock-update")]
    public async Task<IActionResult> UpdateWithLock(int id, [FromQuery] string newName)
    {
        var updated = await _service.UpdateProductWithLockAsync(id, newName);

        if (!updated)
        {
            return Conflict("Another operation is in progress. Please try again later.");
        }

        return Ok("Product updated with lock");
    }

}
