using Grpc.Core;
using PiedraAzul.Client.Models;

namespace PiedraAzul.Client.Wrappers
{
    public static class GrpcExecutor
    {
        public static async Task<Result<T>> Execute<T>(Func<Task<T>> action)
        {
            try
            {
                var result = await action();
                return Result<T>.Success(result);
            }
            catch (RpcException ex)
            {
                var message = string.IsNullOrWhiteSpace(ex.Status.Detail)
                    ? "Error en la solicitud"
                    : ex.Status.Detail;

                var type = ex.StatusCode switch
                {
                    StatusCode.Unauthenticated => "Auth",
                    StatusCode.Unavailable => "Network",
                    _ => "Grpc"
                };

                return Result<T>.Failure(new ErrorResult(
                    Message: message,
                    Code: ex.StatusCode,
                    Type: type
                ));
            }
            catch (Exception)
            {
                return Result<T>.Failure(new ErrorResult(
                    Message: "Error inesperado",
                    Code: StatusCode.Unknown,
                    Type: "System"
                ));
            }
        }
    }
}
