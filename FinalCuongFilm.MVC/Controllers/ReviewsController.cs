using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Common.DTOs;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

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

			ViewData["Title"] = "My Reviews";
			return View(reviews);
		}

		// POST: /Reviews/Create - AJAX endpoint (Đã fix hỗ trợ nhiều Comment & Reply đa tầng)
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
					return Json(new { success = false, message = "Please log in to comment." });
				}

				if (!ModelState.IsValid)
				{
					var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
					return Json(new { success = false, message = string.Join(", ", errors) });
				}

				// Nếu là Comment gốc (không có ParentId), kiểm tra Rating.
				// Nếu là Reply (có ParentId), có thể bỏ qua check Rating hoặc set mặc định là 5 ở Frontend.
				if (!dto.ParentId.HasValue && (dto.Rating < 1 || dto.Rating > 5))
				{
					return Json(new { success = false, message = "Rating must be between 1 and 5 stars." });
				}

				// LƯU Ý CHO CƯỜNG: 
				// Em phải vào file ReviewService.cs, tìm hàm CreateReviewAsync, XÓA BỎ đoạn code ném lỗi 
				// "Bạn đã đánh giá phim này rồi" để hệ thống cho phép 1 user post nhiều comment nhé!
				var review = await _reviewService.CreateReviewAsync(userId, dto);

				// Auto approve ngay lập tức
				await _reviewService.ApproveReviewAsync(review.Id);

				// Lấy tên thật (Ưu tiên FullName nếu hệ thống lưu, nếu không có xài Name mặc định)
				var userName = User.FindFirst("FullName")?.Value
							?? User.FindFirst("Name")?.Value
							?? User.Identity?.Name
							?? "Anonymous";

				return Json(new
				{
					success = true,
					message = dto.ParentId.HasValue ? "Reply posted successfully!" : "Comment posted successfully!",
					review = new
					{
						id = review.Id,
						userName = userName, // Đã lấy đúng tên thật
						rating = dto.Rating,
						comment = dto.Comment ?? string.Empty,
						createdAt = DateTime.Now.ToString("MMM dd, yyyy")
					}
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating review/comment.");
				// Tạm thời hứng lỗi cũ của em nếu em chưa kịp sửa file Service
				if (ex.Message.Contains("đánh giá") || ex.Message.Contains("review"))
				{
					return Json(new { success = false, message = "Please update your old review instead of creating a new one (Or ask Admin to allow multiple comments)." });
				}
				return Json(new { success = false, message = "An error occurred while posting." });
			}
		}

		// POST: /Reviews/Edit/{id} - AJAX endpoint (Đã fix thành trả về JSON cho Frontend In-place Update)
		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(ReviewUpdateDto dto)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (userId == null)
				{
					return Json(new { success = false, message = "Please log in." });
				}

				if (!ModelState.IsValid)
				{
					return Json(new { success = false, message = "Invalid data." });
				}

				// Lấy review ra để check quyền sở hữu
				var reviews = await _reviewService.GetUserReviewsAsync(userId);
				var review = reviews.FirstOrDefault(r => r.Id == dto.Id);

				if (review == null)
				{
					return Json(new { success = false, message = "Comment not found or access denied." });
				}

				var result = await _reviewService.UpdateReviewAsync(userId, dto);
				if (result)
				{
					// Auto approve lại sau khi update
					await _reviewService.ApproveReviewAsync(dto.Id);
					return Json(new { success = true, message = "Comment updated successfully!" });
				}

				return Json(new { success = false, message = "Failed to update comment." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating comment {ReviewId}", dto.Id);
				return Json(new { success = false, message = "An error occurred: " + ex.Message });
			}
		}

		// POST: /Reviews/Delete/{id} - AJAX endpoint
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
					return Json(new { success = false, message = "Please log in." });
				}

				var reviews = await _reviewService.GetUserReviewsAsync(userId);
				var review = reviews.FirstOrDefault(r => r.Id == id);

				if (review == null)
				{
					return Json(new { success = false, message = "Comment not found." });
				}

				var result = await _reviewService.DeleteReviewAsync(userId, id);
				if (result)
				{
					return Json(new { success = true, message = "Comment deleted." });
				}
				else
				{
					return Json(new { success = false, message = "Failed to delete comment." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting comment {ReviewId}", id);
				return Json(new { success = false, message = "An error occurred." });
			}
		}
	}
}