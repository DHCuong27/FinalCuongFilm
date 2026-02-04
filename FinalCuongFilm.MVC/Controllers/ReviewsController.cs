using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Common.DTOs;
using System.Security.Claims;

namespace FinalCuongFilm.MVC.Controllers
{
	public class ReviewsController : Controller
	{
		private readonly IReviewService _reviewService;
		private readonly IMovieService _movieService;
		private readonly ILogger<ReviewsController> _logger;

		public ReviewsController(
			IReviewService reviewService,
			IMovieService movieService,
			ILogger<ReviewsController> logger)
		{
			_reviewService = reviewService;
			_movieService = movieService;
			_logger = logger;
		}

		// GET: /Reviews/Movie/{movieId}
		public async Task<IActionResult> Movie(Guid movieId)
		{
			var movie = await _movieService.GetByIdAsync(movieId);
			if (movie == null)
			{
				return NotFound();
			}

			// ✅ Lấy tất cả reviews (không cần approve)
			var reviews = await _reviewService.GetMovieReviewsAsync(movieId, approvedOnly: false);
			var rating = await _reviewService.GetMovieRatingAsync(movieId);

			ViewBag.Movie = movie;
			ViewBag.Rating = rating;

			return View(reviews);
		}

		// GET: /Reviews/MyReviews
		[Authorize]
		public async Task<IActionResult> MyReviews()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return RedirectToAction("Login", "Account", new { area = "Identity" });
			}

			var reviews = await _reviewService.GetUserReviewsAsync(userId);

			ViewData["Title"] = "Đánh giá của tôi";
			return View(reviews);
		}

		// ✅ POST: /Reviews/Create - AJAX endpoint
		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([FromForm] ReviewCreateDto dto)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (string.IsNullOrEmpty(userId))
				{
					_logger.LogWarning("User tried to create review without being authenticated");
					return Json(new { success = false, message = "Vui lòng đăng nhập" });
				}

				_logger.LogInformation("User {UserId} creating review for movie {MovieId}", userId, dto.MovieId);

				if (!ModelState.IsValid)
				{
					var errors = ModelState.Values
						.SelectMany(v => v.Errors)
						.Select(e => e.ErrorMessage)
						.ToList();
					
					_logger.LogWarning("ModelState invalid: {Errors}", string.Join(", ", errors));
					return Json(new { success = false, message = string.Join(", ", errors) });
				}

				// Validate rating
				if (dto.Rating < 1 || dto.Rating > 5)
				{
					return Json(new { success = false, message = "Rating phải từ 1 đến 5 sao" });
				}

				// Create review
				var review = await _reviewService.CreateReviewAsync(userId, dto);
				_logger.LogInformation("Review created successfully with ID {ReviewId}", review.Id);
				
				// ✅ Auto approve ngay lập tức
				await _reviewService.ApproveReviewAsync(review.Id);
				_logger.LogInformation("Review {ReviewId} auto-approved", review.Id);
				
				// Lấy thông tin user
				var userName = User.Identity?.Name ?? "Anonymous";
				
				return Json(new
				{
					success = true,
					message = "Đánh giá của bạn đã được đăng!",
					review = new
					{
						id = review.Id,
						userName = userName,
						rating = dto.Rating,
						comment = dto.Comment ?? string.Empty,
						createdAt = DateTime.Now.ToString("dd/MM/yyyy")
					}
				});
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogWarning(ex, "Invalid operation when creating review");
				return Json(new { success = false, message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating review for movie {MovieId}. Inner exception: {InnerException}", 
					dto.MovieId, 
					ex.InnerException?.Message ?? "None");
				
				return Json(new 
				{ 
					success = false, 
					message = $"Có lỗi xảy ra: {ex.Message}",
					details = ex.InnerException?.Message 
				});
			}
		}

		// GET: /Reviews/Edit/{id}
		[Authorize]
		public async Task<IActionResult> Edit(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return RedirectToAction("Login", "Account", new { area = "Identity" });
			}

			var reviews = await _reviewService.GetUserReviewsAsync(userId);
			var review = reviews.FirstOrDefault(r => r.Id == id);

			if (review == null)
			{
				return NotFound();
			}

			var movie = await _movieService.GetByIdAsync(review.MovieId);
			ViewBag.Movie = movie;

			var updateDto = new ReviewUpdateDto
			{
				Id = review.Id,
				Rating = review.Rating,
				Comment = review.Comment
			};

			return View(updateDto);
		}

		// POST: /Reviews/Edit/{id}
		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, ReviewUpdateDto dto)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return RedirectToAction("Login", "Account", new { area = "Identity" });
			}

			if (id != dto.Id)
			{
				return NotFound();
			}

			var reviews = await _reviewService.GetUserReviewsAsync(userId);
			var review = reviews.FirstOrDefault(r => r.Id == id);

			if (review == null)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					var result = await _reviewService.UpdateReviewAsync(userId, dto);
					if (result)
					{
						// ✅ Auto approve lại sau khi update
						await _reviewService.ApproveReviewAsync(id);
						
						TempData["Success"] = "Cập nhật đánh giá thành công!";
						
						// Redirect về trang detail của phim
						var movie = await _movieService.GetByIdAsync(review.MovieId);
						return RedirectToAction("Detail", "Movie", new { slug = movie.Slug });
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error updating review {ReviewId}", id);
					ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
				}
			}

			var movieData = await _movieService.GetByIdAsync(review.MovieId);
			ViewBag.Movie = movieData;

			return View(dto);
		}

		// ✅ POST: /Reviews/Delete/{id} - AJAX endpoint
		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (userId == null)
				{
					return Json(new { success = false, message = "Vui lòng đăng nhập" });
				}

				var reviews = await _reviewService.GetUserReviewsAsync(userId);
				var review = reviews.FirstOrDefault(r => r.Id == id);

				if (review == null)
				{
					return Json(new { success = false, message = "Không tìm thấy đánh giá" });
				}

				var result = await _reviewService.DeleteReviewAsync(userId, id);
				if (result)
				{
					return Json(new { success = true, message = "Đã xóa đánh giá" });
				}
				else
				{
					return Json(new { success = false, message = "Không thể xóa đánh giá" });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting review {ReviewId}", id);
				return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
			}
		}
	}
}