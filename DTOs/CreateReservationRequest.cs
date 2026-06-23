namespace RentasApi.DTOs;

public class CreateReservationRequest
{
    public int PropertyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
