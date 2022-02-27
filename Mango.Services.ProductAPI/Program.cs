using AutoMapper;
using Mango.Services.ProductAPI;
using Mango.Services.ProductAPI.DbContexts;
using Mango.Services.ProductAPI.Models.Dto;
using Mango.Services.ProductAPI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Mango.Services.ProductAPI", Version = "v1" });
    options.EnableAnnotations();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"Enter 'Bearer' [space] and your token",
        Name = "Authorization",
        In= ParameterLocation.Header,
        Type= SecuritySchemeType.ApiKey,
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
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

//configuring the DB conn via appsettings
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Conn")));

//configuring automapper
IMapper mapper = MappingConfig.RegisterMaps().CreateMapper();
builder.Services.AddSingleton(mapper);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

//Adding ProductRepository
builder.Services.AddScoped<IProductRepository, ProductRepository>();

//configuring Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:5001/";
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

//Adding use authentication & use authorization
app.UseAuthentication();
app.UseAuthorization();

// Get all products
app.MapGet("api/products", async ([FromServices] IProductRepository productRepository) =>
{
    ResponseDto response = new ResponseDto();
    try
    {
        response.Result = await productRepository.GetProducts();
    }
    catch (Exception ex)
    {
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { ex.ToString() };
    }
    return response;
});

//Get product by Id
app.MapGet("api/products/{id}", async ([FromServices] IProductRepository productRepository, int id) =>
{
    ResponseDto response = new ResponseDto();
    try
    {
        response.Result = await productRepository.GetProductById(id);
    }
    catch (Exception ex)
    {
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { ex.ToString() };
    }
    return response;
});

//Create product
app.MapPost("api/products", [Authorize] async ([FromServices] IProductRepository productRepository, [FromBody] ProductDto productDto) =>
{
    ResponseDto response = new ResponseDto();
    try
    {
        response.Result = await productRepository.CreateUpdateProduct(productDto);
    }
    catch (Exception ex)
    {
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { ex.ToString() };
    }
    return response;
});

//Update product
app.MapPut("api/products", [Authorize] async ([FromServices] IProductRepository productRepository, [FromBody] ProductDto productDto) =>
{
    ResponseDto response = new ResponseDto();
    try
    {
        response.Result = await productRepository.CreateUpdateProduct(productDto);
    }
    catch (Exception ex)
    {
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { ex.ToString() };
    }
    return response;
});

//Delete product
app.MapDelete("api/products/{id}", [Authorize(Roles = "Admin")] async ([FromServices] IProductRepository productRepository, int id) =>
{
    ResponseDto response = new ResponseDto();
    try
    {
        response.Result = await productRepository.DeleteProduct(id);
    }
    catch (Exception ex)
    {
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { ex.ToString() };
    }
    return response;
});

app.Run();
