using System;
using System.Security.Claims;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateAccessToken(User user, int? customerId, int? staffId, int? technicianId);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        DateTime GetTokenExpiration();
        int GetTokenExpirationInSeconds();
    }
}
