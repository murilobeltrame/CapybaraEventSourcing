namespace CapybaraEventSource.Domain.Shared;

public interface IRepository<T>
{
    Task<bool> Has(string code, CancellationToken cancellationToken);
    
    Task<T?> Get(string code, CancellationToken cancellationToken);
}