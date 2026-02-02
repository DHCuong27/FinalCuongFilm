using FinalCuongFilm.API.Extensions;
using FinalCuongFilm.API.Middleware;
using FinalCuongFilm.DataLayer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// CONFIGURE SERVICES
// ============================================

builder.Services.AddDbContext<CuongFilmDbContext>(options =>
	options.UseSqlServer(
		builder.Configuration.GetConnectionString("CuongFilmConnection")));

// Controllers
builder.Services.AddControllers()
	.AddNewtonsoftJson(options =>
	{
		options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
		options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
	});

// Application Services (from Extension)
builder.Services.AddApplicationServices(builder.Configuration);

// Swagger Documentation
builder.Services.AddSwaggerDocumentation();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS
builder.Services.AddCorsPolicy();

// API Explorer
builder.Services.AddEndpointsApiExplorer();

// Health Checks
builder.Services.AddHealthChecks();

// Response Compression
builder.Services.AddResponseCompression();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// ============================================
// CONFIGURE MIDDLEWARE PIPELINE
// ============================================

// Exception Handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger (Development & Staging)
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
	app.UseSwagger();
	app.UseSwaggerUI(options =>
	{
		options.SwaggerEndpoint("/swagger/v1/swagger.json", "CuongFilm API v1");
		options.RoutePrefix = string.Empty; // Swagger at root: /
		options.DocumentTitle = "CuongFilm API Documentation";
	});
}

// HTTPS Redirection
app.UseHttpsRedirection();

// Response Compression
app.UseResponseCompression();

// CORS
app.UseCors("AllowAll"); // Change to "Production" in production

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health Checks
app.MapHealthChecks("/health");

// Map Controllers
app.MapControllers();

// Default Route
app.MapGet("/", () => new
{
	Name = "CuongFilm API",
	Version = "1.0",
	Documentation = "/swagger",
	Health = "/health"
});

app.Run();