using System.Net;
using Aiursoft.AiurDrive.Entities;
using Microsoft.Extensions.Hosting;

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
            _server = await (await AppAsync<Startup>([], port: _port)).UpdateDbAsync<AiurDriveDbContext>();
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
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
