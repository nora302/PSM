using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PSM.Infrastructure.Identity;

namespace PSM.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<Benutzer> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<Benutzer> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var benutzer =
            await _userManager.FindByNameAsync(request.Benutzername);

        if (benutzer == null ||
            !await _userManager.CheckPasswordAsync(
                benutzer,
                request.Passwort))
        {
            return Unauthorized(new
            {
                message = "Benutzername oder Passwort ist falsch."
            });
        }

        if (!benutzer.IstAktiv)
        {
            return Unauthorized(new
            {
                message = "Benutzer ist nicht aktiv."
            });
        }

        var rollen =
            await _userManager.GetRolesAsync(benutzer);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                benutzer.Id),

            new(
                ClaimTypes.Name,
                benutzer.UserName ?? string.Empty)
        };

        foreach (var rolle in rollen)
        {
            claims.Add(
                new Claim(ClaimTypes.Role, rolle));
        }

        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT Key wurde nicht konfiguriert.");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        var tokenString =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return Ok(new
        {
            token = tokenString,
            benutzer = new
            {
                benutzer.Id,
                benutzer.UserName,
                benutzer.Vorname,
                benutzer.Nachname,
                benutzer.StandortId,
                Rollen = rollen
            }
        });
    }
}

public class LoginRequest
{
    public string Benutzername { get; set; } = string.Empty;

    public string Passwort { get; set; } = string.Empty;
}