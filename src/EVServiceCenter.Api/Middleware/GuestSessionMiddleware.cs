using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using EVServiceCenter.Application.Configurations;

namespace EVServiceCenter.Api.Middleware;

public class GuestSessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<GuestSessionOptions> _options;

    public GuestSessionMiddleware(RequestDelegate next, IOptions<GuestSessionOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var opts = _options.Value;
        var cookieName = opts.CookieName ?? "guest_session_id";

        if (!context.Request.Cookies.ContainsKey(cookieName))
        {
            var value = Guid.NewGuid().ToString("N");
            var sameSiteMode = SameSiteMode.Lax;
            if (string.Equals(opts.SameSite, "Strict", StringComparison.OrdinalIgnoreCase)) sameSiteMode = SameSiteMode.Strict;
            else if (string.Equals(opts.SameSite, "None", StringComparison.OrdinalIgnoreCase)) sameSiteMode = SameSiteMode.None;

            context.Response.Cookies.Append(cookieName, value, new CookieOptions
            {
                HttpOnly = true,
                Secure = opts.SecureOnly,
                SameSite = sameSiteMode,
                Path = string.IsNullOrWhiteSpace(opts.Path) ? "/" : opts.Path,
                Expires = DateTimeOffset.UtcNow.AddMinutes(opts.TtlMinutes > 0 ? opts.TtlMinutes : 43200)
            });
        }

        await _next(context);
    }
}


