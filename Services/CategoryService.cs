using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ECommerceApp.RyanW84.Data.DTO;
using ECommerceApp.RyanW84.Data.Models;
using ECommerceApp.RyanW84.Interfaces;
using ECommerceApp.RyanW84.Interfaces.Helpers;

namespace ECommerceApp.RyanW84.Services;

public class CategoryService(ICategoryRepository categoryRepository, ICategoryProcessingHelper categoryProcessingHelper) : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly ICategoryProcessingHelper _categoryProcessingHelper = categoryProcessingHelper;

    public async Task<ApiResponseDto<Category>> CreateCategoryAsync(
        ApiRequestDto<Category> request,
        CancellationToken cancellationToken = default
    )
    {
        var validation = _categoryProcessingHelper.ValidateCreateRequest(request);
        if (validation != null) return validation;

        var name = request.Payload!.Name.Trim();
        if (await _categoryRepository.CategoryExistsAsync(name, cancellationToken))
            return ApiResponseDto<Category>.Failure(
                HttpStatusCode.Conflict,
                $"Category with name '{name}' already exists."
            );

        var category = _categoryProcessingHelper.PrepareForCreate(request.Payload!);

        ApiResponseDto<Category> addResult = await _categoryRepository.AddAsync(
            category,
            cancellationToken
        );
        return addResult.RequestFailed
            ? addResult
            : ApiResponseDto<Category>.Success(addResult.Data, HttpStatusCode.Created);
    }

    public async Task<ApiResponseDto<Category>> GetCategoryAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        ApiResponseDto<Category?> repoResult = await _categoryRepository.GetByIdAsync(
            id,
            cancellationToken
        );
        if (repoResult.RequestFailed || repoResult.Data is null)
            return ApiResponseDto<Category>.Failure(
                HttpStatusCode.NotFound,
                $"Category with id {id} not found."
            );
        return ApiResponseDto<Category>.Success(repoResult.Data);
    }

    public async Task<PaginatedResponseDto<List<Category>>> GetAllCategoriesAsync(
        CategoryQueryParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        parameters ??= new CategoryQueryParameters();

        PaginatedResponseDto<List<Category>> repoResult =
            await _categoryRepository.GetAllCategoriesAsync(parameters, cancellationToken);

        return repoResult.RequestFailed
            ? PaginatedResponseDto<List<Category>>.Failure(
                repoResult.ResponseCode,
                repoResult.ErrorMessage,
                parameters.Page,
                parameters.PageSize
            )
            : PaginatedResponseDto<List<Category>>.Success(
                repoResult.Data ?? [],
                repoResult.CurrentPage,
                repoResult.PageSize,
                repoResult.TotalCount
            );
    }

    public async Task<ApiResponseDto<Category>> UpdateCategoryAsync(
        int id,
        ApiRequestDto<Category> request,
        CancellationToken cancellationToken = default
    )
    {
        if (request?.Payload is null)
            return ApiResponseDto<Category>.Failure(
                HttpStatusCode.BadRequest,
                "Request payload is required."
            );

        ApiResponseDto<Category?> repoResult = await _categoryRepository.GetByIdAsync(
            id,
            cancellationToken
        );
        if (repoResult?.Data is null)
            return ApiResponseDto<Category>.Failure(
                HttpStatusCode.NotFound,
                $"Category with id {id} not found."
            );

        var existing = repoResult.Data!;
        var newName = string.IsNullOrWhiteSpace(request.Payload.Name)
            ? existing.Name
            : request.Payload.Name.Trim();

        if (!string.Equals(newName, existing.Name, StringComparison.OrdinalIgnoreCase))
        {
            var byName = await _categoryRepository.GetByNameAsync(newName, cancellationToken);
            var nameExists =
                !byName.RequestFailed && byName.Data?.CategoryId != existing.CategoryId;
            if (nameExists)
                return ApiResponseDto<Category>.Failure(
                    HttpStatusCode.Conflict,
                    $"Category with name '{newName}' already exists."
                );
        }

        var updatedCategory = _categoryProcessingHelper.PrepareForUpdate(existing, request.Payload, id);

        var updateResult = await _categoryRepository.UpdateAsync(
            updatedCategory,
            cancellationToken
        );
        return updateResult.RequestFailed
            ? updateResult
            : ApiResponseDto<Category>.Success(updateResult.Data);
    }

    public async Task<ApiResponseDto<bool>> DeleteCategoryAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        ApiResponseDto<Category?> repoResult = await _categoryRepository.GetByIdAsync(
            id,
            cancellationToken
        );
        if (repoResult?.Data is null)
            return ApiResponseDto<bool>.Failure(
                HttpStatusCode.NotFound,
                $"Category with id {id} not found."
            );

        ApiResponseDto<bool> delResult = await _categoryRepository.DeleteAsync(
            repoResult.Data.CategoryId,
            cancellationToken
        );
        return delResult.RequestFailed
            ? delResult
            : ApiResponseDto<bool>.Success(true, HttpStatusCode.NoContent);
    }

    public async Task<ApiResponseDto<Category>> GetCategoryByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            return ApiResponseDto<Category>.Failure(HttpStatusCode.BadRequest, "Name is required.");

        ApiResponseDto<Category?> repoResult = await _categoryRepository.GetByNameAsync(
            name.Trim(),
            cancellationToken
        );
        return repoResult.RequestFailed || repoResult.Data is null
            ? ApiResponseDto<Category>.Failure(
                HttpStatusCode.NotFound,
                $"Category '{name}' not found."
            )
            : ApiResponseDto<Category>.Success(repoResult.Data);
    }

    public async Task<ApiResponseDto<List<Category>>> GetDeletedCategoriesAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ApiResponseDto<List<Category>> result =
                await _categoryRepository.GetDeletedCategoriesAsync(cancellationToken);
            return result.RequestFailed
                ? result
                : ApiResponseDto<List<Category>>.Success(result.Data);
        }
        catch (Exception ex)
        {
            return ApiResponseDto<List<Category>>.Failure(
                HttpStatusCode.InternalServerError,
                $"Failed to retrieve deleted categories: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponseDto<bool>> RestoreCategoryAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            ApiResponseDto<bool> result = await _categoryRepository.RestoreAsync(
                id,
                cancellationToken
            );
            return result.RequestFailed ? result : ApiResponseDto<bool>.Success(result.Data);
        }
        catch (Exception ex)
        {
            return ApiResponseDto<bool>.Failure(
                HttpStatusCode.InternalServerError,
                $"Failed to restore category: {ex.Message}"
            );
        }
    }
}
