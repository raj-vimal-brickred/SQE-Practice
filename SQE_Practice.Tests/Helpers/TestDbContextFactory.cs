using Microsoft.EntityFrameworkCore;
using SQE_Practice.Storage;

namespace SQE_Practice.Tests.Helpers;

public class TestDbContextFactory : IDbContextFactory<SqeDbContext>
{
    private readonly DbContextOptions<SqeDbContext> _options;

    public TestDbContextFactory(string databaseName)
    {
        _options = new DbContextOptionsBuilder<SqeDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;
    }

    public SqeDbContext CreateDbContext()
    {
        return new SqeDbContext(_options);
    }

    public Task<SqeDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }
}

