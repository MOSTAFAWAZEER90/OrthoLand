using System.Text;
using OrthoFlowAI.Api.Storage;

namespace OrthoFlowAI.Tests;

public sealed class PrivateFileStoreTests : IDisposable
{
    private readonly string _base = Path.Combine(Path.GetTempPath(), "orthoflow-tests-" + Guid.NewGuid().ToString("N"));
    private readonly PrivateFileStore _store;

    public PrivateFileStoreTests()
    {
        _store = new PrivateFileStore(Path.Combine(_base, "private"), Path.Combine(_base, "wwwroot"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_base)) Directory.Delete(_base, recursive: true);
    }

    [Fact]
    public async Task Saves_outside_wwwroot_and_resolves_back()
    {
        var key = await _store.SaveAsync(new MemoryStream(Encoding.UTF8.GetBytes("img")), "photos", "clinic-1", ".JPG");

        Assert.Matches(@"^photos/clinic-1/[0-9a-f]{32}\.jpg$", key);
        var path = _store.ResolvePath(key)!;
        Assert.StartsWith(Path.Combine(_base, "private"), path);
        Assert.Equal("img", await File.ReadAllTextAsync(path));
    }

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("photos/../../outside.txt")]
    [InlineData("/uploads/../../secret.txt")]
    public void Path_traversal_is_rejected(string key)
    {
        Assert.Null(_store.ResolvePath(key));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Empty_key_resolves_to_null(string? key)
    {
        Assert.Null(_store.ResolvePath(key));
    }

    [Fact]
    public void Legacy_uploads_urls_still_resolve_inside_old_wwwroot()
    {
        var path = _store.ResolvePath("/uploads/photos/c1/a.jpg");
        Assert.Equal(Path.Combine(_base, "wwwroot", "uploads", "photos", "c1", "a.jpg"), path);
    }

    [Fact]
    public async Task Malicious_clinic_id_and_extension_are_sanitized()
    {
        var key = await _store.SaveAsync(new MemoryStream(), "photos", "../../evil", "./../x.html;.exe");

        Assert.DoesNotContain("..", key);
        Assert.NotNull(_store.ResolvePath(key));
    }
}
