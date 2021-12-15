
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDbContext<MinimalApiDb>(opt => opt.UseInMemoryDatabase("MinimalApiDb"));

builder.Services.AddSingleton<ITokenService>(new TokenService());
builder.Services.AddScoped<IUserRepositoryService, UserRepositoryService>();
builder.Services.AddAuthorization();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

await using var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();


#region Define Endpoints
app.MapGet("/", async (MinimalApiDb db) => {

    List<User> users = new List<User>();
    users.Add(new(1, "John Wormald", "johnw", "P@ssw0rd!", "Boss"));
    users.Add(new(2, "Heather Wormald", "heth", "P@ssw0rd!", "Manager"));
    users.Add(new(3, "Hamish Wormald", "mish", "P@ssw0rd!", "Developer"));
    users.Add(new(4, "Harry Wormald", "harry", "P@ssw0rd!", "Developer"));
    users.Add(new(5, "Rosie Wormald", "rosie", "P@ssw0rd!", "Our Pet Dog"));

    await db.Users.AddRangeAsync(users);
    await db.SaveChangesAsync();

    return Results.Ok("This a demo for JWT Authentication using Minimalist Web API. Users loaded");
});

app.MapPost("/login", Login);

[AllowAnonymous]
async Task Login(HttpContext http, ITokenService tokenService, IUserRepositoryService userRepositoryService)
{
    var userModel = await http.Request.ReadFromJsonAsync<User>();
    var userDto = await userRepositoryService.GetUser(userModel);

    if (userDto == null)
    {
        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    var token = tokenService.BuildToken(builder.Configuration["Jwt:Key"], builder.Configuration["Jwt:Issuer"], builder.Configuration["Jwt:Audience"], userDto);
    await http.Response.WriteAsJsonAsync(new { token = token });
    return;
}

app.MapGet("/doaction", () => Results.Ok())
    .AllowAnonymous()
    .WithName("do_action");

app.MapGet("/devaction", [Authorize(Roles = "Developer")]() => "Dev Action Succeeded");

app.MapGet("/mgraction", [Authorize(Roles = "Manager")]() => "Manager Action Succeeded");

app.MapGet("/bossaction", [Authorize(Roles = "Boss")](HttpContext httpContext, ClaimsPrincipal user) => {
    var claims = httpContext.User.Claims;
    var firstClaim = user.FindFirstValue(ClaimTypes.Role);
    return Results.Ok($"{firstClaim} Action Succeeded.");
});
#endregion

app.UseSwaggerUI();

await app.RunAsync();

#region JWT
    public interface ITokenService
    {
        string BuildToken(string key, string issuer, string audience, UserDto user);
    }

    public class TokenService : ITokenService
    {
        private TimeSpan ExpiryDuration = new TimeSpan(0, 30, 0);
        public string BuildToken(string key, string issuer, string audience, UserDto user)
        {
            var claims = new[]
            {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                 };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(issuer, audience, claims,
                expires: DateTime.Now.Add(ExpiryDuration), signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
#endregion

#region Data Access
    public record UserDto(int Id, string Name, string UserName, string Password, string Role);

    public record User(int Id, string Name, [Required] string UserName, [Required] string Password, string Role);

    public interface IUserRepositoryService
    {
        Task<UserDto> GetUser(User userModel);
    }

    public class UserRepositoryService : IUserRepositoryService
    {
        private readonly MinimalApiDb _db;

        public UserRepositoryService(MinimalApiDb db)
        {
            _db = db;
        }
        public async Task<UserDto> GetUser(User userModel)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => string.Equals(x.UserName, userModel.UserName) && string.Equals(x.Password, userModel.Password));
            var userDto = new UserDto(user.Id, user.Name, user.UserName, user.Password, user.Role);
            return userDto;
        }
    }

    public class MinimalApiDb : DbContext
    {
        public MinimalApiDb(DbContextOptions<MinimalApiDb> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
    }
#endregion