	using FinalCuongFilm.ApplicationCore.Entities.Identity;
	using FinalCuongFilm.DataLayer;
	using FinalCuongFilm.MVC.Data;
	using FinalCuongFilm.Service.Interfaces;      
	using FinalCuongFilm.Service.Mappings;
	using FinalCuongFilm.Service.Services;
	using Microsoft.AspNetCore.Http.Features;
	using Microsoft.AspNetCore.Identity;
	using Microsoft.EntityFrameworkCore;


	var builder = WebApplication.CreateBuilder(args);

	//  DATABASE 
	builder.Services.AddDbContext<CuongFilmDbContext>(options =>
		options.UseSqlServer(
			builder.Configuration.GetConnectionString("CuongFilmConnection")));

	builder.Services.AddDbContext<CuongFilmIdentityDbContext>(options =>
		options.UseSqlServer(
			builder.Configuration.GetConnectionString("CuongFilmIdentityConnection")));

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
	});

	// AutoMapper
	builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile)));

	//  Services
	builder.Services.AddScoped<IMovieService, MovieService>();
	builder.Services.AddScoped<IActorService, ActorService>();
	builder.Services.AddScoped<IGenreService, GenreService>();
	builder.Services.AddScoped<ICountryService, CountryService>();
	builder.Services.AddScoped<ILanguageService, LanguageService>();
	builder.Services.AddScoped<IEpisodeService, EpisodeService>();
	builder.Services.AddScoped<IMediaFileService, MediaFileService>();
	builder.Services.AddScoped<IFavoriteService, FavoriteService>();
	builder.Services.AddScoped<IReviewService, ReviewService>();

// Service upload file lên Azure Blob Storage
builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();

// Cấu hình upload file size
builder.Services.Configure<FormOptions>(options =>
	{
		options.MultipartBodyLengthLimit = 2147483648; // 2GB
	});

	// MVC 
	builder.Services.AddControllersWithViews();
	builder.Services.AddRazorPages();

	//  SESSION 
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

	app.UseSession();
	app.UseAuthentication();
	app.UseAuthorization();

	app.MapRazorPages();


	app.MapControllerRoute(
		name: "areas",
		pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

// Movie routes
app.MapControllerRoute(
	name: "movieWatch",
	pattern: "Movie/Watch/{slug}",
	defaults: new { controller = "Movie", action = "Watch" });

app.MapControllerRoute(
	name: "movieDetail",
	pattern: "Movie/Detail/{slug}",
	defaults: new { controller = "Movie", action = "Detail" });

app.MapControllerRoute(
		name: "default",
		pattern: "{controller=Home}/{action=Index}/{id?}");



	await IdentitySeed.SeedAsync(app.Services);

	app.Run();
