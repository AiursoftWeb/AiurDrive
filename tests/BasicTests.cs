using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aiursoft.AiurDrive.Data;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.AiurDrive.Tests
{
    [TestClass]
    public class BasicTests
    {
        private readonly string _endpointUrl;
        private readonly int _port;
        private IHost _server;
        private HttpClient _http;

        public BasicTests()
        {
            _port = Network.GetAvailablePort();
            _endpointUrl = $"http://localhost:{_port}";
        }

        [TestInitialize]
        public async Task CreateServer()
        {
            _server = await App<Startup>(Array.Empty<string>(), port: _port).UpdateDbAsync<AiurDriveDbContext>(UpdateMode.RecreateThenUse);
            _http = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false
            });
            await _server.StartAsync();
        }

        [TestCleanup]
        public async Task CleanServer()
        {
            if (_server != null)
            {
                await _server.StopAsync();
                _server.Dispose();
            }
        }

        [TestMethod]
        public async Task GetHome()
        {
            var response = await _http.GetAsync(_endpointUrl);
            var location = response.Headers.Location?.ToString();

            Assert.AreEqual(
                $"https://directory.aiursoft.com/oauth/authorize?try-auth=True&appid=sample-app&redirect_uri=http%3A%2F%2Flocalhost%3A{_port}%2FAuth%2FAuthResult&state=%2FDashboard%2FIndex",
                location);
        }
    }
}
