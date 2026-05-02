using MediatR;
using MongoDB.Bson;
using SocialBlog.Application.Responses;
using SocialBlog.Core.Entities;
using SocialBlog.Core.Exceptions;
using SocialBlog.Core.Interfaces;

namespace SocialBlog.Application.Queries
{
    public record GetCommentsByPostQuery(
        string PostId,
        int Skip = 0,
        int Limit = 20
    ) : IRequest<PaginatedResult<Comment>>;

    public class GetCommentsByPostQueryHandler : IRequestHandler<GetCommentsByPostQuery, PaginatedResult<Comment>>
    {
        private readonly IPostRepository _postRepository;
        private readonly ICommentRepository _commentRepository;

        public GetCommentsByPostQueryHandler(IPostRepository postRepository, ICommentRepository commentRepository)
        {
            _postRepository = postRepository;
            _commentRepository = commentRepository;
        }

        public async Task<PaginatedResult<Comment>> Handle(GetCommentsByPostQuery request, CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(request.PostId, out _))
                throw new ValidationException("Invalid postId");

            var post = await _postRepository.GetByIdAsync(request.PostId, cancellationToken);
            if (post == null)
                throw new NotFoundException("Post not found", "Post", request.PostId);

            var itemsTask = _commentRepository.GetByPostIdAsync(request.PostId, request.Skip, request.Limit, cancellationToken);
            var totalTask = _commentRepository.CountByPostIdAsync(request.PostId, cancellationToken);

            await Task.WhenAll(itemsTask, totalTask);
            return new PaginatedResult<Comment>(itemsTask.Result, totalTask.Result);
        }
    }
}

