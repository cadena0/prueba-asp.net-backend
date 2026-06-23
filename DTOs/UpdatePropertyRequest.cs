namespace RentasApi.DTOs;

public class UpdatePropertyRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? City { get; set; }
    public decimal? PricePerNight { get; set; }
}
