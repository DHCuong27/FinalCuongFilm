namespace FinalCuongFilm.Common.DTOs
{
	public class PagedResult<T>
	{
		public List<T> Items { get; set; } = new List<T>(); // Danh sách dữ liệu của trang hiện tại
		public int TotalCount { get; set; } // Tổng số bản ghi trong Database
		public int PageIndex { get; set; } // Trang hiện tại
		public int PageSize { get; set; } // Số bản ghi trên 1 trang

		// Tự động tính tổng số trang
		public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
		public bool HasPreviousPage => PageIndex > 1;
		public bool HasNextPage => PageIndex < TotalPages;
	}
}