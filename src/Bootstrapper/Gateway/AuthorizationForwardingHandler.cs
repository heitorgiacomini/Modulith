using System.Net.Http.Headers;

namespace Gateway;

public sealed class AuthorizationForwardingHandler(IHttpContextAccessor httpContextAccessor)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string? authorization = httpContextAccessor.HttpContext?
            .Request.Headers.Authorization
            .FirstOrDefault();

        if (AuthenticationHeaderValue.TryParse(authorization, out AuthenticationHeaderValue? header))
        {
            request.Headers.Authorization = header;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
