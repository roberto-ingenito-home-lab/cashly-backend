using cashly.src.Data.Entities;
using cashly.src.DTOs;

namespace cashly.src.Services.Interfaces;

public interface ICategoryService
{
    Task<Category> Create(CategoryCreateDto dto, int userId);

    Task<Category> Update(int categoryId, CategoryUpdateDto dto, int userId);

    Task Delete(int categoryId, int userId);

    Task<IEnumerable<Category>> GetAll(int userId);
}
