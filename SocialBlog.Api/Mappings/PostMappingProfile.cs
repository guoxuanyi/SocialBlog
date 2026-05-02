using AutoMapper;
using SocialBlog.Core.Entities;
using SocialBlog.Api.Dtos;

namespace SocialBlog.Api.Mappings
{
    public class PostMappingProfile : Profile
    {
        public PostMappingProfile()
        {
            CreateMap<Post, PostDto>();
            CreateMap<PostDto, Post>();
        }
    }
}
