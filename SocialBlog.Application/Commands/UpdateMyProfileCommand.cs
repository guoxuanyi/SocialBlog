using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record UpdateMyProfileCommand(
        string UserId,
        string? DisplayName,
        string? Bio,
        string? AvatarUrl
    ) : IRequest<bool>;

    public class UpdateMyProfileCommandHandler(IUserRepository userRepository) : IRequestHandler<UpdateMyProfileCommand, bool>
    {
        public async Task<bool> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            var updated = await userRepository.UpdateProfileAsync(
                request.UserId,
                request.DisplayName,
                request.Bio,
                request.AvatarUrl,
                cancellationToken);

            if (updated is null)
                throw new NotFoundException("User not found", "User", request.UserId);

            return true;
        }
    }
}
