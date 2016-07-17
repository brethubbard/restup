using Restup.HttpMessage;
using Restup.WebServer.Models.Contracts;
using System;
using System.Threading.Tasks;

namespace Restup.Webserver.UnitTests.TestHelpers
{
    internal class ReplaceContentMessageInspector : IMessageInspector
    {
        private byte[] _content;

        public ReplaceContentMessageInspector(byte[] fakeContent)
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
}
