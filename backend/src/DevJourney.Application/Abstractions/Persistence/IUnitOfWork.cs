namespace DevJourney.Application.Abstractions.Persistence;

/// <summary>
/// Unit of Work: agrupa as alterações feitas pelos repositórios e as confirma em uma única transação.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
