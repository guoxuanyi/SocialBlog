using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record UpdateMyProfileCommand : IRequest<bool>
    {
        public required string UserId { get; init; }
        public string? DisplayName { get; init; }
        public string? Bio { get; init; }
        public string? AvatarUrl { get; init; }
        public string? CoverImageUrl { get; init; }
    }

    public class UpdateMyProfileCommandHandler(IUserRepository userRepository) : IRequestHandler<UpdateMyProfileCommand, bool>
    {
        public async Task<bool> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.UserId, out _))
                throw new ValidationException("Invalid userId");

            var updated = await userRepository.UpdateProfileAsync(
                request.UserId,
                new UserProfileUpdate
                {
                    DisplayName = request.DisplayName,
                    Bio = request.Bio,
                    AvatarUrl = request.AvatarUrl,
                    CoverImageUrl = request.CoverImageUrl
                },
                cancellationToken);

            if (updated is null)
                throw new NotFoundException("User not found", "User", request.UserId);

            return true;
        }
    }
}
