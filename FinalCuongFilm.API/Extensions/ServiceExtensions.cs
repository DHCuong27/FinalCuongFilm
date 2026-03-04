using FinalCuongFilm.Datalayer;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace FinalCuongFilm.API.Extensions
{
	public static class ServiceExtensions
	{
		public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
		{
			//// DbContext - dùng đúng connection string key
			//services.AddDbContext<CuongFilmDbContext>(options =>
			//	options.UseSqlServer(configuration.GetConnectionString("CuongFilmConnection")));

			// Register all business services
			services.AddScoped<IMovieService, MovieService>();
			services.AddScoped<IActorService, ActorService>();
			services.AddScoped<IGenreService, GenreService>();
			services.AddScoped<ICountryService, CountryService>();
			services.AddScoped<ILanguageService, LanguageService>();
			services.AddScoped<IEpisodeService, EpisodeService>();
			services.AddScoped<IMediaFileService, MediaFileService>();

			return services;
		}

		public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
		{
			services.AddSwaggerGen(options =>
			{
				options.SwaggerDoc("v1", new OpenApiInfo
				{
					Title = "CuongFilm API",
					Version = "v1",
					Description = "RESTful API for CuongFilm Movie Management System",
					Contact = new OpenApiContact
					{
						Name = "CuongFilm Team",
						Email = "contact@cuongfilm.com"
					},
					License = new OpenApiLicense
					{
						Name = "MIT License",
						Url = new Uri("https://opensource.org/licenses/MIT")
					}
				});

				// JWT Bearer definition in Swagger
				options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Description = "JWT Authorization. Enter: Bearer {your_token}",
					Name = "Authorization",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.ApiKey,
					Scheme = "Bearer"
				});

				options.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference
							{
								Type = ReferenceType.SecurityScheme,
								Id = "Bearer"
							}
						},
						Array.Empty<string>()
					}
				});

				// XML Comments (nếu có)
				var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
				var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
				if (File.Exists(xmlPath))
					options.IncludeXmlComments(xmlPath);
			});

			return services;
		}

		public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
		{
			var jwtSection = configuration.GetSection("Jwt");
			var key = jwtSection["Key"];

			if (string.IsNullOrWhiteSpace(key))
			{
				// JWT is not configured; add authorization only so the app starts safely
				services.AddAuthentication();
				services.AddAuthorization();
				return services;
			}

			var keyBytes = Encoding.UTF8.GetBytes(key);

			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtSection["Issuer"],
					ValidAudience = jwtSection["Audience"],
					IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
					ClockSkew = TimeSpan.Zero
				};

				options.Events = new JwtBearerEvents
				{
					OnAuthenticationFailed = ctx =>
					{
						ctx.NoResult();
						ctx.Response.StatusCode = 401;
						ctx.Response.ContentType = "text/plain";
						return ctx.Response.WriteAsync("Invalid token");
					}
				};
			});

			services.AddAuthorization();
			return services;
		}

		public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
		{
			services.AddCors(options =>
			{
				options.AddPolicy("AllowAll", policy =>
				{
					policy.AllowAnyOrigin()
						  .AllowAnyMethod()
						  .AllowAnyHeader();
				});

				options.AddPolicy("Production", policy =>
				{
					policy.WithOrigins("https://cuongfilm.com", "https://www.cuongfilm.com")
						  .AllowAnyMethod()
						  .AllowAnyHeader()
						  .AllowCredentials();
				});
			});

			return services;
		}
	}
}