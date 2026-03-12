using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace FinalCuongFilm.API.Extensions
{
	public static class ServiceExtensions
	{
		public static IServiceCollection AddApplicationServices(
			this IServiceCollection services,
			IConfiguration configuration)
		{
			// AutoMapper
			services.AddAutoMapper(cfg =>
				cfg.AddMaps(typeof(FinalCuongFilm.Service.Mappings.MappingProfile)));

			// ✅ FIX: Đăng ký Identity với CuongFilmUser + CuongFilmIdentityDbContext
			// Thiếu phần này là nguyên nhân chính gây 401 và 500 trên API
			services.AddIdentity<CuongFilmUser, CuongFilmRole>(options =>
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
			.AddDefaultTokenProviders();

			// Business services
			services.AddScoped<IMovieService, MovieService>();
			services.AddScoped<IActorService, ActorService>();
			services.AddScoped<IGenreService, GenreService>();
			services.AddScoped<ICountryService, CountryService>();
			services.AddScoped<ILanguageService, LanguageService>();
			services.AddScoped<IEpisodeService, EpisodeService>();
			services.AddScoped<IMediaFileService, MediaFileService>();
			services.AddScoped<IFavoriteService, FavoriteService>();
			services.AddScoped<IReviewService, ReviewService>();
			services.AddScoped<IAzureBlobService, AzureBlobService>();

			return services;
		}

		public static IServiceCollection AddJwtAuthentication(
			this IServiceCollection services,
			IConfiguration configuration)
		{
			var jwtKey = configuration["Jwt:Key"]
							  ?? throw new InvalidOperationException("Jwt:Key is not configured");
			var jwtIssuer = configuration["Jwt:Issuer"] ?? "CuongFilmAPI";
			var jwtAudience = configuration["Jwt:Audience"] ?? "CuongFilmClients";

			services.AddAuthentication(options =>
			{
				// ✅ FIX: Đặt scheme mặc định là JWT thay vì Cookie
				// (khi dùng AddIdentity, scheme mặc định bị ghi đè thành Cookie)
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.RequireHttpsMetadata = false; // Đặt true khi production
				options.SaveToken = true;
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(
												   Encoding.UTF8.GetBytes(jwtKey)),
					ValidateIssuer = true,
					ValidIssuer = jwtIssuer,
					ValidateAudience = true,
					ValidAudience = jwtAudience,
					ValidateLifetime = true,
					ClockSkew = TimeSpan.Zero
				};

				// ✅ FIX: Trả về JSON 401 thay vì redirect về trang login
				options.Events = new JwtBearerEvents
				{
					OnChallenge = async context =>
					{
						context.HandleResponse();
						context.Response.StatusCode = 401;
						context.Response.ContentType = "application/json";
						await context.Response.WriteAsync(
							"{\"success\":false,\"message\":\"Unauthorized - Token không hợp lệ hoặc đã hết hạn\",\"statusCode\":401}");
					},
					OnForbidden = async context =>
					{
						context.Response.StatusCode = 403;
						context.Response.ContentType = "application/json";
						await context.Response.WriteAsync(
							"{\"success\":false,\"message\":\"Forbidden - Bạn không có quyền truy cập\",\"statusCode\":403}");
					}
				};
			});

			return services;
		}

		public static IServiceCollection AddSwaggerDocumentation(
			this IServiceCollection services)
		{
			services.AddSwaggerGen(options =>
			{
				options.SwaggerDoc("v1", new OpenApiInfo
				{
					Title = "CuongFilm API",
					Version = "v1",
					Description = "RESTful API for CuongFilm streaming platform"
				});

				// ✅ Cho phép gửi Bearer token qua Swagger UI
				options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Name = "Authorization",
					Type = SecuritySchemeType.ApiKey,
					Scheme = "Bearer",
					BearerFormat = "JWT",
					In = ParameterLocation.Header,
					Description = "Nhập: Bearer {token}"
				});

				options.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference
							{
								Type = ReferenceType.SecurityScheme,
								Id   = "Bearer"
							}
						},
						Array.Empty<string>()
					}
				});
			});

			return services;
		}

		public static IServiceCollection AddCorsPolicy(
			this IServiceCollection services)
		{
			services.AddCors(options =>
			{
				options.AddPolicy("AllowAll", policy =>
					policy.AllowAnyOrigin()
						  .AllowAnyMethod()
						  .AllowAnyHeader());
			});

			return services;
		}
	}
}