using MediatR;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetUserByUsernameQuery(string Username) : IRequest<User>;

    public class GetUserByUsernameQueryHandler(IUserRepository userRepository) : IRequestHandler<GetUserByUsernameQuery, User>
    {
        public async Task<User> Handle(GetUserByUsernameQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ValidationException("Username is required");

            var user = await userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            if (user is null)
                throw new NotFoundException("User not found", "User", request.Username);

            return user;
        }
    }
}
