namespace MinimalWebAPi.DataAccess
{
    public record UserDto(int Id, string Name, string UserName, string Password, string Role);

    public record User(int Id, string Name, [Required] string UserName, [Required] string Password, string Role);

    public interface IUserRepositoryService
    {
        Task LoadUsers(List<User> users);
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

        public async Task<UserDto> GetUser(User userModel)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => string.Equals(x.UserName, userModel.UserName) && string.Equals(x.Password, userModel.Password));
            if (user == null)
                return null;

            var userDto = new UserDto(user.Id, user.Name, user.UserName, user.Password, user.Role);
            return userDto;
        }

        public async Task<UserDto> GetUserByUserName(string userName)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => string.Equals(x.UserName, userName));
            if (user == null)
                return null;

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
}
