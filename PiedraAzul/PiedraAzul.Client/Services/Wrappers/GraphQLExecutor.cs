using PiedraAzul.Client.Models;
using PiedraAzul.Client.Services.GraphQLServices;

namespace PiedraAzul.Client.Services.Wrappers;

public static class GraphQLExecutor
{
    public static async Task<Result<T>> Execute<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return Result<T>.Success(result);
        }
        catch (GraphQLClientException ex)
        {
            var type = ex.Message.Contains("autenticad") || ex.Message.Contains("auth")
                ? "Auth"
                : "GraphQL";
            return Result<T>.Failure(new ErrorResult(ex.Message, type));
        }
        catch (HttpRequestException)
        {
            return Result<T>.Failure(new ErrorResult("Error de conexión", "Network"));
        }
        catch (Exception)
        {
            return Result<T>.Failure(new ErrorResult("Error inesperado", "System"));
        }
    }
}
