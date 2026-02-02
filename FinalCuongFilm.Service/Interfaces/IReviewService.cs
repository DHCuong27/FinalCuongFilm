using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
    public interface IReviewService
    {
        Task<IEnumerable<ReviewDto>> GetMovieReviewsAsync(Guid movieId, bool approvedOnly = true);
        Task<IEnumerable<ReviewDto>> GetUserReviewsAsync(string userId);
        Task<ReviewDto?> GetUserReviewForMovieAsync(string userId, Guid movieId);
        Task<ReviewDto> CreateReviewAsync(string userId, ReviewCreateDto dto);
        Task<bool> UpdateReviewAsync(string userId, ReviewUpdateDto dto);
        Task<bool> DeleteReviewAsync(string userId, Guid reviewId);
        Task<bool> ApproveReviewAsync(Guid reviewId); // Admin
        Task<MovieRatingDto> GetMovieRatingAsync(Guid movieId);
    }
}