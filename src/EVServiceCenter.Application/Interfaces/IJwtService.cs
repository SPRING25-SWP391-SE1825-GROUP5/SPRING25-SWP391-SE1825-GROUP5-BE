using System;
using System.Collections.Generic;
using System.Security.Claims;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal ValidateToken(string token);
        DateTime GetTokenExpiration();
        int GetTokenExpirationInSeconds();
    }
}
