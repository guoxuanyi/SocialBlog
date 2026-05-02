using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetUserByIdQuery(string UserId) : IRequest<User>;

    public class GetUserByIdQueryHandler(IUserRepository userRepository) : IRequestHandler<GetUserByIdQuery, User>
    {
        public async Task<User> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user is null)
                throw new NotFoundException("User not found", "User", request.UserId);

            return user;
        }
    }
}
