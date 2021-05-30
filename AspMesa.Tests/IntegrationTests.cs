using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AspMesa.Tests
{
    // Полезные ссылки:
    // - Простые тесты для ASP.NET:
    //     https://docs.microsoft.com/ru-ru/dotnet/architecture/microservices/multi-container-microservice-net-applications/test-aspnet-core-services-web-apps
    // - Более сложные тесты, хотя я здесь это не использую:
    //     https://docs.microsoft.com/ru-ru/aspnet/core/test/integration-tests?view=aspnetcore-5.0
    // - Просто интересная штука о том, как сохранять состояние между тестами.
    //     https://xunit.net/docs/shared-context
    //     Кстати, перед запуском каждого теста вызывается конструктор этого класса, а значит состояние всего сервера исчезает.
    // - О том, что такое InlineData и Theory:
    //     https://andrewlock.net/creating-parameterised-tests-in-xunit-with-inlinedata-classdata-and-memberdata/
    // - Пара ссылок про наименование тестов:
    //     https://docs.microsoft.com/ru-ru/dotnet/core/testing/unit-testing-best-practices#naming-your-tests
    //     https://stackoverflow.com/questions/155436/unit-test-naming-best-practices

    public class IntegrationTests
    {
        private readonly HttpClient _client;
        private readonly TestStorage _storage;

        public IntegrationTests()
        {
            // Мы не хотим работать с файловой системой во время тестов,
            // поэтому будем использовать простенький TestStorage
            _storage = new TestStorage();

            // Быстренько поднимаем тестовый сервер и создаём HttpClient, чтобы в него тыкать.
            var server = new TestServer(new WebHostBuilder()
                .ConfigureServices(services => services.AddSingleton(typeof(IStorage), _storage))
                .UseStartup<Startup>());
            _client = server.CreateClient();
        }

        #region InitRandom

        /// <summary>
        ///     Отправляет POST в InitRandom, не передавая seed, и проверяет появление данных.
        /// </summary>
        [Fact]
        public async void InitRandom_Empty_Success()
        {
            // Arrange.
            var url = "/api/InitRandom";
            var seed = new StringContent("");

            // Act.
            var response = await _client.PostAsync(url, seed);

            // Assert.
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        ///     Отправляет в InitRandom переданный seed, проверяет появление данных,
        ///     проверяет что они ожидаемые для этого seed.
        /// </summary>
        [Fact]
        public async void InitRandom_Seed_Success()
        {
            // Arrange.
            var url = "/api/InitRandom";
            var seed = new StringContent("123");

            // Act.
            var response = await _client.PostAsync(url, seed);

            // Assert.
            response.EnsureSuccessStatusCode();
            // TODO
        }

        /// <summary>
        ///     Отправляет в InitRandom некорректный seed и ожидает HTTP 400.
        /// </summary>
        [Fact]
        public async void InitRandom_InvalidSeed_BadRequest()
        {
            // Arrange.
            var url = "/api/InitRandom";
            var data = new StringContent("foobar");

            // Act.
            var response = await _client.PostAsync(url, data);

            // Assert.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        #endregion

        #region CreateUser

        /// <summary>
        ///     Создаём пользователя и проверяем, что регистрация не выдала ошибок.
        /// </summary>
        [Theory]
        [InlineData("Ivan Pupkin", "pupkin@staff.hse.ru")]
        [InlineData("False", "eva@localhost")]
        [InlineData("I'm using pluses!", "plus+plus@invalid.email")]
        public async Task CreateUser(string name, string email)
        {
            // Arrange
            var url = "/api/RegisterUser";
            var request = $@"{{ ""UserName"": ""{name}"", ""Email"": ""{email}"" }}";

            // Act
            var response = await _client.PostAsync(url, new StringContent(request));
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(@"{ }", content);

            Assert.NotEmpty(_storage.Users);
            var user = _storage.Users.Last();
            Assert.Equal(name, user.UserName);
            Assert.Equal(email, user.Email);
        }

        /// <summary>
        ///     Создаём одного и того же пользователя дважды. Должна быть ошибка 400: "Email already taken"
        /// </summary>
        [Fact]
        public async Task CreateUserTwice()
        {
            // Arrange
            var url = "/api/RegisterUser";
            var request = @"{{ ""UserName"": ""Dolly"", ""Email"": ""sheep@dolly.com"" }}";

            // Act
            var first = await _client.PostAsync(url, new StringContent(request));
            var firstContent = await first.Content.ReadAsStringAsync();

            var second = await _client.PostAsync(url, new StringContent(request));
            var secondContent = await first.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(@"{ }", firstContent);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
            Assert.Equal(@"{ ""error"": ""Email already taken"" }", secondContent);

            var user = Assert.Single(_storage.Users);
            Assert.Equal("Dolly", user.UserName);
            Assert.Equal("sheep@dolly.com", user.Email);
        }

        /// <summary>
        ///     Создаём двух пользователей с одним именем, но разными емайлами. Так делать можно.
        /// </summary>
        [Fact]
        public async Task CreateUserSameName()
        {
            await CreateUser("Bob", "bob@gmail.com");
            await CreateUser("Bob", "bob@yandex.ru");

            Assert.Equal(2, _storage.Users.Count);
        }

        #endregion

        #region GetUserInfo

        /// <summary>
        ///     Создаём пользователя и проверяем, что возвращается правильная информация о пользователе.
        /// </summary>
        [Fact]
        public async Task GetUserInfo()
        {
            // Arrange
            var url = "/api/user/foo@bar";
            await CreateUser("John Snow", "foo@bar");

            // Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            // Assert.
            response.EnsureSuccessStatusCode();
            Assert.Equal(@"{ ""UserName"": ""John Snow"", ""Email"": ""foo@bar"" }", content);
        }

        /// <summary>
        ///     Пробуем получить информацию по несуществующему пользователю. Ожидается 404.
        /// </summary>
        [Fact]
        public async Task UserInfoNotFound()
        {
            // Arrange
            var url = "/api/user/notregistered@ya.ru";

            // Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            // Assert.
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(@"{ ""error"": ""User not found"" }", content);
        }

        #endregion

        #region GetUsersList

        /// <summary>
        ///     Создаём пользователей и проверяем, что список их содержит в правильном порядке.
        /// </summary>
        [Fact]
        public async Task GetUsersList()
        {
            // Arrange
            var url = "/api/users";
            await CreateUser("BBB_First User", "1st@example.org");
            await CreateUser("AAA_Second User", "2nd@random.email");

            // Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            // Assert.
            response.EnsureSuccessStatusCode();
            Assert.Equal(@"[
                { ""UserName"": ""AAA_Second User"", ""Email"": ""2nd@random.email"" },
                { ""UserName"": ""BBB_First User"", ""Email"": ""2st@example.org"" }
            ]", content);
        }

        /// <summary>
        ///     Если пользователей нет, то список пользователей должекн быть пуст.
        /// </summary>
        [Fact]
        public async Task EmptyUsersList()
        {
            // Arrange
            var url = "/api/users";

            // Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            // Assert.
            response.EnsureSuccessStatusCode();
            Assert.Equal(@"[]", content);
        }

        // TODO:  Пагинация

        #endregion

        #region SendMessage

        /// <summary>
        ///     Просто отправляет одно сообщение от одного существующего пользователя другому.
        /// </summary>
        [Fact]
        public async Task SendMessage()
        {
            // Arrange
            await CreateUser("Real John", "John@example.org");
            await CreateUser("Not a Bob", "Bob@hse.ru");
            var url = "/api/Send";
            var message = @"Subject: Test message
From: John@example.org
To: Bob@hse.ru

Hello world!!
This is a second line.
";

            // Act
            var response = await _client.PostAsync(url, new StringContent(message));
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("{}", content);

            var mail = Assert.Single(_storage.Messages);
            Assert.Equal("Test message", mail.Subject);
            Assert.Equal("John@example.org", mail.SenderId);
            Assert.Equal("Bob@hse.ru", mail.ReceiverId);
            Assert.Equal("Hello world!!\nThis is a second line.\n", mail.Message);
        }

        /// <summary>
        ///     Отправляет письмо от несуществующего отправителя существующему получателю.
        /// </summary>
        [Fact]
        public async Task SenderNotFound()
        {
            // Arrange
            await CreateUser("Not a Bob", "Bob@hse.ru");
            var url = "/api/Send";
            var message = "Subject: Test message\nFrom: John@example.org\nTo: Bob@hse.ru\n\nHi!";

            // Act
            var response = await _client.PostAsync(url, new StringContent(message));
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(@"{""error"": ""Sender not found""}", content);
            Assert.Empty(_storage.Messages);
        }

        /// <summary>
        ///     Отправляет письмо от существующего отправителя несуществующему получателю.
        /// </summary>
        [Fact]
        public async Task ReceiverNotFound()
        {
            // Arrange
            await CreateUser("Real John", "John@example.org");
            var url = "/api/Send";
            var message = "Subject: Test message\nFrom: John@example.org\nTo: Bob@hse.ru\n\nHi!";

            // Act
            var response = await _client.PostAsync(url, new StringContent(message));
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(@"{""error"": ""Receiver not found""}", content);
            Assert.Empty(_storage.Messages);
        }

        #endregion

        // TODO: Список сообщений
    }
}