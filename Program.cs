using Microsoft.AspNetCore.HttpLogging;
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
builder.Services.AddSingleton<ITokenService>(new TokenService());
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

async Task Login(HttpContext http, IUserService userService)
{
    var userModel = await http.Request.ReadFromJsonAsync<User>();

    var token = await userService.UserJwt(userModel);

    if (string.IsNullOrWhiteSpace(token))
    {
        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await http.Response.WriteAsJsonAsync(new { token = token });
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
