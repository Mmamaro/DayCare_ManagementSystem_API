
using DayCare_ManagementSystem_API.Helper;
using DayCare_ManagementSystem_API.Models;
using DayCare_ManagementSystem_API.Repositories;
using DayCare_ManagementSystem_API.Service;
using DayCare_ManagementSystem_API.Services;
using MongoDB.Driver;
using Serilog;

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
