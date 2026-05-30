using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;

namespace PiedraAzul.GraphQL;

/// <summary>
/// Logs every GraphQL request and any resolver/execution errors.
/// Registered via AddDiagnosticEventListener in GraphQLExtensions.
/// </summary>
public sealed class GraphQLDiagnosticEventListener : ExecutionDiagnosticEventListener
{
    private readonly ILogger<GraphQLDiagnosticEventListener> _logger;

    public GraphQLDiagnosticEventListener(ILogger<GraphQLDiagnosticEventListener> logger)
    {
        _logger = logger;
    }

    // ── Request lifecycle ────────────────────────────────────────────────

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        // In HC 14 the parsed document is available on the context after parsing
        _logger.LogInformation("[GraphQL] Request started");
        return new RequestScope(_logger, context);
    }

    // ── Resolver errors ──────────────────────────────────────────────────

    public override void ResolverError(IMiddlewareContext context, IError error)
    {
        _logger.LogError(
            error.Exception,
            "[GraphQL] Resolver error in {Field}: {Message} | Path: {Path}",
            context.Selection.Field.Name,
            error.Message,
            string.Join("/", context.Path.ToList()));
    }

    // ── Request-level errors (syntax, validation, etc.) ──────────────────

    public override void RequestError(IRequestContext context, Exception exception)
    {
        _logger.LogError(exception,
            "[GraphQL] Request error: {Message}", exception.Message);
    }

    // ── Inner scope that logs when the request finishes ──────────────────

    private sealed class RequestScope : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IRequestContext _context;

        public RequestScope(ILogger logger, IRequestContext context)
        {
            _logger = logger;
            _context = context;
        }

        public void Dispose()
        {
            _logger.LogInformation("[GraphQL] Request finished");
        }
    }
}
