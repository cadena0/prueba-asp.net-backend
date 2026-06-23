using System.ComponentModel.DataAnnotations;

namespace RentasApi.Models
{
public class Favorite
{
    [Key]
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int PropertyId { get; set; }
    public Property? Property { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
}
