using AutoMapper;
using Mango.Services.CouponAPI;
using Mango.Services.CouponAPI.DbContexts;
using Mango.Services.CouponAPI.Models.Dtos;
using Mango.Services.CouponAPI.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Mango.Services.CouponAPI", Version = "v1" });
    options.EnableAnnotations();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"Enter 'Bearer' [space] and your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    var securityRequirement = new OpenApiSecurityRequirement();
    securityRequirement.Add(
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            },
            Scheme = "oauth2",
            Name = "Bearer",
            In = ParameterLocation.Header
        },
        new List<string>()
    );
    options.AddSecurityRequirement(securityRequirement);
});

//configuring the DB conn via appsettings
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Conn")));

//configuring automapper
IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
builder.Services.AddSingleton(mapper);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<ICouponRepository, CouponRepository>();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:5001/"; // Ideally should come from appSettings
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
        };
    });

//configuring Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "mango");
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/coupon/{couponCode}", 
    async ([FromServices] ICouponRepository _couponRepository, string couponCode) =>
{
    ResponseDto response = new();
    try
    {
        CouponDto couponDto = await _couponRepository.GetCouponByCode(couponCode);
        response.Result = couponDto;
    }
    catch (Exception e)
    {
        Console.WriteLine("Error occurred while fetching coupon code details, \n{0}", e);
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { e.ToString() };
    }

    return response;
});

app.Run();
