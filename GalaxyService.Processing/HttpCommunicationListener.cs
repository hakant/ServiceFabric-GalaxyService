using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace GalaxyService.Processing
{
    public sealed class HttpCommunicationListener : ICommunicationListener
    {
        private readonly string _publishUri;
        private readonly HttpListener _httpListener;
        private readonly Func<HttpListenerContext, CancellationToken, Task> _processRequest;
        private readonly CancellationTokenSource _processRequestsCancellation = new CancellationTokenSource();

        public HttpCommunicationListener(string uriPrefix, string uriPublished, Func<HttpListenerContext, CancellationToken, Task> processRequest)
        {
            _publishUri = uriPublished;
            _processRequest = processRequest;
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(uriPrefix);
        }

        public void Abort()
        {
            _processRequestsCancellation.Cancel();
            _httpListener.Abort();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            _processRequestsCancellation.Cancel();
            _httpListener.Close();
            return Task.FromResult(true);
        }

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            _httpListener.Start();

            var openTask = ProcessRequestsAsync(_processRequestsCancellation.Token);

            return Task.FromResult(_publishUri);
        }

        private async Task ProcessRequestsAsync(CancellationToken processRequests)
        {
            while (!processRequests.IsCancellationRequested)
            {
                HttpListenerContext request = await _httpListener.GetContextAsync();

                // The ContinueWith forces rethrowing the exception if the task fails.
                Task requestTask = _processRequest(request, _processRequestsCancellation.Token)
                    .ContinueWith(async t => await t /* Rethrow unhandled exception */,
                        TaskContinuationOptions.OnlyOnFaulted);
            }
        }
    }
}