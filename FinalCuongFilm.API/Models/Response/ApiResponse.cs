namespace FinalCuongFilm.API.Models.Response
{
	/// <summary>
	/// Generic API response wrapper
	/// </summary>
	/// <typeparam name="T">Data type</typeparam>
	public class ApiResponse<T>
	{
		public bool Success { get; set; }
		public string Message { get; set; } = string.Empty;
		public T? Data { get; set; }
		public List<string> Errors { get; set; } = new();
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
		public int StatusCode { get; set; }

		public static ApiResponse<T> SuccessResult(T data, string message = "Success", int statusCode = 200)
		{
			return new ApiResponse<T>
			{
				Success = true,
				Message = message,
				Data = data,
				StatusCode = statusCode
			};
		}

		public static ApiResponse<T> FailureResult(string message, List<string>? errors = null, int statusCode = 400)
		{
			return new ApiResponse<T>
			{
				Success = false,
				Message = message,
				Errors = errors ?? new List<string>(),
				StatusCode = statusCode
			};
		}

		public static ApiResponse<T> FailureResult(string message, string error, int statusCode = 400)
		{
			return new ApiResponse<T>
			{
				Success = false,
				Message = message,
				Errors = new List<string> { error },
				StatusCode = statusCode
			};
		}
	}
}