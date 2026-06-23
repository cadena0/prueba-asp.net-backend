namespace RentasApi.DTOs;

public class PropertySearchRequest
{
    public string? City { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
