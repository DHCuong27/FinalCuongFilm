using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.MVC.Data;
using FinalCuongFilm.MVC.Filters;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Service.Mappings;
using FinalCuongFilm.Service.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Lấy connection string trước khi dùng
var connectionString = builder.Configuration.GetConnectionString("CuongFilmConnection");
if (string.IsNullOrEmpty(connectionString))
{
	throw new InvalidOperationException("CRITICAL ERROR: Không tìm thấy chuỗi kết nối Database! Hãy kiểm tra lại biến ConnectionStrings__CuongFilmConnection trên Railway.");
}

// ✅ FFmpeg path: ưu tiên ENV, fallback theo OS
var ffmpegPathFromEnv = builder.Configuration["FFMPEG_PATH"];
if (!string.IsNullOrWhiteSpace(ffmpegPathFromEnv) && Directory.Exists(ffmpegPathFromEnv))
{
	Xabe.FFmpeg.FFmpeg.SetExecutablesPath(ffmpegPathFromEnv);
}
else if (OperatingSystem.IsWindows())
{
	var localFfmpeg = Path.Combine(builder.Environment.ContentRootPath, "ffmpeg");
	if (Directory.Exists(localFfmpeg))
	{
		Xabe.FFmpeg.FFmpeg.SetExecutablesPath(localFfmpeg);
	}
	else
	{
		// Không set path -> sẽ lỗi rõ ràng nếu thiếu ffmpeg trên Windows
	}
}
else
{
	// Linux / Railway / Docker
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

builder.Services.AddDbContext<CuongFilmDbContext>(options =>
	options.UseNpgsql(connectionString));

builder.Services.AddDbContext<CuongFilmIdentityDbContext>(options =>
	options.UseNpgsql(connectionString));

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

builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Identity/Account/Login";
	options.LogoutPath = "/Identity/Account/Logout";
	options.AccessDeniedPath = "/Identity/Account/AccessDenied";
	options.ExpireTimeSpan = TimeSpan.FromHours(24);
	options.SlidingExpiration = true;
});

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile)));

builder.Services.AddHttpClient();
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
builder.Services.AddScoped<IStorageService, SupabaseStorageService>();
builder.Services.AddScoped<IVideoConversionService, VideoConversionService>();
builder.Services.AddScoped<IVipService, VipService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(60);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

var app = builder.Build();

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

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		var identityContext = services.GetRequiredService<CuongFilmIdentityDbContext>();
		var movieContext = services.GetRequiredService<CuongFilmDbContext>();

		identityContext.Database.Migrate();
		movieContext.Database.Migrate();

		await FinalCuongFilm.MVC.Data.IdentitySeed.SeedAsync(services);
	}
	catch (Exception ex)
	{
		var logger = services.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "Có lỗi xảy ra trong quá trình Migrate hoặc Seed dữ liệu.");
	}
}
app.Run();