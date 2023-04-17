namespace MinimalWebAPi.DataAccess
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        [Required]
        public string? UserName { get; set; }
        [Required]
        public string? Password { get; set; }
        public string? Role { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    };
    public interface IUserRepositoryService
    {
        Task LoadUsers(List<User> users);
        Task SaveUser(User user);
        Task<UserDto> GetUser(User userModel);
        Task<UserDto> GetUserByUserName(string userName);
    }

    public class UserRepositoryService : IUserRepositoryService
    {
        private readonly MinimalApiDb _db;

        public UserRepositoryService(MinimalApiDb db)
        {
            _db = db;
        }
        public async Task LoadUsers(List<User> users)
        {
            await _db.Users.AddRangeAsync(users);
            await _db.SaveChangesAsync();
        }

        public async Task SaveUser(User userChanges)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => string.Equals(x.UserName, userChanges.UserName) && string.Equals(x.Password, userChanges.Password));
            
            user.Name = userChanges.Name;
            user.UserName = userChanges.UserName;
            user.Password = userChanges.Password;          
            user.Role = userChanges.Role;
            user.RefreshToken = userChanges.RefreshToken;
            user.RefreshTokenExpiryTime = userChanges.RefreshTokenExpiryTime;

            await _db.SaveChangesAsync();
        }

        public async Task<UserDto> GetUser(User userModel)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => string.Equals(x.UserName, userModel.UserName) && string.Equals(x.Password, userModel.Password));
            if (user == null)
                return null;

            var userDto = new UserDto
            {
                Id = user.Id, 
                Name = user.Name, 
                UserName = user.UserName,
                Password = user.Password,
                RefreshToken = user.RefreshToken,
                Role = user.Role, 
                RefreshTokenExpiryTime =  user.RefreshTokenExpiryTime
            };

            return userDto;
        }

        public async Task<UserDto> GetUserByUserName(string userName)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => string.Equals(x.UserName, userName));
            if (user == null) return null;

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                UserName = user.UserName,
                Password = user.Password,
                RefreshToken = user.RefreshToken,
                Role = user.Role,
                RefreshTokenExpiryTime = user.RefreshTokenExpiryTime
            };

            return userDto;
        }
    }

    public class MinimalApiDb : DbContext
    {
        public MinimalApiDb(DbContextOptions<MinimalApiDb> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
    }
}
