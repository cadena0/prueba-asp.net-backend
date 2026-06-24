using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RentasApi.Data;
using RentasApi.DTOs;
using RentasApi.Models;

namespace RentasApi.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return new AuthResponse { Success = false, Message = "Email already registered." };

        // Determine role: only the configured InitialOwner email becomes OWNER
        var ownerEmail = _config["InitialOwner:Email"];
        var role = (ownerEmail != null && request.Email.Equals(ownerEmail, StringComparison.OrdinalIgnoreCase)) ? "OWNER" : "GUEST";

        // Ensure there is at most one OWNER in the system
        if (role == "OWNER")
        {
            if (await _db.Users.AnyAsync(u => u.Role == "OWNER"))
                return new AuthResponse { Success = false, Message = "El propietario ya fue registrado." };
        }
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = HashPassword(request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        return new AuthResponse { Success = true, Token = token };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return new AuthResponse { Success = false, Message = "Invalid credentials." };

        if (!VerifyPassword(request.Password, user.PasswordHash))
            return new AuthResponse { Success = false, Message = "Invalid credentials." };

        var token = GenerateJwtToken(user);
        return new AuthResponse { Success = true, Token = token };
    }

    private string GenerateJwtToken(User user)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured");
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "60");

        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Email),
            new Claim(ClaimTypes.Role, user.Role ?? "USER")
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }

    private bool VerifyPassword(string password, string stored)
    {
        if (string.IsNullOrEmpty(stored)) return false;
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var computed = pbkdf2.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(computed, hash);
    }
}
