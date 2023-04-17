using MinimalWebAPi.DataAccess;
using static System.Net.WebRequestMethods;

namespace MinimalWebAPi
{
    public interface IUserService
    {
        Task LoadUsers();
        Task<UserDto> GetUser(User userModel);
        Task SaveUser(UserDto userDto);
        Task<UserDto> GetUserByUserName(string userName);
    }
    public class UserService : IUserService
    {
        private IUserRepositoryService _repository;

        public UserService(IUserRepositoryService repository)
        {
            _repository = repository;
        }
        public async Task LoadUsers()
        {
            List<User> users = new List<User>
            {
                new User { Id = 1, Name = "John Wormald", UserName = "johnw", Password = "P@55w0rd!", Role = "Boss"},
                new User { Id = 2, Name = "Thomas", UserName = "tom", Password = "P@55w0rd!", Role = "Manager"},
                new User { Id = 3, Name = "Richard", UserName = "dick", Password = "P@55w0rd!", Role = "Developer"},
                new User { Id = 4, Name = "Harry", UserName = "harry", Password = "P@55w0rd!", Role = "User"}
            };

            await _repository.LoadUsers(users);
        }

        public async Task<UserDto> GetUser(User userModel)
        {
            return await _repository.GetUser(userModel);
        }

        public async Task SaveUser(UserDto userDto)
        {
            var user = new User
            {
                Id = userDto.Id,
                Name = userDto.Name,
                UserName = userDto.UserName,
                Password = userDto.Password,
                RefreshToken = userDto.RefreshToken,
                Role = userDto.Role,
                RefreshTokenExpiryTime = userDto.RefreshTokenExpiryTime
            };
            await _repository.SaveUser(user);
        }

        public async Task<UserDto> GetUserByUserName(string userName)
        {
            return await _repository.GetUserByUserName(userName);

        }

    }
}
