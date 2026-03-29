using FinalCuongFilm.API.Extensions;
using FinalCuongFilm.API.Middleware;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Service.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── 1. ĐĂNG KÝ SERVICES (DEPENDENCY INJECTION) 

// Database
builder.Services.AddDbContext<CuongFilmDbContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("CuongFilmConnection")));

builder.Services.AddDbContext<CuongFilmIdentityDbContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("CuongFilmIdentityConnection")));

// Controllers + JSON options
builder.Services.AddControllers()
	.AddNewtonsoftJson(options =>
	{
		options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
		options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
	});

// Application services
builder.Services.AddApplicationServices(builder.Configuration);

// Đăng ký HttpClient cho TmdbService
builder.Services.AddHttpClient<ITmdbService, TmdbService>();
// Đăng ký Service xử lý Import
builder.Services.AddScoped<IMovieImportService, MovieImportService>();

// Swagger
builder.Services.AddSwaggerDocumentation();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS
builder.Services.AddCorsPolicy();

// API Explorer
builder.Services.AddEndpointsApiExplorer();

// Health Checks
builder.Services.AddHealthChecks()
	.AddSqlServer(
		builder.Configuration.GetConnectionString("CuongFilmConnection")!,
		name: "sqlserver",
		failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);

// Response Compression
builder.Services.AddResponseCompression();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();


// =========================================================================
// KHÓA CONTAINER VÀ BUILD APP
var app = builder.Build();
// =========================================================================


// ── 2. MIDDLEWARE PIPELINE (ĐÚNG THỨ TỰ) ──────

// 1. Global exception handling — phải đứng ĐẦU TIÊN
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Request logging
app.UseMiddleware<RequestLoggingMiddleware>();

// 3. Rate limiting
app.UseMiddleware<RateLimitingMiddleware>();

// 4. Swagger (dev + staging)
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
	app.UseSwagger();
	app.UseSwaggerUI(options =>
	{
		options.SwaggerEndpoint("/swagger/v1/swagger.json", "CuongFilm API v1");
		options.RoutePrefix = "swagger";
		options.DocumentTitle = "CuongFilm API Documentation";
		options.DisplayRequestDuration();
	});
}

// 5. HTTPS redirect
app.UseHttpsRedirection();

// 6. Response compression
app.UseResponseCompression();

// 7. UseRouting phải đứng TRƯỚC UseCors, UseAuthentication, UseAuthorization
app.UseRouting();

// 8. CORS — phải sau UseRouting, trước UseAuthentication
app.UseCors("AllowAll");

// 9. Auth — đúng thứ tự: Authentication trước Authorization
app.UseAuthentication();
app.UseAuthorization();

// 10. Health check endpoint
app.MapHealthChecks("/health");

// 11. Controllers
app.MapControllers();

// 12. Root info
app.MapGet("/", () => new
{
	Name = "CuongFilm API",
	Version = "1.0",
	Documentation = "/swagger",
	Health = "/health",
	Endpoints = new[]
	{
		"GET    /api/movies",
		"GET    /api/movies/{id}",
		"POST   /api/movies          [Admin]",
		"PUT    /api/movies/{id}     [Admin]",
		"DELETE /api/movies/{id}     [Admin]",
		"GET    /api/actors",
		"GET    /api/genres",
		"GET    /api/countries",
		"GET    /api/languages",
		"GET    /api/episodes",
		"GET    /api/episodes/movie/{movieId}",
		"POST   /api/auth/login",
		"POST   /api/auth/register"
	}
});

app.Run();