
using System.Web;

namespace MotorTown
{
    internal class PasswordHandler : DelegatingHandler
    {
        private string? password;

        public PasswordHandler(string? password) : this(new HttpClientHandler(), password)
        {
        }

        public PasswordHandler(HttpMessageHandler innerHandler, string? password) : base(innerHandler)
        {
            this.password = password;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (password != null)
            {
                addPasword(request);
            }

            return base.Send(request, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (password != null)
            {
                addPasword(request);
            }

            return base.SendAsync(request, cancellationToken);
        }
        private void addPasword(HttpRequestMessage request)
        {
            var uriBuilder = new UriBuilder(request.RequestUri!);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query.Add("password", password);

            uriBuilder.Query = query.ToString();

            request.RequestUri = uriBuilder.Uri;
        }
    }
}