using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using System.Net.Http;

namespace SecureFileManager.Maui.Services
{
    public class TokenHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch
            {
                // SecureStorage may not be available in some environments; ignore
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
