using MediatR;
using MongoDB.Bson;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Commands
{
    public record FollowUserCommand(string FollowerId, string FollowingId) : IRequest<bool>;

    public class FollowUserCommandHandler(IFollowRepository followRepository, IUserRepository userRepository) : IRequestHandler<FollowUserCommand, bool>
    {
        public async Task<bool> Handle(FollowUserCommand request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.FollowerId, out _))
                throw new ValidationException("Invalid followerId");

            if (!ObjectId.TryParse(request.FollowingId, out _))
                throw new ValidationException("Invalid followingId");

            if (request.FollowerId == request.FollowingId)
                throw new ValidationException("Cannot follow yourself");

            var target = await userRepository.GetByIdAsync(request.FollowingId, cancellationToken);
            if (target is null)
                throw new NotFoundException("User not found", "User", request.FollowingId);

            var exists = await followRepository.ExistsAsync(request.FollowerId, request.FollowingId, cancellationToken);
            if (exists)
                throw new ConflictException("Already following");

            await followRepository.AddAsync(new Follow
            {
                FollowerId = request.FollowerId,
                FollowingId = request.FollowingId,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }
    }
}
