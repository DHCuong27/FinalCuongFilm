using System.ComponentModel.DataAnnotations;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class VipPackage
	{
		public Guid Id { get; set; }

		[Required(ErrorMessage = "Tên gói không được để trống")]
		[MaxLength(100, ErrorMessage = "Tên gói tối đa 100 ký tự")]
		public string Name { get; set; } = null!;

		[Required]
		[Range(1000, 10000000, ErrorMessage = "Giá tiền phải từ 1.000 VNĐ đến 10.000.000 VNĐ")]
		public decimal Price { get; set; }

		[Required]
		[Range(1, 3650, ErrorMessage = "Thời hạn phải từ 1 ngày đến 3650 ngày (10 năm)")]
		public int DurationInDays { get; set; }

		[MaxLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
		public string? Description { get; set; }

		public bool IsActive { get; set; } = true;

		// Thêm trường này để quản lý UI "Most Popular" thay vì hardcode
		public bool IsPopular { get; set; } = false;
	}
}