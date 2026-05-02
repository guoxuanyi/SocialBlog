using AutoMapper;
using SocialBlog.Api.Dtos;
using SocialBlog.Core.Entities;

namespace SocialBlog.Api.Mappings
{
    public class CommentMappingProfile : Profile
    {
        public CommentMappingProfile()
        {
            CreateMap<Comment, CommentDto>();
        }
    }
}

