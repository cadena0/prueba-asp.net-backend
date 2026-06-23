namespace RentasApi.DTOs;

public class DashboardMetricsDto
{
    public int TotalProperties { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal OccupancyRate { get; set; } // Porcentaje 0-100
    public int TotalReservations { get; set; }
    public List<PropertyMetricsDto> Properties { get; set; } = new List<PropertyMetricsDto>();
}

public class PropertyMetricsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public decimal Earnings { get; set; }
    public decimal OccupancyRate { get; set; }
}
