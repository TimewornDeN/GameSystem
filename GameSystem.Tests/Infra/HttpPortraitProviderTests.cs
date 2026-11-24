using System.Text;
using GameSystem.Infra.WebApi;
using GameSystem.Tests.TestDoubles;
using Xunit;

namespace GameSystem.Tests.Infra;

public class HttpPortraitProviderTests
{
    private static HttpPortraitProvider CreateProvider(StubHttpMessageHandler handler, string cacheDirectory)
        => new(new HttpClient(handler) { BaseAddress = new Uri("https://genshin.jmp.blue/") }, cacheDirectory);

    private static string NewTempDirectory() => Path.Combine(Path.GetTempPath(), "gamesystem-portraits-" + Guid.NewGuid().ToString("N"));

    private static void Cleanup(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public async Task GetPortraitAsync_DownloadsThePicture_AndLaterServesItFromTheCache()
    {
        string cache = NewTempDirectory();
        try
        {
            var online = CreateProvider(new StubHttpMessageHandler().RespondWith("characters/albedo/icon-big", "PICTURE"), cache);

            byte[]? first = await online.GetPortraitAsync("albedo");

            Assert.Equal(Encoding.UTF8.GetBytes("PICTURE"), first);

            // A provider that knows no pictures at all: only the cache folder can answer.
            var offline = CreateProvider(new StubHttpMessageHandler(), cache);
            Assert.Equal(first, await offline.GetPortraitAsync("albedo"));
        }
        finally
        {
            Cleanup(cache);
        }
    }

    [Fact]
    public async Task GetPortraitAsync_NamesTheFileByTheRealImageFormat_AndTellsWhereItLies()
    {
        string cache = NewTempDirectory();
        try
        {
            // The stub sends text (UTF-8), so a format whose first bytes are plain ASCII is used: GIF starts with "GIF8".
            string gif = "GIF89a-----";
            var provider = CreateProvider(new StubHttpMessageHandler().RespondWith("characters/albedo/icon-big", gif), cache);

            Assert.Null(provider.GetLocalPath("albedo"));

            Assert.NotNull(await provider.GetPortraitAsync("albedo"));

            string? path = provider.GetLocalPath("albedo");
            Assert.NotNull(path);
            Assert.EndsWith(".gif", path);
            Assert.True(File.Exists(path));
            Assert.Null(provider.GetLocalPath("keqing"));
        }
        finally
        {
            Cleanup(cache);
        }
    }

    [Fact]
    public async Task GetPortraitAsync_ReturnsNull_WhenTheServerHasNoPicture()
    {
        string cache = NewTempDirectory();
        try
        {
            var provider = CreateProvider(new StubHttpMessageHandler(), cache);

            Assert.Null(await provider.GetPortraitAsync("nobody"));
            Assert.False(Directory.Exists(cache) && Directory.GetFiles(cache).Length > 0);
        }
        finally
        {
            Cleanup(cache);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPortraitAsync_ReturnsNull_ForAnEmptyId(string id)
    {
        string cache = NewTempDirectory();
        try
        {
            var provider = CreateProvider(new StubHttpMessageHandler(), cache);

            Assert.Null(await provider.GetPortraitAsync(id));
        }
        finally
        {
            Cleanup(cache);
        }
    }

    [Fact]
    public async Task GetPortraitAsync_DoesNotLetAnIdEscapeTheCacheFolder()
    {
        string cache = NewTempDirectory();
        try
        {
            var handler = new StubHttpMessageHandler().RespondWith("characters/..%2F..%2Fevil/icon-big", "PICTURE");
            var provider = CreateProvider(handler, cache);

            await provider.GetPortraitAsync("../../evil");

            string parent = Path.GetFullPath(Path.Combine(cache, ".."));
            Assert.DoesNotContain(Directory.GetFiles(parent, "evil*"), _ => true);
        }
        finally
        {
            Cleanup(cache);
        }
    }
}
