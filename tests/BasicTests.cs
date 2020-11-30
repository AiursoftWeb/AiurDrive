using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;
using static Aiursoft.WebTools.Extends;

namespace AiurDrive.Tests
{
    [TestClass]
    public class BasicTests
    {
        private readonly string _endpointUrl = $"http://localhost:{_port}";
        private const int _port = 15999;
        private IHost _server;
        private HttpClient _http;
        private ServiceCollection _services;
        private ServiceProvider _serviceProvider;

        [TestInitialize]
        public async Task CreateServer()
        {
            _server = App<Startup>(port: _port);
            _http = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            });
            _services = new ServiceCollection();
            _services.AddHttpClient();
            await _server.StartAsync();
            _serviceProvider = _services.BuildServiceProvider();
        }

        [TestCleanup]
        public async Task CleanServer()
        {
            await _server.StopAsync();
            _server.Dispose();
        }

        [TestMethod]
        public async Task GetHome()
        {
            var response = await _http.GetAsync(_endpointUrl);
            var location = response.Headers.Location.ToString();

            Assert.AreEqual(
                "https://gateway.aiursoft.com/oauth/authorize?try-auth=True&appid=aaaaa&redirect_uri=http%3A%2F%2Flocalhost%3A15999%2FAuth%2FAuthResult&state=%2FDashboard%2FIndex",
                location);
        }
    }
}
