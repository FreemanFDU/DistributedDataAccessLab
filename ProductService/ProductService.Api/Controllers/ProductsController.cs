using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductService.Api.Data;
using ProductService.Api.Models;

namespace ProductService.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductDbContext _context;

    public ProductsController(ProductDbContext context)
    {
        _context = context;
    }

    // ✅ GET ALL
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _context.Products.ToListAsync();
        return Ok(products);
    }

    // ✅ GET BY ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    // ✅ CREATE
    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
            new { id = product.Id }, product);
    }

    // ✅ UPDATE
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Product updated)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        product.Name = updated.Name;
        product.Price = updated.Price;
        product.Stock = updated.Stock;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ✅ DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ✅ ✅ ✅ 新增：同步扣库存接口
    [HttpPost("{id}/decrease")]
    public async Task<IActionResult> DecreaseStock(int id, [FromQuery] int quantity)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound();

        if (product.Stock < quantity)
            return BadRequest("Insufficient stock.");

        product.Stock -= quantity;
        await _context.SaveChangesAsync();

        return Ok(product);
    }
}