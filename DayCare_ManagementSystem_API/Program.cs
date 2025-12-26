
using DayCare_ManagementSystem_API.Helper;
using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using DayCare_ManagementSystem_API.Service;
using DayCare_ManagementSystem_API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;
using System.Text;

namespace DayCare_ManagementSystem_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            #region [ Configure Jwt ]
            //Jwt configuration starts here
            var jwtIssuer = Environment.GetEnvironmentVariable("Jwt_Issuer");
            var jwtKey = Environment.GetEnvironmentVariable("Jwt_Key");

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtIssuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });
            //Jwt configuration ends here 
            #endregion

            #region [ configure Swagger Token Validation ]
            //Define Swagger generation options and add Bearer token authentication
            builder.Services.AddSwaggerGen(opt =>
            {
                opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Daycare Portal", Version = "v1" });
                opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });

                opt.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type=ReferenceType.SecurityScheme,
                            Id="Bearer"
                        }
                    },
                    new string[]{}
                }
            });
            });
            #endregion

            #region [ Connection to MongoDB ]
            //Get Connection Settings
            builder.Services.Configure<DBSettings>(builder.Configuration.GetSection("DbSettings"));

            //Creating a MongoClient instance -- Connecting to MongoDb
            builder.Services.AddSingleton<IMongoClient>(_ =>
            {
                var connectioString = builder.Configuration.GetSection("DbSettings:ConnectionString")?.Value;

                return new MongoClient(connectioString);
            });

            #endregion

            #region [ Registering Services ]

            builder.Services.AddSingleton<EmailService>();
            builder.Services.AddHostedService<SeedWorkerService>();
            builder.Services.AddSingleton<PasswordHelper>();
            builder.Services.AddSingleton<SeedDbService>();
            builder.Services.AddSingleton<EmailService>();
            builder.Services.AddSingleton<IUser, UserRepo>();
            builder.Services.AddSingleton<MFAService>();
            builder.Services.AddSingleton<TokensHelper>();
            builder.Services.AddSingleton<IToken, TokenService>();
            builder.Services.AddSingleton<IRefreshToken, RefreshTokenRepo>();

            #endregion

            #region [ Add logging provider ]

            builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
            #endregion

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
