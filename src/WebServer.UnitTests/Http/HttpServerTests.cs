using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Restup.HttpMessage;
using Restup.HttpMessage.Models.Schemas;
using Restup.Webserver.Http;
using Restup.WebServer.Models.Contracts;
using System;
using System.Threading.Tasks;

namespace Restup.Webserver.UnitTests.Http
{
    [TestClass]
    public class HttpServerTests
    {
        [TestMethod]
        public void HandleRequest_RegisteredOnDefaultRoute_RoutesSuccesfully()
        {
            new HttpServerFluentTests()
                .Given
                    .ListeningOnDefaultRoute()
                .When
                    .RequestHasArrived("/Get")
                .Then
                    .AssertRouteHandlerReceivedRequest()
                    .AssertLastResponse(x => x.ResponseStatus, HttpResponseStatus.OK);
        }

        [TestMethod]
        [DataRow("api")]
        [DataRow("/api")]
        [DataRow("api/")]
        public void HandleRequest_RegisteredOnPrefixedRoute_RoutesSuccesfully(string registeredPrefix)
        {
            new HttpServerFluentTests()
               .Given
                   .ListeningOnRoute(registeredPrefix)
               .When
                   .RequestHasArrived("/api/Get")
               .Then
                   .AssertRouteHandlerReceivedRequest()
                   .AssertRouteHandlerRequest(x => x.Uri, new Uri("/Get", UriKind.Relative))
                   .AssertLastResponse(x => x.ResponseStatus, HttpResponseStatus.OK);
        }

        [TestMethod]
        public void HandleRequest_OnNonRegisteredRoute_ReturnsBadRequest()
        {
            new HttpServerFluentTests()
               .When
                   .RequestHasArrived("/api/Get")
               .Then
                   .AssertLastResponse(x => x.ResponseStatus, HttpResponseStatus.BadRequest);
        }

        [TestMethod]
        public void GivenMultipleRouteHandlersAreAddedInSequentialOrder_WhenRequestIsReceivedOnApiRoute_ThenRequestIsSuccesfullyReceived()
        {
            new HttpServerFluentTests()
               .Given
                   .ListeningOnDefaultRoute()
                   .ListeningOnRoute("/api")
               .When
                   .RequestHasArrived("/api/Get")
               .Then
                   .AssertRouteHandlerReceivedNoRequests(string.Empty)
                   .AssertRouteHandlerReceivedRequest("/api")
                   .AssertRouteHandlerRequest("/api", x => x.Uri, new Uri("/Get", UriKind.Relative))
                   .AssertLastResponse(x => x.ResponseStatus, HttpResponseStatus.OK);
        }

        [TestMethod]
        public void GivenMultipleRouteHandlersAreAddedInSequentialOrder_WhenRequestIsReceivedOnAnyRoute_ThenRequestIsSuccesfullyReceived()
        {
            new HttpServerFluentTests()
               .Given
                   .ListeningOnDefaultRoute()
                   .ListeningOnRoute("/api")
               .When
                   .RequestHasArrived("/index.html")
               .Then
                   .AssertRouteHandlerReceivedNoRequests("/api")
                   .AssertRouteHandlerReceivedRequest(string.Empty)
                   .AssertRouteHandlerRequest(string.Empty, x => x.Uri, new Uri("/index.html", UriKind.Relative))
                   .AssertLastResponse(x => x.ResponseStatus, HttpResponseStatus.OK);
        }

        [TestMethod]
        public void GivenMultipleRouteHandlersAreAddedInReverseOrder_WhenRequestIsReceivedOnApiRoute_ThenRequestIsSuccesfullyReceived()
        {
            new HttpServerFluentTests()
               .Given
                   .ListeningOnRoute("/api")
                   .ListeningOnDefaultRoute()
               .When
                   .RequestHasArrived("/api/Get")
               .Then
                   .AssertRouteHandlerReceivedNoRequests(string.Empty)
                   .AssertRouteHandlerReceivedRequest("/api")
                   .AssertRouteHandlerRequest("/api", x => x.Uri, new Uri("/Get", UriKind.Relative))
                   .AssertLastResponse(x => x.ResponseStatus, HttpResponseStatus.OK);
        }

        [TestMethod]
        public void GivenMultipleRouteHandlersAreAddedInReverseOrder_WhenRequestIsReceivedOnAnyRoute_ThenRequestIsSuccesfullyReceived()
        {
            new HttpServerFluentTests()
               .Given
                   .ListeningOnRoute("/api")
                   .ListeningOnDefaultRoute()
               .When
                   .RequestHasArrived("/index.html")
               .Then
                   .AssertRouteHandlerReceivedNoRequests("/api")
                   .AssertRouteHandlerReceivedRequest(string.Empty)
                   .AssertRouteHandlerRequest(string.Empty, x => x.Uri, new Uri("/index.html", UriKind.Relative))
                   .AssertLastResponse(x => x.ResponseStatus, HttpResponseStatus.OK);
        }

        [TestMethod]
        public void GivenMultipleRouteHandlersAreBeingAddedWithTheSamePrefix_ThenAnExceptionShouldBeThrown()
        {
            var httpServer = new HttpServer(80);
            httpServer.RegisterRoute("api", new EchoRouteHandler());

            Assert.ThrowsException<Exception>(() => httpServer.RegisterRoute("api", new EchoRouteHandler()));
        }

        [TestMethod]
        public void GivenMultipleRouteHandlersAreBeingAddedOnTheCatchAllRoute_ThenAnExceptionShouldBeThrown()
        {
            var httpServer = new HttpServer(80);
            httpServer.RegisterRoute(new EchoRouteHandler());

            Assert.ThrowsException<Exception>(() => httpServer.RegisterRoute(new EchoRouteHandler()));
        }

        private class DummyInspector : IMessageInspector
        {
            private byte[] _content;

            public DummyInspector(byte[] fakeContent)
            {
                _content = fakeContent;
            }

            public Task<object> AfterReceiveRequest(MutableHttpServerRequest request)
            {
                request.Content = _content;

                return Task.FromResult<object>(null);
            }

            public Task BeforeSendReply(HttpServerResponse response, object correlationObject)
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public async Task GivenMessageInspectorChangesIncomingData_ThenHandleRequestShouldHaveThatData()
        {
            var newContent = new byte[] { 1, 2, 3 };
            var httpServer = new HttpServer(80);
            httpServer.RegisterRoute(new EchoRouteHandler());
            httpServer.RegisterMessageInspector(new DummyInspector(newContent));
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
