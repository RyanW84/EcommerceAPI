using System.Net;
using ECommerceApp.RyanW84.Data.DTO;
using ECommerceApp.RyanW84.Data.Models;
using ECommerceApp.RyanW84.Interfaces.Helpers;

namespace ECommerceApp.RyanW84.Services.Helpers;

public class ProductProcessingHelper : IProductProcessingHelper
{
    public ApiResponseDto<Product>? ValidateCreateRequest(ApiRequestDto<Product> request)
    {
        if (request.Payload is null)
            return ApiResponseDto<Product>.Failure(HttpStatusCode.BadRequest, "Product data is required");

        return null;
    }

    public Product PrepareForUpdate(Product existing, Product incoming, int id)
    {
        // Enforce route id and preserve price behavior from existing implementation
        return new Product
        {
            ProductId = id,
            Name = string.IsNullOrWhiteSpace(incoming.Name) ? existing.Name : incoming.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(incoming.Description) ? existing.Description : incoming.Description.Trim(),
            Price = existing.Price, // preserve existing price per business rule in original code
            Stock = incoming.Stock == 0 ? existing.Stock : incoming.Stock,
            CategoryId = incoming.CategoryId == 0 ? existing.CategoryId : incoming.CategoryId,
            IsActive = incoming.IsActive == false && existing.IsActive ? false : (incoming.IsActive || existing.IsActive),
            IsDeleted = existing.IsDeleted,
            DeletedAt = existing.DeletedAt,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
