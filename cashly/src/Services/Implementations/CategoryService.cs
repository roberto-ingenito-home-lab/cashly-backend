using System.Net;
using cashly.src.Data;
using cashly.src.Data.Entities;
using cashly.src.DTOs;
using cashly.src.Exceptions;
using cashly.src.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace cashly.src.Services.Implementations;

public class CategoryService(AppDbContext dbContext) : ICategoryService
{
    public async Task<Category> Create(CategoryCreateDto dto, int userId)
    {
        Category newCategory = new()
        {
            CategoryName = dto.CategoryName,
            IconName = dto.IconName,
            ColorHex = dto.ColorHex,
            IsHidden = dto.IsHidden,
            UserId = userId,
        };

        dbContext.Categories.Add(newCategory);

        await dbContext.SaveChangesAsync();

        return newCategory;
    }

    public async Task<Category> Update(int categoryId, CategoryUpdateDto dto, int userId)
    {
        // Trova la categoria esistente
        var category =
            await dbContext.Categories.FirstOrDefaultAsync(c =>
                c.CategoryId == categoryId && c.UserId == userId
            ) ?? throw new AppException("category-not-found", HttpStatusCode.NotFound);

        // Aggiorna i campi
        category.CategoryName = dto.CategoryName;
        category.IconName = dto.IconName;
        category.ColorHex = dto.ColorHex;
        if (dto.IsHidden.HasValue)
        {
            category.IsHidden = dto.IsHidden.Value;
        }

        // Salva le modifiche
        await dbContext.SaveChangesAsync();

        return category;
    }

    public async Task Delete(int categoryId, int userId)
    {
        // Setta a null CategoryId sulle transazioni e abbonamenti collegati prima di eliminare la categoria
        await dbContext.Transactions
            .Where(t => t.CategoryId == categoryId && t.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.CategoryId, (int?)null));

        await dbContext.Subscriptions
            .Where(s => s.CategoryId == categoryId && s.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(s => s.CategoryId, (int?)null));

        await dbContext
            .Categories.Where(c => c.CategoryId == categoryId && c.UserId == userId)
            .ExecuteDeleteAsync();
    }

    public async Task<IEnumerable<Category>> GetAll(int userId)
    {
        return await dbContext.Categories.Where(c => c.UserId == userId).ToListAsync();
    }
}
