namespace FinalCuongFilm.API.Models.Response
{
	public class PaginatedResult<T>
	{
		public List<T> Items { get; set; } = new();
		public int TotalCount { get; set; }
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
		public bool HasPreviousPage => PageNumber > 1;
		public bool HasNextPage => PageNumber < TotalPages;

		public PaginatedResult(List<T> items, int count, int pageNumber, int pageSize)
		{
			Items = items;
			TotalCount = count;
			PageNumber = pageNumber;
			PageSize = pageSize;
		}

		public static PaginatedResult<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
		{
			var count = source.Count();
			var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
			return new PaginatedResult<T>(items, count, pageNumber, pageSize);
		}
	}
}