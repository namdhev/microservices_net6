using AutoMapper;
using Mango.Services.ShoppingCartAPI;
using Mango.Services.ShoppingCartAPI.DbContexts;
using Mango.Services.ShoppingCartAPI.Messages;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Mango.MessageBus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Mango.Services.ShoppingCartAPI", Version = "v1" });
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
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICouponRepository, CouponRepository>();
builder.Services.AddHttpClient<ICouponRepository, CouponRepository>(u => u.BaseAddress = 
    new Uri(builder.Configuration["ServiceUrls:CouponAPI"]));
builder.Services.AddSingleton<IMessageBus, AzureServiceBusMessageBus>();

// declaring constants
const string cartRoute = "api/cart";
var checkoutConnectionString = builder.Configuration.GetConnectionString("CheckoutTopicConn");
var checkoutTopicName = builder.Configuration.GetSection("CheckoutTopicName").Value;

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

//Adding use authentication & use authorization
app.UseAuthentication();
app.UseAuthorization();

// get cart by user id
app.MapGet($"{cartRoute}/{{userId}}", async ([FromServices] ICartRepository cartRepository, string userId) =>
{
    ResponseDto response = new();
    try
    {
        CartDto cartDto = await cartRepository.GetCartByUserId(userId);
        response.Result = cartDto;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>(){e.ToString()};
    }

    return response;
});


// create cart
app.MapPost($"{cartRoute}/createCart", async ([FromServices] ICartRepository cartRepository, [FromBody] CartDto cartDto) =>
{
    ResponseDto response = new();
    try
    {
        CartDto _cartDto = await cartRepository.CreateUpdateCart(cartDto);
        response.Result = _cartDto;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { e.ToString() };
    }

    return response;
});

// Update cart
app.MapPost($"{cartRoute}/updateCart", async ([FromServices] ICartRepository cartRepository, [FromBody] CartDto cartDto) =>
{
    ResponseDto response = new();
    try
    {
        CartDto _cartDto = await cartRepository.CreateUpdateCart(cartDto);
        response.Result = _cartDto;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { e.ToString() };
    }

    return response;
});

// Remove from cart
app.MapPost($"{cartRoute}/removeFromCart", async ([FromServices] ICartRepository cartRepository, [FromBody] int cartDetailsId) =>
{
    ResponseDto response = new();
    try
    {
        bool isRemovalSuccess = await cartRepository.RemoveFromCart(cartDetailsId);
        response.Result = isRemovalSuccess;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { e.ToString() };
    }

    return response;
});

// Apply coupon code
app.MapPost($"{cartRoute}/applyCouponCode", async ([FromServices] ICartRepository cartRepository, [FromBody] CartDto cartDto) =>
{
    ResponseDto response = new();
    try
    {
        bool isRemovalSuccess = await cartRepository.ApplyCoupon(cartDto.CartHeader.UserId, cartDto.CartHeader.CouponCode);
        response.Result = isRemovalSuccess;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { e.ToString() };
    }

    return response;
});

// Remove coupon code
app.MapPost($"{cartRoute}/removeCouponCode", async ([FromServices] ICartRepository cartRepository, [FromBody] string userId) =>
{
    ResponseDto response = new();
    try
    {
        bool isRemovalSuccess = await cartRepository.RemoveCoupon(userId);
        response.Result = isRemovalSuccess;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { e.ToString() };
    }

    return response;
});

// Checkout
app.MapPost($"{cartRoute}/checkout", async Task<object> (
    [FromServices] ICartRepository cartRepository,
    [FromServices] ICouponRepository couponRepository,
    [FromServices] IMessageBus messageBus,
    [FromBody] CheckoutHeaderDto checkoutHeader) =>
{
    ResponseDto response = new();
    try
    {
        CartDto cartDto = await cartRepository.GetCartByUserId(checkoutHeader.UserId);

        if (cartDto == null)
            return Microsoft.AspNetCore.Http.Results.BadRequest();

        if (checkoutHeader.CouponCode != null)
        {
            CouponDto coupon = await couponRepository.GetCoupon(checkoutHeader.CouponCode);
            if (checkoutHeader.DiscountTotal != coupon.DiscountAmount)
            {
                response.IsSuccess = false;
                response.ErrorMessages = new List<string>() {"Coupon has changed, please confirm"};
                response.DisplayMessage = "Coupon has changed, please confirm";
                return response;
            }
        }

        checkoutHeader.CartDetails = cartDto.CartDetails;
        await messageBus.PublishMessage(checkoutHeader, checkoutTopicName, checkoutConnectionString);
        await cartRepository.ClearCart(checkoutHeader.UserId);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() { e.ToString() };
    }

    return response;
});

// Clear cart
app.MapPost($"{cartRoute}/clearCart", async ([FromServices] ICartRepository cartRepository, [FromBody] string userId) =>
{
    ResponseDto response = new();
    try
    {
        bool hasCartBeenCleared = await cartRepository.ClearCart(userId);
        response.Result = hasCartBeenCleared;
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        response.IsSuccess = false;
        response.ErrorMessages = new List<string>() {e.ToString()};
    }

    return response;
});



app.Run();
