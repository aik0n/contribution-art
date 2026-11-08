using System.Net;
using System.Net.Http.Headers;

namespace ContributionArt.Tests.TestDoubles
{
    /// <summary>
    /// Returns a preset response and records the request. The values are copied during the send
    /// because the client disposes the request as soon as the call ends.
    /// </summary>
    internal sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode _statusCode = HttpStatusCode.OK;
        private string _responseBody = "{}";
        private HttpMethod? _lastMethod;
        private Uri? _lastUri;
        private AuthenticationHeaderValue? _lastAuthorization;
        private string? _lastBody;

        public HttpMethod? LastMethod
        {
            get { return _lastMethod; }
        }

        public Uri? LastUri
        {
            get { return _lastUri; }
        }

        public AuthenticationHeaderValue? LastAuthorization
        {
            get { return _lastAuthorization; }
        }

        public string? LastBody
        {
            get { return _lastBody; }
        }

        public void RespondWith(HttpStatusCode statusCode, string body)
        {
            _statusCode = statusCode;
            _responseBody = body;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _lastMethod = request.Method;
            _lastUri = request.RequestUri;
            _lastAuthorization = request.Headers.Authorization;
            _lastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode) { Content = new StringContent(_responseBody) };
        }
    }
}
