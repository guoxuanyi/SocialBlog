using AutoMapper;
using SocialBlog.Api.Dtos;
using SocialBlog.Core.Entities;

namespace SocialBlog.Api.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, UserProfileDto>();
            CreateMap<User, PublicUserDto>();
        }
    }
}
