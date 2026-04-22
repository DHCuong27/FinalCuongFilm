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

		// =================================================================================
		// API 1: RATE MOVIE - DÀNH RIÊNG CHO VIỆC CLICK ĐÁNH GIÁ SAO (1 User / 1 Lần / 1 Phim)
		// =================================================================================
		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RateMovie(Guid movieId, int rating)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (string.IsNullOrEmpty(userId))
				{
					return Json(new { success = false, message = "Please log in to rate." });
				}

				if (rating < 1 || rating > 5)
				{
					return Json(new { success = false, message = "Invalid rating value." });
				}

				// Kiểm tra xem User này đã từng vote sao cho phim này chưa
				var userReviews = await _reviewService.GetUserReviewsAsync(userId);
				var existingRating = userReviews.FirstOrDefault(r => r.MovieId == movieId && r.Rating > 0);

				if (existingRating != null)
				{
					// Nếu đã vote rồi -> Update lại số điểm mới (Giữ nguyên comment cũ nếu có)
					var updateDto = new ReviewUpdateDto
					{
						Id = existingRating.Id,
						Rating = rating,
						Comment = existingRating.Comment
					};
					await _reviewService.UpdateReviewAsync(userId, updateDto);

					return Json(new { success = true, message = "Your rating has been updated!" });
				}
				else
				{
					// Nếu chưa vote -> Tạo mới một bản ghi chỉ chứa Rating (Comment = null)
					var createDto = new ReviewCreateDto
					{
						MovieId = movieId,
						Rating = rating,
						Comment = null
					};
					var review = await _reviewService.CreateReviewAsync(userId, createDto);
					await _reviewService.ApproveReviewAsync(review.Id);

					return Json(new { success = true, message = "Thanks for rating this movie!" });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error rating movie {MovieId}", movieId);
				return Json(new { success = false, message = "An error occurred while rating." });
			}
		}

		// =================================================================================
		// API 2: CREATE COMMENT - DÀNH RIÊNG CHO BÌNH LUẬN TEXT (Nhiều Comment / 1 User)
		// =================================================================================
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

				// QUAN TRỌNG: Ép Rating = 0 cho tất cả các bình luận dạng Text.
				// Việc này đảm bảo các comment không làm sai lệch thuật toán tính trung bình cộng (Average) của số sao.
				dto.Rating = 0;

				var review = await _reviewService.CreateReviewAsync(userId, dto);

				// Auto approve bình luận ngay lập tức
				await _reviewService.ApproveReviewAsync(review.Id);

				// Lấy tên người dùng chuẩn xác nhất (Ưu tiên FullName)
				var userName = User.FindFirst("FullName")?.Value
							?? User.FindFirst("Name")?.Value
							?? User.Identity?.Name
							?? "Anonymous";

				return Json(new
				{
					success = true,
					message = "Comment posted successfully!",
					review = new
					{
						id = review.Id,
						userName = userName,
						rating = dto.Rating,
						comment = dto.Comment ?? string.Empty,
						createdAt = DateTime.Now.ToString("MMM dd, yyyy")
					}
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating comment.");
				return Json(new { success = false, message = "An error occurred while posting your comment." });
			}
		}

		// =================================================================================
		// API 3: EDIT COMMENT - CẬP NHẬT BÌNH LUẬN
		// =================================================================================
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

				var reviews = await _reviewService.GetUserReviewsAsync(userId);
				var review = reviews.FirstOrDefault(r => r.Id == dto.Id);

				if (review == null)
				{
					return Json(new { success = false, message = "Comment not found or access denied." });
				}

				// Đảm bảo không bị thay đổi Rating khi sửa bình luận
				dto.Rating = review.Rating;

				var result = await _reviewService.UpdateReviewAsync(userId, dto);
				if (result)
				{
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

		// =================================================================================
		// API 4: DELETE COMMENT - XÓA BÌNH LUẬN
		// =================================================================================
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