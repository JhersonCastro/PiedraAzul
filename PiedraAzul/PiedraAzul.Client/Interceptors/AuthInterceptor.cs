using Grpc.Core;
using Grpc.Core.Interceptors;
using PiedraAzul.Client.Services;
namespace PiedraAzul.Client.Interceptors;
    public class AuthInterceptor : Interceptor
{
    private readonly ITokenService tokenService;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthInterceptor(ITokenService tokenService)
    {
        this.tokenService = tokenService;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        return HandleCall(request, context, continuation);
    }

    private AsyncUnaryCall<TResponse> HandleCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        var responseAsync = HandleResponse(request, context, continuation);

        return new AsyncUnaryCall<TResponse>(
            responseAsync,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    private async Task<TResponse> HandleResponse<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        where TRequest : class
        where TResponse : class
    {
        var token = await tokenService.GetAccessTokenAsync();

        var headers = new Metadata();

        foreach (var h in context.Options.Headers ?? Enumerable.Empty<Metadata.Entry>())
        {
            if (h.Key != "authorization")
                headers.Add(h);
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            headers.Add(new Metadata.Entry("authorization", $"Bearer {token}"));
        }

        var newOptions = context.Options.WithHeaders(headers);

        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method,
            context.Host,
            newOptions);

        try
        {
            var call = continuation(request, newContext);
            return await call.ResponseAsync;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
        {
            await _refreshLock.WaitAsync();
            try
            {
                var newToken = await tokenService.RefreshTokenAsync();

                if (string.IsNullOrWhiteSpace(newToken))
                    throw;

                var retryHeaders = new Metadata();

                foreach (var h in context.Options.Headers ?? Enumerable.Empty<Metadata.Entry>())
                {
                    if (h.Key != "authorization")
                        retryHeaders.Add(h);
                }

                retryHeaders.Add(new Metadata.Entry("authorization", $"Bearer {newToken}"));

                var retryOptions = context.Options.WithHeaders(retryHeaders);

                var retryContext = new ClientInterceptorContext<TRequest, TResponse>(
                    context.Method,
                    context.Host,
                    retryOptions);

                var retryCall = continuation(request, retryContext);

                return await retryCall.ResponseAsync;
            }
            finally
            {
                _refreshLock.Release();
            }
        }
    }
}