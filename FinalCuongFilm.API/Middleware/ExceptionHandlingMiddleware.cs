using FinalCuongFilm.API.Models.Response;
using System.Net;
using System.Text.Json;

namespace FinalCuongFilm.API.Middleware
{
	public class ExceptionHandlingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<ExceptionHandlingMiddleware> _logger;
		private readonly IHostEnvironment _env;

		public ExceptionHandlingMiddleware(
			RequestDelegate next,
			ILogger<ExceptionHandlingMiddleware> logger,
			IHostEnvironment env)
		{
			_next = next;
			_logger = logger;
			_env = env;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
				await HandleExceptionAsync(context, ex);
			}
		}

		private async Task HandleExceptionAsync(HttpContext context, Exception exception)
		{
			context.Response.ContentType = "application/json";

			var response = exception switch
			{
				ArgumentNullException => new ApiResponse<object>
				{
					Success = false,
					Message = "Invalid request",
					StatusCode = (int)HttpStatusCode.BadRequest,
					Errors = new List<string> { exception.Message }
				},
				UnauthorizedAccessException => new ApiResponse<object>
				{
					Success = false,
					Message = "Unauthorized",
					StatusCode = (int)HttpStatusCode.Unauthorized,
					Errors = new List<string> { exception.Message }
				},
				KeyNotFoundException => new ApiResponse<object>
				{
					Success = false,
					Message = "Resource not found",
					StatusCode = (int)HttpStatusCode.NotFound,
					Errors = new List<string> { exception.Message }
				},
				_ => new ApiResponse<object>
				{
					Success = false,
					Message = "Internal server error",
					StatusCode = (int)HttpStatusCode.InternalServerError,
					Errors = _env.IsDevelopment()
						? new List<string> { exception.Message, exception.StackTrace ?? "" }
						: new List<string> { "An error occurred processing your request" }
				}
			};

			context.Response.StatusCode = response.StatusCode;

			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase
			};

			await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
		}
	}
}