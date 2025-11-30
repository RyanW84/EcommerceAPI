using ECommerceApp.RyanW84.Data.DTO;
using ECommerceApp.RyanW84.Data.Models;
using ECommerceApp.RyanW84.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.RyanW84.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService) => _saleService = saleService;

    // POST /api/sales
    [HttpPost]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Create(
        [FromBody] ApiRequestDto<Sale> request,
        CancellationToken cancellationToken
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        ApiResponseDto<Sale> result = await _saleService.CreateSaleAsync(request, cancellationToken);
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.SaleId }, result);
    }

    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        ApiResponseDto<Sale> result = await _saleService.GetSaleByIdAsync(id, cancellationToken);
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return Ok(result);
    }

    [HttpGet]
    [ResponseCache(
        Duration = 30,
        Location = ResponseCacheLocation.Any,
        VaryByQueryKeys = new[] { "*" }
    )]
    public async Task<IActionResult> GetAll(
        [FromQuery] SaleQueryParameters queryParameters,
        CancellationToken cancellationToken = default
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        PaginatedResponseDto<List<Sale>> result = await _saleService.GetSalesAsync(queryParameters, cancellationToken);
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return Ok(result);
    }

    [HttpGet("with-deleted-products")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetAllWithDeletedProducts(CancellationToken cancellationToken)
    {
        ApiResponseDto<List<Sale>> result = await _saleService.GetHistoricalSalesAsync(cancellationToken);
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return Ok(result);
    }

    [HttpGet("{id:int}/with-deleted-products")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetByIdWithDeletedProducts(
        int id,
        CancellationToken cancellationToken
    )
    {
        ApiResponseDto<Sale> result = await _saleService.GetSaleByIdWithHistoricalProductsAsync(
            id,
            cancellationToken
        );
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return Ok(result);
    }

    // PUT /api/sales/{id}
    [HttpPut("{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ApiRequestDto<Sale> request,
        CancellationToken cancellationToken
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        ApiResponseDto<Sale> result = await _saleService.UpdateSaleAsync(id, request, cancellationToken);
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return Ok(result);
    }

    // DELETE /api/sales/{id}
    [HttpDelete("{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        ApiResponseDto<bool> result = await _saleService.DeleteSaleAsync(id, cancellationToken);
        if (result.RequestFailed)
            return this.FromFailure(result.ResponseCode, result.ErrorMessage);
        return NoContent();
    }
}
