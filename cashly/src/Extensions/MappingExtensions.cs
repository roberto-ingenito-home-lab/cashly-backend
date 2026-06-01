using cashly.src.Data.Entities;
using cashly.src.DTOs;

namespace cashly.src.Extensions;

public static class MappingExtensions
{
    public static CategoryResponseDto? ToDto(this Category value)
    {
        return new CategoryResponseDto
        {
            CategoryId = value.CategoryId,
            CategoryName = value.CategoryName,
            IconName = value.IconName,
            ColorHex = value.ColorHex,
            UserId = value.UserId,
        };
    }

    public static TransactionResponseDto ToDto(this Transaction value)
    {
        return new TransactionResponseDto
        {
            TransactionId = value.TransactionId,
            Amount = value.Amount,
            Type = value.Type,
            TransactionDate = value.TransactionDate,
            Description = value.Description,
            CategoryId = value.CategoryId,
        };
    }

    public static UserResponseDto ToDto(this User value)
    {
        return new UserResponseDto
        {
            Email = value.Email,
            CreatedAt = value.CreatedAt,
            UserId = value.UserId,
        };
    }
}