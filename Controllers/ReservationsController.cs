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
public class ReservationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReservationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Reservations.ToListAsync();
        var dtos = list.Select(r => new ReservationDto
        {
            Id = r.Id,
            PropertyId = r.PropertyId,
            UserId = r.UserId,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            TotalPrice = r.TotalPrice
        });
        return Ok(dtos);
    }

    [HttpGet("byuser")]
    [Authorize]
    public async Task<IActionResult> GetByUser()
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var list = await _db.Reservations.Where(r => r.UserId == userId).ToListAsync();
        var dtos = list.Select(r => new ReservationDto
        {
            Id = r.Id,
            PropertyId = r.PropertyId,
            UserId = r.UserId,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            TotalPrice = r.TotalPrice
        });
        return Ok(dtos);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest req)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var prop = await _db.Properties.FindAsync(req.PropertyId);
        if (prop == null) return BadRequest(new { message = "Property not found" });

        // Minimal overlap check
        var overlap = await _db.Reservations.AnyAsync(r => r.PropertyId == req.PropertyId && r.StartDate < req.EndDate && req.StartDate < r.EndDate);
        if (overlap) return BadRequest(new { message = "Dates overlap with existing reservation" });

        var days = (req.EndDate.Date - req.StartDate.Date).Days;
        if (days <= 0) return BadRequest(new { message = "Invalid date range" });

        var total = prop.PricePerNight * days;

        var res = new Reservation
        {
            PropertyId = req.PropertyId,
            UserId = userId,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            TotalPrice = total,
            CreatedAt = DateTime.UtcNow
        };

        _db.Reservations.Add(res);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = res.Id }, new { res.Id });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Cancel(int id)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var res = await _db.Reservations.FindAsync(id);
        if (res == null) return NotFound();
        if (res.UserId != userId) return Forbid();

        _db.Reservations.Remove(res);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
