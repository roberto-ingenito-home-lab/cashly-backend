namespace cashly.src.DTOs;

public class CategoryResponseDto
{
    public int CategoryId { get; set; }
    public required string CategoryName { get; set; }
    public string? IconName { get; set; }
    public string? ColorHex { get; set; }
    public int UserId { get; set; }

}