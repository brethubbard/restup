using Restup.HttpMessage;
using Restup.HttpMessage.Headers.Response;
using Restup.HttpMessage.Models.Contracts;
using Restup.HttpMessage.Models.Schemas;
using Restup.Webserver.Models.Contracts;
using Restup.WebServer.Logging;
using Restup.WebServer.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace Restup.Webserver.Http
{
    public class HttpServer : IDisposable
    {
        private readonly int _port;
        private StreamSocketListener _listener;
        private readonly SortedDictionary<RouteRegistration, IEnumerable<IMessageInspector>> _routes;
        private readonly ContentEncoderFactory _contentEncoderFactory;
        private ILogger _log;

        public HttpServer(int serverPort)
        {
            _log = LogManager.GetLogger<HttpServer>();
            _port = serverPort;
            _routes = new SortedDictionary<RouteRegistration, IEnumerable<IMessageInspector>>();
            _contentEncoderFactory = new ContentEncoderFactory();
        }

        public async Task StartServerAsync()
        {
            _listener = new StreamSocketListener();

            _listener.ConnectionReceived += ProcessRequestAsync;

            await _listener.BindServiceNameAsync(_port.ToString());

            _log.Info($"Webserver listening on port {_port}");
        }

        public void StopServer()
        {
            ((IDisposable)this).Dispose();

            _log.Info($"Webserver stopped listening on port {_port}");
        }

        /// <summary>
        /// Registers the <see cref="IRouteHandler"/> on the root url.
        /// </summary>
        /// <param name="restRoutehandler">The rest route handler to register.</param>
        public void RegisterRoute(IRouteHandler restRoutehandler, params IMessageInspector[] messageInspectors)
        {
            RegisterRoute("/", restRoutehandler, messageInspectors);
        }

        /// <summary>
        /// Registers the <see cref="IRouteHandler"/> on the specified url prefix.
        /// </summary>
        /// <param name="urlPrefix">The urlprefix to use, e.g. /api, /api/v001, etc. </param>
        /// <param name="restRoutehandler">The rest route handler to register.</param>
        public void RegisterRoute(string urlPrefix, IRouteHandler restRoutehandler, params IMessageInspector[] messageInspectors)
        {
            var routeRegistration = new RouteRegistration(urlPrefix, restRoutehandler);

            if (_routes.ContainsKey(routeRegistration))
            {
                throw new Exception($"RouteHandler already registered for prefix: {urlPrefix}");
            }

            _routes.Add(routeRegistration, messageInspectors ?? Enumerable.Empty<IMessageInspector>());
        }

        private async void ProcessRequestAsync(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            await Task.Run(async () =>
            {
                try
                {
                    using (var inputStream = args.Socket.InputStream)
                    {
                        var request = await MutableHttpServerRequest.Parse(inputStream);

                        var httpResponse = await HandleRequestAsync(request);

                        await WriteResponseAsync(httpResponse, args.Socket);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Exception while handling process: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        args.Socket.Dispose();
                    }
                    catch { }
                }
            });
        }

        internal async Task<HttpServerResponse> HandleRequestAsync(MutableHttpServerRequest request)
        {
            var route = _routes.FirstOrDefault(x => x.Key.Match(request));
            var routeRegistration = route.Key;
            var messageInspectors = route.Value;

            if (routeRegistration == null)
            {
                return HttpServerResponse.Create(new Version(1, 1), HttpResponseStatus.BadRequest);
            }

            var associatedObjects = await InvokeMessageInspectorsAfterReceivedRequestAsync(messageInspectors, request);

            var httpResponse = await routeRegistration.HandleAsync(request);

            await InvokeMessageInspectorsBeforeSendReplyAsync(messageInspectors, associatedObjects, httpResponse);

            return await AddContentEncodingAsync(httpResponse, request.AcceptEncodings);
        }

        private async Task<IReadOnlyDictionary<IMessageInspector, object>> InvokeMessageInspectorsAfterReceivedRequestAsync(IEnumerable<IMessageInspector> messageInspectors, MutableHttpServerRequest request)
        {
            SortedDictionary<IMessageInspector, object> associatedObjects = null;
            if (messageInspectors.Any())
            {
                associatedObjects = new SortedDictionary<IMessageInspector, object>();
                foreach (var messageInspector in messageInspectors)
                {
                    associatedObjects.Add(messageInspector, await messageInspector.AfterReceiveRequest(request));
                }
            }

            return associatedObjects;
        }

        private async Task InvokeMessageInspectorsBeforeSendReplyAsync(IEnumerable<IMessageInspector> messageInspectors, IReadOnlyDictionary<IMessageInspector, object> associatedObjects, HttpServerResponse httpResponse)
        {
            if (messageInspectors.Any())
            {
                foreach (var messageInspector in messageInspectors)
                {
                    await messageInspector.BeforeSendReply(httpResponse, associatedObjects[messageInspector]);
                }
            }
        }

        private async Task<HttpServerResponse> AddContentEncodingAsync(HttpServerResponse httpResponse, IEnumerable<string> acceptEncodings)
        {
            var contentEncoder = _contentEncoderFactory.GetEncoder(acceptEncodings);
            var encodedContent = await contentEncoder.Encode(httpResponse.Content);

            var newResponse = HttpServerResponse.Create(httpResponse.HttpVersion, httpResponse.ResponseStatus);

            foreach (var header in httpResponse.Headers)
            {
                newResponse.AddHeader(header);
            }
            newResponse.Content = encodedContent;
            newResponse.AddHeader(new ContentLengthHeader(encodedContent?.Length ?? 0));

            var contentEncodingHeader = contentEncoder.ContentEncodingHeader;
            AddHeaderIfNotNull(contentEncodingHeader, newResponse);

            return newResponse;
        }

        private static void AddHeaderIfNotNull(IHttpHeader contentEncodingHeader, HttpServerResponse newResponse)
        {
            if (contentEncodingHeader != null)
            {
                newResponse.AddHeader(contentEncodingHeader);
            }
        }

        private static async Task WriteResponseAsync(HttpServerResponse response, StreamSocket socket)
        {
            using (var output = socket.OutputStream)
            {
                await output.WriteAsync(response.ToBytes().AsBuffer());
                await output.FlushAsync();
            }
        }

        void IDisposable.Dispose()
        {
            _listener.ConnectionReceived -= ProcessRequestAsync;
            _listener.Dispose();
        }
    }
}
