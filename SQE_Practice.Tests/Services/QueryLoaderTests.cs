using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using SQE_Practice.Services;
using Xunit;

namespace SQE_Practice.Tests.Services;

public class QueryLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public QueryLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SQETests_QL_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public async Task LoadAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);
        var loader = new QueryLoader(envMock.Object);

        Func<Task> act = async () => await loader.LoadAsync();

        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*Query Configuration Not Found*");
    }

    [Fact]
    public async Task LoadAsync_ValidJson_ReturnsStandingQueries()
    {
        var queriesDir = Path.Combine(_tempDir, "Queries");
        Directory.CreateDirectory(queriesDir);

        var json = @"{
            ""queries"": [
                {
                    ""name"": ""TestQuery1"",
                    ""eventName"": ""OrderFailed"",
                    ""Aggregration"": ""count"",
                    ""windowSeconds"": 120,
                    ""dimensions"": [""service"", ""region""],
                    ""alert"": {
                        ""operator"": ""gte"",
                        ""threshold"": 5,
                        ""cooldownSeconds"": 30,
                        ""enabled"": true
                    }
                }
            ]
        }";
        await File.WriteAllTextAsync(Path.Combine(queriesDir, "queries.json"), json);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);
        var loader = new QueryLoader(envMock.Object);

        var queries = await loader.LoadAsync();

        queries.Should().HaveCount(1);
        queries[0].Name.Should().Be("TestQuery1");
        queries[0].EventName.Should().Be("OrderFailed");
        queries[0].Aggregration.Should().Be("count");
        queries[0].WindowSeconds.Should().Be(120);
        queries[0].Dimensions.Should().ContainInOrder("service", "region");
        queries[0].Alert.Should().NotBeNull();
        queries[0].Alert!.Threshold.Should().Be(5);
        queries[0].Alert!.CooldownSeconds.Should().Be(30);
    }
}

