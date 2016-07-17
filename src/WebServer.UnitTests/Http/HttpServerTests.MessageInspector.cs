using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Restup.HttpMessage;
using Restup.Webserver.Http;
using Restup.Webserver.UnitTests.TestHelpers;
using System;
using System.Threading.Tasks;

namespace Restup.Webserver.UnitTests.Http
{
    [TestClass]
    public class HttpServerTests_MessageInspector
    {
        [TestMethod]
        public async Task GivenMessageInspectorChangesIncomingData_ThenHandleRequestShouldHaveThatData()
        {
            var newContent = new byte[] { 1, 2, 3 };
            var httpServer = new HttpServer(80);
            httpServer.RegisterRoute(new EchoRouteHandler());
            httpServer.RegisterMessageInspector(new ReplaceContentMessageInspector(newContent));
            var request = new MutableHttpServerRequest()
            {
                Uri = new Uri("http://localhost/"),
                Content = new byte[] { 0 }
            };

            var response = await httpServer.HandleRequestAsync(request);

            CollectionAssert.AreEqual(newContent, response.Content);
        }
    }
}
