using AutoMapper;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AccountRequest, User>();
            CreateMap<User, AccountResponse>();

        }
    }
}
