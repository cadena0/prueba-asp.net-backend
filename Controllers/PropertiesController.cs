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
public class PropertiesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PropertiesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var props = await _db.Properties.Include(p => p.Images).Include(p => p.Owner).ToListAsync();
        var dtos = props.Select(p => new PropertyDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            City = p.City,
            PricePerNight = p.PricePerNight,
            OwnerId = p.OwnerId,
            OwnerName = p.Owner?.FullName ?? string.Empty,
            ImageUrls = p.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>()
        });
        return Ok(dtos);
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] PropertySearchRequest request)
    {
        var query = _db.Properties.Include(p => p.Images).Include(p => p.Owner).AsQueryable();

        if (!string.IsNullOrEmpty(request.City))
            query = query.Where(p => p.City.ToLower().Contains(request.City.ToLower()));

        List<PropertyDto> dtos;
        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            // Filtrar propiedades que NO tengan reservas solapadas
            var bookedPropertyIds = await _db.Reservations
                .Where(r => r.StartDate < request.EndDate && request.StartDate < r.EndDate)
                .Select(r => r.PropertyId)
                .Distinct()
                .ToListAsync();

            query = query.Where(p => !bookedPropertyIds.Contains(p.Id));
        }

        var props = await query.ToListAsync();
        dtos = props.Select(p => new PropertyDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            City = p.City,
            PricePerNight = p.PricePerNight,
            OwnerId = p.OwnerId,
            OwnerName = p.Owner?.FullName ?? string.Empty,
            ImageUrls = p.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>()
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var p = await _db.Properties.Include(x => x.Images).Include(x => x.Owner).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        var dto = new PropertyDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            City = p.City,
            PricePerNight = p.PricePerNight,
            OwnerId = p.OwnerId,
            OwnerName = p.Owner?.FullName ?? string.Empty,
            ImageUrls = p.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>()
        };
        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "OWNER")]
    public async Task<IActionResult> Create([FromBody] CreatePropertyRequest request)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var ownerId)) return Unauthorized();

        var prop = new Property
        {
            Title = request.Title,
            Description = request.Description,
            City = request.City,
            PricePerNight = request.PricePerNight,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Properties.Add(prop);
        await _db.SaveChangesAsync();

        if (request.ImageUrls != null && request.ImageUrls.Any())
        {
            foreach (var url in request.ImageUrls)
            {
                _db.PropertyImages.Add(new PropertyImage { ImageUrl = url, PropertyId = prop.Id });
            }
            await _db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(Get), new { id = prop.Id }, new { prop.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "OWNER")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePropertyRequest request)
    {
        var prop = await _db.Properties.FindAsync(id);
        if (prop == null) return NotFound();

        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();
        if (prop.OwnerId != userId) return Forbid();

        if (request.Title != null) prop.Title = request.Title;
        if (request.Description != null) prop.Description = request.Description;
        if (request.City != null) prop.City = request.City;
        if (request.PricePerNight.HasValue) prop.PricePerNight = request.PricePerNight.Value;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "OWNER")]
    public async Task<IActionResult> Delete(int id)
    {
        var prop = await _db.Properties.FindAsync(id);
        if (prop == null) return NotFound();

        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();
        if (prop.OwnerId != userId) return Forbid();

        _db.Properties.Remove(prop);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
