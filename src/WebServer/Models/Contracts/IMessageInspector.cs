using Restup.HttpMessage;
using System.Threading.Tasks;

namespace Restup.WebServer.Models.Contracts
{
    public interface IMessageInspector
    {
        Task<object> AfterReceiveRequest(MutableHttpServerRequest request);

        Task BeforeSendReply(HttpServerResponse response, object correlationObject);
    }
}
