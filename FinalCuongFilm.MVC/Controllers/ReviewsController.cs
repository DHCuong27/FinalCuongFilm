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

		public ReviewsController(
			IReviewService reviewService,
			IMovieService movieService)
		{
			_reviewService = reviewService;
			_movieService = movieService;
		}

		// GET: /Reviews/Movie/{movieId}
		public async Task<IActionResult> Movie(Guid movieId)
		{
			var movie = await _movieService.GetByIdAsync(movieId);
			if (movie == null)
			{
				return NotFound();
			}

			var reviews = await _reviewService.GetMovieReviewsAsync(movieId, approvedOnly: true);
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

		// GET: /Reviews/Create?movieId={id}
		[Authorize]
		public async Task<IActionResult> Create(Guid movieId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return RedirectToAction("Login", "Account", new { area = "Identity" });
			}

			var movie = await _movieService.GetByIdAsync(movieId);
			if (movie == null)
			{
				return NotFound();
			}

			// Check if user already reviewed
			var existingReview = await _reviewService.GetUserReviewForMovieAsync(userId, movieId);
			if (existingReview != null)
			{
				TempData["Error"] = "Bạn đã đánh giá phim này rồi!";
				return RedirectToAction("Edit", new { id = existingReview.Id });
			}

			ViewBag.Movie = movie;
			return View(new ReviewCreateDto { MovieId = movieId });
		}

		// POST: /Reviews/Create
		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ReviewCreateDto dto)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return RedirectToAction("Login", "Account", new { area = "Identity" });
			}

			if (ModelState.IsValid)
			{
				try
				{
					var review = await _reviewService.CreateReviewAsync(userId, dto);

					TempData["Success"] = "Đánh giá của bạn đã được gửi và đang chờ duyệt!";
					return RedirectToAction("Details", "Movies", new { id = dto.MovieId });
				}
				catch (InvalidOperationException ex)
				{
					ModelState.AddModelError("", ex.Message);
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
				}
			}

			var movie = await _movieService.GetByIdAsync(dto.MovieId);
			ViewBag.Movie = movie;
			return View(dto);
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

			if (ModelState.IsValid)
			{
				try
				{
					var success = await _reviewService.UpdateReviewAsync(userId, dto);

					if (success)
					{
						TempData["Success"] = "Đánh giá đã được cập nhật!";
						return RedirectToAction(nameof(MyReviews));
					}

					return NotFound();
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
				}
			}

			return View(dto);
		}

		// POST: /Reviews/Delete/{id}
		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return Json(new { success = false, message = "Vui lòng đăng nhập!" });
			}

			try
			{
				var success = await _reviewService.DeleteReviewAsync(userId, id);

				if (success)
				{
					return Json(new { success = true, message = "Đã xóa đánh giá!" });
				}

				return Json(new { success = false, message = "Không tìm thấy đánh giá!" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
			}
		}

		// GET: /Reviews/GetRating/{movieId} (AJAX)
		[HttpGet]
		public async Task<IActionResult> GetRating(Guid movieId)
		{
			try
			{
				var rating = await _reviewService.GetMovieRatingAsync(movieId);
				return Json(new { success = true, data = rating });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}
	}
}