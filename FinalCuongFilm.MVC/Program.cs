using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.MVC.Data;
using FinalCuongFilm.MVC.Filters;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Service.Mappings;
using FinalCuongFilm.Service.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Resolve the PostgreSQL connection string before registering services
var connectionString = GetRequiredPostgresConnectionString(builder.Configuration);
builder.Logging.AddConsole();

// FFmpeg path: prefer ENV, then fallback by OS
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
		// Leave unset so missing FFmpeg fails clearly on Windows
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

builder.Services.AddResponseCompression(options =>
{
	options.EnableForHttps = true;
	options.Providers.Add<BrotliCompressionProvider>();
	options.Providers.Add<GzipCompressionProvider>();
	options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
	{
		"application/json",
		"application/manifest+json",
		"image/svg+xml"
	});
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
	options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
	options.Level = CompressionLevel.Fastest;
});
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

app.Use(async (context, next) =>
{
	context.Response.OnStarting(() =>
	{
		var headers = context.Response.Headers;
		headers.TryAdd("X-Content-Type-Options", "nosniff");
		headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
		headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
		headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
		return Task.CompletedTask;
	});

	await next();
});
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
	OnPrepareResponse = ctx =>
	{
		var requestPath = ctx.Context.Request.Path.Value ?? string.Empty;
		var isDocumentLike = requestPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
			|| requestPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
			|| requestPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

		ctx.Context.Response.Headers.CacheControl = app.Environment.IsDevelopment()
			? "no-cache"
			: isDocumentLike
				? "public,max-age=3600"
				: "public,max-age=31536000,immutable";
	}
});
app.UseRouting();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
	Authorization = new[] { new HangfireCustomAuthorizationFilter() }
});
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
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
		logger.LogError(ex, "Database migration or seed failed.");
		if (!app.Environment.IsDevelopment())
		{
			throw;
		}
	}
}
app.Run();
static string GetRequiredPostgresConnectionString(IConfiguration configuration)
{
	var candidates = new (string Source, string? Value)[]
	{
		("DATABASE_URL", configuration["DATABASE_URL"]),
		("POSTGRES_URL", configuration["POSTGRES_URL"]),
		("POSTGRES_DATABASE_URL", configuration["POSTGRES_DATABASE_URL"]),
		("ConnectionStrings__CuongFilmConnection", configuration.GetConnectionString("CuongFilmConnection")),
		("ConnectionStrings__DefaultConnection", configuration.GetConnectionString("DefaultConnection"))
	};

	var selected = candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate.Value));
	if (string.IsNullOrWhiteSpace(selected.Value))
	{
		throw new InvalidOperationException(
			"CRITICAL ERROR: Missing PostgreSQL connection string. Configure DATABASE_URL with a public PostgreSQL URL, for example from Supabase Transaction Pooler.");
	}

	var normalizedConnectionString = NormalizePostgresConnectionString(selected.Value);
	Console.WriteLine($"Database connection source: {selected.Source}; host: {GetSafePostgresHost(normalizedConnectionString)}");
	return normalizedConnectionString;
}

static string GetSafePostgresHost(string connectionString)
{
	try
	{
		if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
			|| connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
		{
			return new Uri(connectionString).Host;
		}

		return new NpgsqlConnectionStringBuilder(connectionString).Host ?? "unknown";
	}
	catch
	{
		return "unknown";
	}
}
static string StripEnvironmentVariablePrefix(string value)
{
	var equalsIndex = value.IndexOf('=');
	if (equalsIndex <= 0)
	{
		return value;
	}

	var prefix = value[..equalsIndex].Trim();
	var knownPrefixes = new[]
	{
		"DATABASE_URL",
		"POSTGRES_URL",
		"POSTGRES_DATABASE_URL",
		"ConnectionStrings__CuongFilmConnection",
		"ConnectionStrings__DefaultConnection"
	};

	return knownPrefixes.Any(item => item.Equals(prefix, StringComparison.OrdinalIgnoreCase))
		? value[(equalsIndex + 1)..].Trim()
		: value;
}
static string NormalizePostgresConnectionString(string connectionString)
{
	connectionString = StripEnvironmentVariablePrefix(connectionString.Trim());

	if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
		&& !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
	{
		return connectionString;
	}

	var uri = new Uri(connectionString);
	var userInfoParts = uri.UserInfo.Split(':', 2);
	var username = userInfoParts.Length > 0 ? Uri.UnescapeDataString(userInfoParts[0]) : string.Empty;
	var password = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : string.Empty;

	var builder = new NpgsqlConnectionStringBuilder
	{
		Host = uri.Host,
		Port = uri.Port > 0 ? uri.Port : 5432,
		Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
		Username = username,
		Password = password,
		SslMode = SslMode.Require,
		TrustServerCertificate = true
	};

	var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
	foreach (var item in query)
	{
		var pair = item.Split('=', 2);
		if (pair.Length != 2)
		{
			continue;
		}

		var key = Uri.UnescapeDataString(pair[0]);
		var value = Uri.UnescapeDataString(pair[1]);
		if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase)
			&& Enum.TryParse<SslMode>(value, ignoreCase: true, out var sslMode))
		{
			builder.SslMode = sslMode;
		}
	}

	return builder.ConnectionString;
}





