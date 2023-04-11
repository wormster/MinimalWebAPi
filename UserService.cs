using MinimalWebAPi.DataAccess;
using static System.Net.WebRequestMethods;

namespace MinimalWebAPi
{
    public interface IUserService
    {
        Task LoadUsers();
        Task<UserDto> GetUser(User userModel);
        Task<UserDto> GetUserByUserName(string userName);
        Task<string> UserJwt(User userModel);
    }
    public class UserService : IUserService
    {
        private IUserRepositoryService _repository;
        private ITokenService _tokenService;
        private IConfiguration _config;

        public UserService(IUserRepositoryService repository, ITokenService tokenService, IConfiguration config)
        {
            _repository = repository;
            _tokenService = tokenService;
            _config = config;
        }
        public async Task LoadUsers()
        {
            List<User> users = new List<User>
            {
                new(1, "John Wormald", "johnw", "P@55w0rd!", "Boss"),
                new(2, "Thomas", "tom", "P@55w0rd!", "Manager"),
                new(3, "Richard", "dick", "P@55w0rd!", "Developer"),
                new(4, "Harry", "harry", "P@55w0rd!", "User")
            };

            await _repository.LoadUsers(users);
        }

        public async Task<UserDto> GetUser(User userModel)
        {
            return await _repository.GetUser(userModel);
        }

        public async Task<UserDto> GetUserByUserName(string userName)
        {
            return await _repository.GetUserByUserName(userName);

        }

        public async Task<string> UserJwt(User userModel)
        {
            if (userModel == null)
                return string.Empty;

            var userDto = await this.GetUser(userModel);

            var token = _tokenService.BuildToken(
                _config["Jwt:Key"] ?? string.Empty,
                _config["Jwt:Issuer"] ?? string.Empty,
                _config["Jwt:Audience"] ?? string.Empty,
                userDto);

           return token;
        }

    }
}
