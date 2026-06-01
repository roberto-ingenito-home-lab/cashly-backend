using cashly.src.DTOs;
using cashly.src.Extensions;
using cashly.src.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cashly.src.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpDelete("{categoryId}")]
    public async Task<IActionResult> Delete(int categoryId)
    {
        var userId = User.GetUserId();
        await categoryService.Delete(categoryId, userId);

        return Ok(); // 200
    }

    [HttpGet()]
    public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAll()
    {
        var userId = User.GetUserId();
        var categories = await categoryService.GetAll(userId);

        return Ok(categories.Select(e => e.ToDto())); // 200
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponseDto>> Create([FromBody] CategoryCreateDto category)
    {
        var userId = User.GetUserId();

        var newCategory = await categoryService.Create(category, userId);
        return StatusCode(201, newCategory.ToDto());
    }

    [HttpPut("{categoryId}")]
    public async Task<ActionResult<CategoryResponseDto>> Update(int categoryId, [FromBody] CategoryUpdateDto categoryDto)
    {
        var userId = User.GetUserId();

        var updatedCategory = await categoryService.Update(categoryId, categoryDto, userId);
        return Ok(updatedCategory.ToDto()); // 200
    }
}
