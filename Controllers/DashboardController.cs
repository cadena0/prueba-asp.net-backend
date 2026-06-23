using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using RentasApi.Data;
using RentasApi.DTOs;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "OWNER")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(userIdClaim, out var ownerId)) return Unauthorized();

        var properties = await _db.Properties.Where(p => p.OwnerId == ownerId).ToListAsync();
        var reservations = await _db.Reservations
            .Where(r => properties.Select(p => p.Id).Contains(r.PropertyId))
            .ToListAsync();

        var totalEarnings = reservations.Sum(r => r.TotalPrice);
        var totalReservations = reservations.Count;

        // Calcular ocupación: días reservados / (días totales posibles en últimos 365 días)
        var daysPossible = properties.Count * 365;
        var daysBooked = reservations.Sum(r => (int)(r.EndDate - r.StartDate).TotalDays);
        var occupancyRate = daysPossible > 0 ? (daysBooked * 100.0m / daysPossible) : 0;

        var propertyMetrics = properties.Select(p => {
            var propReservations = reservations.Where(r => r.PropertyId == p.Id).ToList();
            var propEarnings = propReservations.Sum(r => r.TotalPrice);
            var propDaysBooked = propReservations.Sum(r => (int)(r.EndDate - r.StartDate).TotalDays);
            var propOccupancy = propDaysBooked > 0 ? (propDaysBooked * 100.0m / 365) : 0;

            return new PropertyMetricsDto
            {
                Id = p.Id,
                Title = p.Title,
                ReservationCount = propReservations.Count,
                Earnings = propEarnings,
                OccupancyRate = propOccupancy
            };
        }).ToList();

        var metrics = new DashboardMetricsDto
        {
            TotalProperties = properties.Count,
            TotalEarnings = totalEarnings,
            OccupancyRate = occupancyRate,
            TotalReservations = totalReservations,
            Properties = propertyMetrics
        };

        return Ok(metrics);
    }
}
