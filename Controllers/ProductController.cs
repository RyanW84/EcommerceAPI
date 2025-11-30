using System.Threading;
using System.Threading.Tasks;
using ECommerceApp.RyanW84.Data.DTO;
using ECommerceApp.RyanW84.Data.Models;
using ECommerceApp.RyanW84.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.RyanW84.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

    // GET /api/products
    [HttpGet]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetProductsAsync(
        [FromQuery] ProductQueryParameters queryParameters,
        CancellationToken cancellationToken = default
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        PaginatedResponseDto<List<Product>> result = await _productService.GetProductsAsync(
            queryParameters,
            cancellationToken
        );
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return Ok(result);
    }

    // GET /api/products/{id}
    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetProductById(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        ApiResponseDto<Product?> result = await _productService.GetProductByIdAsync(
            id,
            cancellationToken
        );
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        if (result.Data == null)
            return NotFound(new { message = "Product not found" });
        return Ok(result);
    }

    // GET /api/products/category/{categoryId}
    [HttpGet("category/{categoryId:int}")]
    [ResponseCache(
        Duration = 60,
        Location = ResponseCacheLocation.Any,
        VaryByQueryKeys = new[] { "categoryId" }
    )]
    public async Task<IActionResult> GetProductsByCategory(
        int categoryId,
        CancellationToken cancellationToken = default
    )
    {
        ApiResponseDto<List<Product>> result = await _productService.GetProductsByCategoryIdAsync(
            categoryId,
            cancellationToken
        );
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return Ok(result);
    }

    // POST /api/products
    [HttpPost]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] ApiRequestDto<Product> request,
        CancellationToken cancellationToken = default
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        ApiResponseDto<Product> result = await _productService.CreateProductAsync(
            request,
            cancellationToken
        );
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);

        return CreatedAtAction(nameof(GetProductById), new { id = result.Data!.ProductId }, result);
    }

    // PUT /api/products/{id}
    [HttpPut("{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> UpdateProduct(
        int id,
        [FromBody] ApiRequestDto<Product> request,
        CancellationToken cancellationToken = default
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        ApiResponseDto<Product> result = await _productService.UpdateProductAsync(
            id,
            request,
            cancellationToken
        );
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return Ok(result);
    }

    // DELETE /api/products/{id}
    [HttpDelete("{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> DeleteProduct(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        ApiResponseDto<bool> result = await _productService.DeleteProductAsync(
            id,
            cancellationToken
        );
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return NoContent();
    }

    // GET /api/products/deleted
    [HttpGet("deleted")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetDeletedProducts(
        CancellationToken cancellationToken = default
    )
    {
        ApiResponseDto<List<Product>> result = await _productService.GetDeletedProductsAsync(
            cancellationToken
        );
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return Ok(result);
    }

    // POST /api/products/{id}/restore
    [HttpPost("{id:int}/restore")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> RestoreProduct(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        ApiResponseDto<bool> result = await _productService.RestoreProductAsync(
            id,
            cancellationToken
        );
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return Ok(result);
    }
}
