using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.IRepositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace EVServiceCenter.Application.Service
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        public AccountService(IAccountRepository accountRepository, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _accountRepository = accountRepository;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public async Task<User?> GetAccountByEmailAsync(string email)
        {
            return await _accountRepository.GetAccountByEmailAsync(email);
        }

        public async Task<User?> GetAccountByPhoneNumberAsync(string phoneNumber)
        {
            return await _accountRepository.GetAccountByPhoneNumberAsync(phoneNumber);
        }


        public async Task<User> CreateAccountAsync(User account)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account), "Account cannot be null.");
            }

            var existingByEmail = await _accountRepository.GetAccountByEmailAsync(account.Email);
            if (existingByEmail != null)
                throw new Exception("Email already exists.");

            // ✅ Kiểm tra trùng số điện thoại
            var existingByPhone = await _accountRepository.GetAccountByPhoneNumberAsync(account.PhoneNumber);
            if (existingByPhone != null)
                throw new Exception("Phone number already exists.");

            account.IsActive = true;
            var createdAccount = await _accountRepository.CreateAccountAsync(account);
            if (createdAccount == null)
            {
                throw new Exception("Failed to create account.");
            }
            return createdAccount;
        }
    }
}
