namespace DevJourney.Domain.Common;

/// <summary>
/// Classe base para entidades: toda entidade tem identidade (Id) e datas de auditoria.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }

    protected void Touch(DateTimeOffset now) => UpdatedAt = now;
}
