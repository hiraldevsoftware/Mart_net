
using Mart.Domain.Interface;
using Mart.Persistence;
using Mart.Persistence.Repositories;
using Mart.Persistence.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

namespace Mart
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowEverything", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "Mart API", Version = "v1" });
            });

            builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IAddressRepository, AddressRepository>();
            builder.Services.AddScoped<IStoreRepository, StoreRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<ICouponRepository, CouponRepository>();
            builder.Services.AddScoped<IWalletRepository, WalletRepository>();
            builder.Services.AddScoped<IAdminProductRepository, AdminProductRepository>();
            builder.Services.AddScoped<IMediaService, MediaService>();
            builder.Services.AddScoped<IAdminCategoryRepository, AdminCategoryRepository>();
            builder.Services.AddScoped<IAdminOrderRepository, AdminOrderRepository>();
            builder.Services.AddScoped<IAdminRiderRepository, AdminRiderRepository>();
            builder.Services.AddScoped<IAdminBannerRepository, AdminBannerRepository>();
            builder.Services.AddScoped<IAdminStoreRepository, AdminStoreRepository>();
            builder.Services.AddScoped<IAdminSupportRepository, AdminSupportRepository>();
            builder.Services.AddScoped<IAdminMarketingRepository, AdminMarketingRepository>();
            builder.Services.AddScoped<IRiderRepository, RiderRepository>();
            builder.Services.AddScoped<IVendorRepository, VendorRepository>();
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisConnection")));

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "localhost:6379";
                options.InstanceName = "Mart_";
            });
     


            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configuration = "localhost:6379";
                return ConnectionMultiplexer.Connect(configuration);
            });
            builder.Services.AddHostedService<OrderTimeoutService>();
            builder.Services.AddScoped<IQRCodeService, QRCodeService>();

            var jwtSettings = builder.Configuration.GetSection("Jwt");

            // Ahiya check karo: Jo appsettings mathi na male to temporary key vapre
            string secretKey = jwtSettings["Key"] ?? "Aa_Mari_Moti_Temporary_Key_32_Chars_Long_12345";
            string issuer = jwtSettings["Issuer"] ?? "Mart.Api";
            string audience = jwtSettings["Audience"] ?? "Mart.MobileApp";

            var key = Encoding.UTF8.GetBytes(secretKey);

            builder.Services.AddAuthentication(options =>
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
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });


            var app = builder.Build();
            //app.UseCors("AllowEverything");


     




            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                //app.UseSwaggerUI();

                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mart API V1");
                });
            }


            app.UseHttpsRedirection();
            app.UseStaticFiles();

      

            app.UseRouting();



            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = 200;
                    await context.Response.CompleteAsync();
                    return;
                }
                await next();
            });



            app.UseAuthentication();
            app.UseAuthorization();




            app.MapControllers();

            app.Run();
        }
    }
}                                                                      


                                                                       