using System.ComponentModel.DataAnnotations;

namespace RentasApi.Models
{
public class User
{
    [Key]
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "USER";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Property> Properties { get; set; } = new List<Property>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
}
