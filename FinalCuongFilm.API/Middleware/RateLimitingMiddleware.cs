using System.Collections.Concurrent;
using System.Net;
using FinalCuongFilm.API.Models.Response;
using System.Text.Json;

namespace FinalCuongFilm.API.Middleware
{
	/// <summary>
	/// Simple in-memory rate limiter: max 100 requests per minute per IP
	/// </summary>
	public class RateLimitingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<RateLimitingMiddleware> _logger;
		private static readonly ConcurrentDictionary<string, RateLimitCounter> _counters = new();
		private const int MaxRequests = 100;
		private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

		public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
			var now = DateTime.UtcNow;

			var counter = _counters.AddOrUpdate(clientIp,
				_ => new RateLimitCounter { Count = 1, WindowStart = now },
				(_, existing) =>
				{
					if (now - existing.WindowStart > Window)
						return new RateLimitCounter { Count = 1, WindowStart = now };
					existing.Count++;
					return existing;
				});

			context.Response.Headers["X-RateLimit-Limit"] = MaxRequests.ToString();
			context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, MaxRequests - counter.Count).ToString();

			if (counter.Count > MaxRequests)
			{
				_logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
				context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
				context.Response.ContentType = "application/json";

				var response = ApiResponse<object>.FailureResult(
					"Too many requests. Please try again later.",
					"Rate limit exceeded",
					(int)HttpStatusCode.TooManyRequests
				);

				await context.Response.WriteAsync(
					JsonSerializer.Serialize(response, new JsonSerializerOptions
					{
						PropertyNamingPolicy = JsonNamingPolicy.CamelCase
					})
				);
				return;
			}

			await _next(context);
		}
	}

	public class RateLimitCounter
	{
		public int Count { get; set; }
		public DateTime WindowStart { get; set; }
	}
}