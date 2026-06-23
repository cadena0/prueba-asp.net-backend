using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using RentasApi.Data;
using RentasApi.DTOs;
using RentasApi.Models;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FavoritesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public FavoritesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyFavorites()
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var favorites = await _db.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Property)
            .ThenInclude(p => p.Images)
            .ToListAsync();

        var dtos = favorites.Select(f => new PropertyDto
        {
            Id = f.Property.Id,
            Title = f.Property.Title,
            Description = f.Property.Description,
            City = f.Property.City,
            PricePerNight = f.Property.PricePerNight,
            OwnerId = f.Property.OwnerId,
            OwnerName = f.Property.Owner?.FullName ?? string.Empty,
            ImageUrls = f.Property.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>()
        });

        return Ok(dtos);
    }

    [HttpPost("{propertyId}")]
    [Authorize]
    public async Task<IActionResult> AddFavorite(int propertyId)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var property = await _db.Properties.FindAsync(propertyId);
        if (property == null) return NotFound(new { message = "Property not found" });

        var existing = await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == propertyId);
        if (existing != null) return BadRequest(new { message = "Already in favorites" });

        var favorite = new Favorite { UserId = userId, PropertyId = propertyId, CreatedAt = DateTime.UtcNow };
        _db.Favorites.Add(favorite);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Added to favorites" });
    }

    [HttpDelete("{propertyId}")]
    [Authorize]
    public async Task<IActionResult> RemoveFavorite(int propertyId)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var favorite = await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.PropertyId == propertyId);
        if (favorite == null) return NotFound();

        _db.Favorites.Remove(favorite);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
