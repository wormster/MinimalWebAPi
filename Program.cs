using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using MinimalWebAPi;
using MinimalWebAPi.DataAccess;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestHeaders.Add("X-Request-Header");
    logging.RequestHeaders.Add("X-Response-Header");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDbContext<MinimalApiDb>(opt => opt.UseInMemoryDatabase("MinimalApiDb"));
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IUserRepositoryService, UserRepositoryService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddAuthorization(o => o.AddPolicy("BossOnly",
    b => b.RequireClaim(ClaimTypes.Role, "Boss")));

builder.Services.AddAuthentication().AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new()
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = false,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty))
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

await using var app = builder.Build();

app.UseHttpLogging();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();


#region Define Endpoints

app.MapGet("/", LoadUsers);
app.MapPost("/login", Login).AllowAnonymous();
app.MapPost("/refresh", Refresh).RequireAuthorization();
app.MapPost("/revoke", Revoke).RequireAuthorization();
app.MapGet("/anon", () => Results.Ok("Anonymous action")).AllowAnonymous().WithName("anonymous_action");

RouteGroupBuilder actions = app.MapGroup("/actions").RequireAuthorization();
actions.MapGet("/dev", DevAction).RequireAuthorization(r => r.RequireClaim(ClaimTypes.Role, "Developer"));
actions.MapGet("/mgr", MgrAction);
actions.MapGet("/boss", BossAction).RequireAuthorization("BossOnly");

#endregion

async Task<IResult> LoadUsers(IUserService userService)
{
    await userService.LoadUsers();
;
    return Results.Ok("This a demo for JWT Authentication using Minimalist Web API. Users loaded");
}

async Task<IResult> Login(HttpContext http, IUserService userService, ITokenService tokenService)
{
    var userModel = await http.Request.ReadFromJsonAsync<User>();

    if (userModel is null)
    {
        return Results.BadRequest("Invalid client request");
    }

    var userDto = await userService.GetUser(userModel);

    if (userDto is null)
    {
        return Results.Unauthorized();
    }

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, userDto.UserName),
        new Claim(ClaimTypes.Role, userDto.Role)
    };

    var accessToken = tokenService.GenerateAccessToken(claims);
    var refreshToken = tokenService.GenerateRefreshToken();

    userDto.RefreshToken = refreshToken;
    userDto.RefreshTokenExpiryTime = DateTime.Now.AddHours(1);

    await userService.SaveUser(userDto);

    if (string.IsNullOrWhiteSpace(accessToken))
    {
        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Results.Unauthorized();
    }

    return Results.Ok(new TokenApiModel
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken
    });
}

async static Task<IResult> Refresh(TokenApiModel tokenApiModel, HttpContext httpContext, IUserService userService, ITokenService tokenService)
{
    if (tokenApiModel is null)
        return Results.BadRequest("Invalid client request");

    string accessToken = tokenApiModel.AccessToken;
    string refreshToken = tokenApiModel.RefreshToken;

    var principal = tokenService.GetPrincipalFromExpiredToken(accessToken);

    var username = principal.Identity.Name; //this is mapped to the Name claim by default

    var user = await userService.GetUserByUserName(httpContext.User.Identity.Name);

    if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
        return Results.BadRequest("Invalid client request");

    var newAccessToken = tokenService.GenerateAccessToken(principal.Claims);
    var newRefreshToken = tokenService.GenerateRefreshToken();
    user.RefreshToken = newRefreshToken;

    await userService.SaveUser(user);

    return Results.Ok(new TokenApiModel()
    {
        AccessToken = newAccessToken,
        RefreshToken = newRefreshToken
    });
}

async static Task<IResult> Revoke(HttpContext httpContext, IUserService userService)
{
    var user = await userService.GetUserByUserName(httpContext.User.Identity.Name);

    if (user == null) return Results.BadRequest();

    user.RefreshToken = null;
    await userService.SaveUser(user);

    return Results.NoContent();
}
static async Task<IResult> DevAction(HttpContext httpContext, ClaimsPrincipal claimsPrincipal, IUserService userService)
{
    var claims = httpContext.User.Claims;
    var firstClaim = claimsPrincipal.FindFirstValue(ClaimTypes.Role);

    var user = await userService.GetUserByUserName(httpContext.User.Identity.Name);
    return Results.Ok($"{user.Name} {firstClaim} Action Succeeded.");
}

[Authorize(Roles = "Manager")]
static async Task<IResult> MgrAction(HttpContext httpContext, ClaimsPrincipal claimsPrincipal, IUserService userService)
{
    var claims = httpContext.User.Claims;
    var firstClaim = claimsPrincipal.FindFirstValue(ClaimTypes.Role);

    var user = await userService.GetUserByUserName(httpContext.User.Identity.Name);

    return Results.Ok($"{user.Name} {firstClaim} Action Succeeded.");
}

static async Task<IResult> BossAction(HttpContext httpContext, ClaimsPrincipal claimsPrincipal, IUserService userService)
{
    var claims = httpContext.User.Claims;
    var firstClaim = claimsPrincipal.FindFirstValue(ClaimTypes.Role);

    var user = await userService.GetUserByUserName(httpContext.User.Identity.Name);

    return Results.Ok($"{user.Name} {firstClaim} Action Succeeded.");
}


app.UseSwaggerUI();

await app.RunAsync();
