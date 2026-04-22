using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.ApplicationCore.Entities.Identity; 
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace FinalCuongFilm.Service.Services
{
	public class ReviewService : IReviewService
	{
		private readonly CuongFilmDbContext _context;
		private readonly UserManager<CuongFilmUser> _userManager; 

		public ReviewService(CuongFilmDbContext context, UserManager<CuongFilmUser> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		// Get reviews for a movie, optionally only approved ones

		public async Task<IEnumerable<ReviewDto>> GetMovieReviewsAsync(Guid movieId, bool approvedOnly = true)
		{
			var query = _context.Reviews
				.Include(r => r.Movie)
				.Where(r => r.MovieId == movieId);

			if (approvedOnly)
			{
				query = query.Where(r => r.IsApproved);
			}

			var reviews = await query
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync();

			// Lấy username từ UserManager
			var reviewDtos = new List<ReviewDto>();
			foreach (var review in reviews)
			{
				var user = await _userManager.FindByIdAsync(review.UserId);
				reviewDtos.Add(new ReviewDto
				{
					Id = review.Id,
					UserId = review.UserId,
					UserName = user?.UserName ?? "Unknown",
					MovieId = review.MovieId,
					MovieTitle = review.Movie.Title,
					Rating = review.Rating,
					Comment = review.Comment,
					IsApproved = review.IsApproved,
					CreatedAt = review.CreatedAt,
					UpdatedAt = review.UpdatedAt,
					FullName = user?.FullName ?? "Unknown",
					AvatarUrl = user?.AvatarUrl ?? string.Empty
				});
			}

			return reviewDtos;
		}

		public async Task<IEnumerable<ReviewDto>> GetUserReviewsAsync(string userId)
		{
			var reviews = await _context.Reviews
				.Include(r => r.Movie)
				.Where(r => r.UserId == userId)
				.OrderByDescending(r => r.CreatedAt)
				.ToListAsync();

			var user = await _userManager.FindByIdAsync(userId);
			var userName = user?.UserName ?? "Unknown";

			return reviews.Select(r => new ReviewDto
			{
				Id = r.Id,
				UserId = r.UserId,
				UserName = userName,
				MovieId = r.MovieId,
				MovieTitle = r.Movie.Title,
				Rating = r.Rating,
				Comment = r.Comment,
				IsApproved = r.IsApproved,
				CreatedAt = r.CreatedAt,
				UpdatedAt = r.UpdatedAt
			});
		}

		public async Task<ReviewDto?> GetUserReviewForMovieAsync(string userId, Guid movieId)
		{
			var review = await _context.Reviews
				.Include(r => r.Movie)
				.FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId);

			if (review == null) return null;

			var user = await _userManager.FindByIdAsync(userId);

			return new ReviewDto
			{
				Id = review.Id,
				UserId = review.UserId,
				UserName = user?.UserName ?? "Unknown",
				MovieId = review.MovieId,
				MovieTitle = review.Movie.Title,
				Rating = review.Rating,
				Comment = review.Comment,
				IsApproved = review.IsApproved,
				CreatedAt = review.CreatedAt,
				UpdatedAt = review.UpdatedAt
			};
		}

		public async Task<ReviewDto> CreateReviewAsync(string userId, ReviewCreateDto dto)
		{
			// Check if user already reviewed this movie
			

			// Check movie exists
			var movie = await _context.Movies.FindAsync(dto.MovieId);
			if (movie == null)
			{
				throw new KeyNotFoundException("Film Not found");
			}

			// Check user exists
			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				throw new InvalidOperationException("User invalid!");
			}

			var review = new Review
			{
				UserId = userId,
				MovieId = dto.MovieId,
				Rating = dto.Rating,
				Comment = dto.Comment,
				IsApproved = false,
				CreatedAt = DateTime.UtcNow
			};

			_context.Reviews.Add(review);
			await _context.SaveChangesAsync();

			// Load movie navigation
			await _context.Entry(review).Reference(r => r.Movie).LoadAsync();

			return new ReviewDto
			{
				Id = review.Id,
				UserId = review.UserId,
				UserName = user.UserName ?? "Unknown",
				MovieId = review.MovieId,
				MovieTitle = review.Movie.Title,
				Rating = review.Rating,
				Comment = review.Comment,
				IsApproved = review.IsApproved,
				CreatedAt = review.CreatedAt,
				UpdatedAt = review.UpdatedAt
			};
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
			review.IsApproved = false;

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
				throw new KeyNotFoundException("Film not found");
			}

			var approvedReviews = movie.Reviews.Where(r => r.IsApproved).ToList();
			// Đảm bảo em thêm ".Where(r => r.Rating > 0)" trước khi tính toán
			var validRatings = _context.Reviews.Where(r => r.MovieId == movieId && r.Rating > 0);

			int totalReviews = await validRatings.CountAsync();
			double averageRating = totalReviews > 0 ? await validRatings.AverageAsync(r => r.Rating) : 0;
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
	}
}