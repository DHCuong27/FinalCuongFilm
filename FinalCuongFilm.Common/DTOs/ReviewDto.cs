using System.ComponentModel.DataAnnotations;

namespace FinalCuongFilm.Common.DTOs
{
	public class ReviewDto
	{
		public Guid Id { get; set; }
		public string UserId { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;
		public Guid MovieId { get; set; }
		public string MovieTitle { get; set; } = string.Empty;
		public int Rating { get; set; }
		public string? Comment { get; set; }
		public bool IsApproved { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}

	public class ReviewCreateDto
	{
		[Required(ErrorMessage = "Vui lòng chọn phim")]
		public Guid MovieId { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn số sao")]
		[Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5 sao")]
		public int Rating { get; set; }

		[MaxLength(1000, ErrorMessage = "Nội dung tối đa 1000 ký tự")]
		public string? Comment { get; set; }
	}

	public class ReviewUpdateDto
	{
		[Required]
		public Guid Id { get; set; }

		[Required]
		[Range(1, 5)]
		public int Rating { get; set; }

		[MaxLength(1000)]
		public string? Comment { get; set; }
	}

	public class MovieRatingDto
	{
		public Guid MovieId { get; set; }
		public string MovieTitle { get; set; } = string.Empty;
		public double AverageRating { get; set; }
		public int TotalReviews { get; set; }
		public int TotalFavorites { get; set; }
		public Dictionary<int, int> RatingDistribution { get; set; } = new();
		// RatingDistribution: { 5: 100, 4: 50, 3: 20, 2: 10, 1: 5 }
	}
}