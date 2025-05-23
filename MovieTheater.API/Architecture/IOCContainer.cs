using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MovieTheater.Application.Interfaces;
using MovieTheater.Application.Interfaces.Commons;
using MovieTheater.Application.Services;
using MovieTheater.Application.Services.Commons;
using MovieTheater.Domain;
using MovieTheater.Infrastructure;
using MovieTheater.Infrastructure.Commons;
using MovieTheater.Infrastructure.Interfaces;
using MovieTheater.Infrastructure.Repositories;
using Resend;
using System.Text;

namespace MovieTheater.API.Architecture;

public static class IocContainer
{
    public static IServiceCollection SetupIocContainer(this IServiceCollection services)
    {
        //Add Logger
        services.AddScoped<ILoggerService, LoggerService>();

        //Add Project Services
        services.SetupDbContext();
        services.SetupSwagger();

        //Add generic repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        //Add business services
        services.SetupBusinessServicesLayer();

        services.SetupJwt();

        // services.SetupGraphQl();
        services.SetupReSendService();

        services.SetupVnpay();
        return services;
    }


    // public static IServiceCollection SetupGraphQl(this IServiceCollection services)
    // {
    //     services
    //         .AddGraphQLServer()
    //         .AddErrorFilter<GraphQLErrorFilter>()
    //         .AddQueryType<Query>();
    //     
    //     return services;
    // }

    public static IServiceCollection SetupReSendService(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = Environment.GetEnvironmentVariable("RESEND_APITOKEN")!;
        });
        services.AddTransient<IResend, ResendClient>();

        return services;
    }

    public static IServiceCollection SetupVnpay(this IServiceCollection services)
    {
        // Xây dựng IConfiguration từ các nguồn cấu hình
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Lấy thư mục hiện tại
            .AddJsonFile("appsettings.json", true, true)  // Đọc appsettings.json
            .AddEnvironmentVariables()                    // Đọc biến môi trường từ Docker
            .Build();

        return services;
    }

    private static IServiceCollection SetupDbContext(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables()
            .Build();


        // Lấy connection string từ "DefaultConnection"
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Đăng ký DbContext với Npgsql
        services.AddDbContext<MovieTheaterDbContext>(options =>
            options.UseNpgsql(connectionString,
                sql => sql.MigrationsAssembly(typeof(MovieTheaterDbContext).Assembly.FullName)
            )
        );

        return services;
    }

    public static IServiceCollection SetupBusinessServicesLayer(this IServiceCollection services)
    {
        // Inject những service vào DI container

        //services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILoggerService, LoggerService>();
        services.AddScoped<ICurrentTime, CurrentTime>();
        services.AddScoped<IClaimsService, ClaimsService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IBlobService, BlobService>();

        services.AddHttpContextAccessor();

        return services;
    }


    private static IServiceCollection SetupSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.UseInlineDefinitionsForEnums();

            c.SwaggerDoc("v1",
                new OpenApiInfo { Title = "MovieTheaterAPI", Version = "v1" });
            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Description = "Enter your JWT token in this field",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            c.AddSecurityDefinition("Bearer", jwtSecurityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
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
                    new string[] { }
                }
            };

            c.AddSecurityRequirement(securityRequirement);

            // Cấu hình Swagger để sử dụng Newtonsoft.Json
            c.UseAllOfForInheritance();
        });

        return services;
    }

    private static IServiceCollection SetupJwt(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables()
            .Build();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, // Bật kiểm tra Issuer
                    ValidateAudience = true, // Bật kiểm tra Audience
                    ValidateLifetime = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidAudience = configuration["JWT:Audience"],
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"] ??
                                                                        throw new InvalidOperationException()))
                };
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CustomerPolicy", policy =>
                policy.RequireRole("Customer"));

            options.AddPolicy("MemberPolicy", policy =>
                policy.RequireRole("Member"));

            options.AddPolicy("EmployeePolicy", policy =>
                policy.RequireRole("Employee"));

            options.AddPolicy("AdminPolicy", policy =>
                policy.RequireRole("Admin"));
        });

        return services;
    }
}