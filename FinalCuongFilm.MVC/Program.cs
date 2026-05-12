using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.MVC.Data;
using FinalCuongFilm.MVC.Filters;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Service.Mappings;
using FinalCuongFilm.Service.Services;
using Hangfire;
using Hangfire.PostgreSql; // Thư viện mới cho Hangfire
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// 1. Bật chế độ tương thích DateTime cho PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Cấu hình đường dẫn FFmpeg động cho Azure/Railway
if (builder.Environment.IsDevelopment())
{
	// Khi chạy dưới máy Windows (Localhost): Tìm trong thư mục dự án
	Xabe.FFmpeg.FFmpeg.SetExecutablesPath(Path.Combine(builder.Environment.ContentRootPath, "ffmpeg"));
}
else
{
	// Khi đưa lên mạng (Railway/Linux/Docker): FFmpeg đã được cài mặc định ở hệ thống
	Xabe.FFmpeg.FFmpeg.SetExecutablesPath("/usr/bin");
}

builder.WebHost.ConfigureKestrel(serverOptions =>
{
	serverOptions.Limits.MaxRequestBodySize = 5368709120; // 5GB
});

builder.Services.Configure<FormOptions>(options =>
{
	options.MultipartBodyLengthLimit = 5368709120; // 5GB
});

// Lấy 1 chuỗi kết nối duy nhất từ Supabase
var connectionString = builder.Configuration.GetConnectionString("CuongFilmConnection");

// DATABASE 1: Nghiệp vụ Phim
builder.Services.AddDbContext<CuongFilmDbContext>(options =>
	options.UseNpgsql(connectionString));

// DATABASE 2: Identity (Dùng chung chuỗi kết nối với DB Phim)
builder.Services.AddDbContext<CuongFilmIdentityDbContext>(options =>
	options.UseNpgsql(connectionString));

// 2. Chốt chặn an toàn
if (string.IsNullOrEmpty(connectionString))
{
	throw new InvalidOperationException("CRITICAL ERROR: Không tìm thấy chuỗi kết nối Database! Hãy kiểm tra lại biến ConnectionStrings__CuongFilmConnection trên Railway.");
}

// 3. HANGFIRE: Dùng PostgreSQL Storage
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(connectionString));

builder.Services.AddHangfireServer();

builder.Services.AddIdentity<CuongFilmUser, CuongFilmRole>(options =>
{
	options.Password.RequireDigit = true;
	options.Password.RequiredLength = 8;
	options.Password.RequireUppercase = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireNonAlphanumeric = false;
	options.User.RequireUniqueEmail = true;
	options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<CuongFilmIdentityDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Identity/Account/Login";
	options.LogoutPath = "/Identity/Account/Logout";
	options.AccessDeniedPath = "/Identity/Account/AccessDenied";
	options.ExpireTimeSpan = TimeSpan.FromHours(24);
	options.SlidingExpiration = true;
});

// AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile)));

// Services
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IActorService, ActorService>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<IMediaFileService, MediaFileService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddHttpClient<ITmdbService, TmdbService>();
builder.Services.AddScoped<IMovieImportService, MovieImportService>();
//builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();
builder.Services.AddScoped<IVideoConversionService, VideoConversionService>();
builder.Services.AddScoped<IVipService, VipService>();

// MVC 
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// SESSION 
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(60);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

var app = builder.Build();

// PIPELINE 
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
	Authorization = new[] { new HangfireCustomAuthorizationFilter() }
});
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
	name: "movies",
	pattern: "movies",
	defaults: new { controller = "Movie", action = "Index", type = 1 });

app.MapControllerRoute(
	name: "tvseries",
	pattern: "tv-series",
	defaults: new { controller = "Movie", action = "Index", type = 2 });

app.MapControllerRoute(
	name: "areas",
	pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

// Tự động Migrate và Seed Data
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		var identityContext = services.GetRequiredService<CuongFilmIdentityDbContext>();
		var movieContext = services.GetRequiredService<CuongFilmDbContext>();

		// Tự động đẩy bảng lên Supabase nếu chưa có
		identityContext.Database.Migrate();
		movieContext.Database.Migrate();

		// Bơm dữ liệu Admin
		await FinalCuongFilm.MVC.Data.IdentitySeed.SeedAsync(services);
	}
	catch (Exception ex)
	{
		var logger = services.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "Có lỗi xảy ra trong quá trình Migrate hoặc Seed dữ liệu.");
	}
}
app.Run();