using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Datalayer;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.Service.Services
{
	public class ReviewService : IReviewService
	{
		private readonly CuongFilmDbContext _context;

		public ReviewService(CuongFilmDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<ReviewDto>> GetMovieReviewsAsync(Guid movieId, bool approvedOnly = true)
		{
			var query = _context.Reviews
				.Include(r => r.User)
				.Include(r => r.Movie)
				.Where(r => r.MovieId == movieId);

			if (approvedOnly)
			{
				query = query.Where(r => r.IsApproved);
			}

			var reviews = await query
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync();

			return reviews.Select(r => MapToDto(r));
		}

		public async Task<IEnumerable<ReviewDto>> GetUserReviewsAsync(string userId)
		{
			var reviews = await _context.Reviews
				.Include(r => r.User)
				.Include(r => r.Movie)
				.Where(r => r.UserId == userId)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync();

			return reviews.Select(r => MapToDto(r));
		}

		public async Task<ReviewDto?> GetUserReviewForMovieAsync(string userId, Guid movieId)
		{
			var review = await _context.Reviews
				.Include(r => r.User)
				.Include(r => r.Movie)
				.FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);

			return review == null ? null : MapToDto(review);
		}

		public async Task<ReviewDto> CreateReviewAsync(string userId, ReviewCreateDto dto)
		{
			// Check if user already reviewed this movie
			var existing = await _context.Reviews
				.FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == dto.MovieId);

			if (existing != null)
			{
				throw new InvalidOperationException("Bạn đã đánh giá phim này rồi! Vui lòng cập nhật đánh giá cũ.");
			}

			// Check movie exists
			var movie = await _context.Movies.FindAsync(dto.MovieId);
			if (movie == null)
			{
				throw new KeyNotFoundException("Không tìm thấy phim!");
			}

			var review = new Review
			{
				UserId = userId,
				MovieId = dto.MovieId,
				Rating = dto.Rating,
				Comment = dto.Comment,
				IsApproved = false, // Chờ admin duyệt
				CreatedAt = DateTime.UtcNow
			};

			_context.Reviews.Add(review);
			await _context.SaveChangesAsync();

			// Load navigation properties
			await _context.Entry(review).Reference(r => r.User).LoadAsync();
			await _context.Entry(review).Reference(r => r.Movie).LoadAsync();

			return MapToDto(review);
		}

		public async Task<bool> UpdateReviewAsync(string userId, ReviewUpdateDto dto)
		{
			var review = await _context.Reviews
				.FirstOrDefaultAsync(r => r.Id == dto.Id && r.UserId == userId);

			if (review == null)
			{
				return false;
			}

			review.Rating = dto.Rating;
			review.Comment = dto.Comment;
			review.UpdatedAt = DateTime.UtcNow;
			review.IsApproved = false; // Reset approval status

			await _context.SaveChangesAsync();

			return true;
		}

		public async Task<bool> DeleteReviewAsync(string userId, Guid reviewId)
		{
			var review = await _context.Reviews
				.FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

			if (review == null)
			{
				return false;
			}

			_context.Reviews.Remove(review);
			await _context.SaveChangesAsync();

			return true;
		}

		public async Task<bool> ApproveReviewAsync(Guid reviewId)
		{
			var review = await _context.Reviews.FindAsync(reviewId);

			if (review == null)
			{
				return false;
			}

			review.IsApproved = true;
			await _context.SaveChangesAsync();

			return true;
		}

		public async Task<MovieRatingDto> GetMovieRatingAsync(Guid movieId)
		{
			var movie = await _context.Movies
				.Include(m => m.Reviews.Where(r => r.IsApproved))
				.Include(m => m.Favorites)
				.FirstOrDefaultAsync(m => m.Id == movieId);

			if (movie == null)
			{
				throw new KeyNotFoundException("Không tìm thấy phim!");
			}

			var approvedReviews = movie.Reviews.Where(r => r.IsApproved).ToList();

			var ratingDistribution = new Dictionary<int, int>
			{
				{ 5, approvedReviews.Count(r => r.Rating == 5) },
				{ 4, approvedReviews.Count(r => r.Rating == 4) },
				{ 3, approvedReviews.Count(r => r.Rating == 3) },
				{ 2, approvedReviews.Count(r => r.Rating == 2) },
				{ 1, approvedReviews.Count(r => r.Rating == 1) }
			};

			return new MovieRatingDto
			{
				MovieId = movie.Id,
				MovieTitle = movie.Title,
				AverageRating = approvedReviews.Any()
					? Math.Round(approvedReviews.Average(r => r.Rating), 1)
					: 0,
				TotalReviews = approvedReviews.Count,
				TotalFavorites = movie.Favorites.Count,
				RatingDistribution = ratingDistribution
			};
		}

		private static ReviewDto MapToDto(Review review)
		{
			return new ReviewDto
			{
				Id = review.Id,
				UserId = review.UserId,
				UserName = review.User?.UserName ?? "",
				MovieId = review.MovieId,
				MovieTitle = review.Movie?.Title ?? "",
				Rating = review.Rating,
				Comment = review.Comment,
				IsApproved = review.IsApproved,
				CreatedAt = review.CreatedAt,
				UpdatedAt = review.UpdatedAt
			};
		}
	}
}