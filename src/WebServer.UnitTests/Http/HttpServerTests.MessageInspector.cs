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
        public async Task RegisterRoute_WithMessageInspector_RequestMutated()
        {
            // single inspector who mutates incoming request
            var newContent = new byte[] { 1, 2, 3 };
            var httpServer = new HttpServer(80);
            httpServer.RegisterRoute(new EchoRouteHandler(), new ReplaceContentMessageInspector(newContent));
            var request = new MutableHttpServerRequest()
            {
                Uri = new Uri("http://localhost/"),
                Content = new byte[] { 0 }
            };

            var response = await httpServer.HandleRequestAsync(request);

            CollectionAssert.AreEqual(newContent, response.Content);
        }

        [TestMethod]
        public async Task RegisterRoute_WithMessageInspector_AssociatedObjectPassed()
        {
            // single inspector with associated object
            Assert.Fail();
        }

        [TestMethod]
        public async Task RegisterRoute_WithMessageInspector_AssociatedObjectCorrelated()
        {
            // multiple inspectors, with and without correlated objects
            Assert.Fail();
        }

        [TestMethod]
        public async Task RegisterRoute_WithMessageInspector_ExceptionThrown()
        {
            // throw exception in inspector, should be rethrown or not?
            Assert.Fail();
        }

        [TestMethod]
        public async Task RegisterRoute_WithMessageInspector_ResponseMutated()
        {
            // inspector BeforeSend mutates response
            Assert.Fail();
        }
    }
}
