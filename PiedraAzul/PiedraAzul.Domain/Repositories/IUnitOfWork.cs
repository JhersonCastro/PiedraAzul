using System.Threading;
using System.Threading.Tasks;

namespace PiedraAzul.Domain.Repositories
{
    public interface IUnitOfWork
    {
        Task<TResult> ExecuteAsync<TResult>(
            Func<CancellationToken, Task<TResult>> action,
            CancellationToken ct = default);
    }
}
