
namespace MatsyaSetu.Application.Services
{
    // Transient service
    public interface ITransientService
    {
        Guid Id { get; }
    }

    public class TransientService : ITransientService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    // Scoped service
    public interface IScopedService
    {
        Guid Id { get; }
    }

    public class ScopedService : IScopedService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    // Singleton service
    public interface ISingletonService
    {
        Guid Id { get; }
    }

    public class SingletonService : ISingletonService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }
}
