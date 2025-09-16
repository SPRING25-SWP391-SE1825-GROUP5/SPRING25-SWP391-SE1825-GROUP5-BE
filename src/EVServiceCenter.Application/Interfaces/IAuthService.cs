using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IAuthService
    {
       Task<string> RegisterAsync(AccountRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
    }
}
