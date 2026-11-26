using System.Text.RegularExpressions;
using OrthoFlowAI.Api.Storage;

namespace OrthoFlowAI.Tests;

public class SignedFileUrlServiceTests
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static (long expires, string sig) Parse(string url)
    {
        var m = Regex.Match(url, @"expires=(\d+)&sig=([A-Za-z0-9_\-]+)$");
        Assert.True(m.Success, url);
        return (long.Parse(m.Groups[1].Value), m.Groups[2].Value);
    }

    [Fact]
    public void Valid_url_is_accepted()
    {
        var svc = new SignedFileUrlService("test-key-0123456789", TimeSpan.FromMinutes(10), new FixedClock(T0));
        var url = svc.CreateUrl("photos", "p1");

        Assert.StartsWith("/api/photos/p1/file?", url);
        var (expires, sig) = Parse(url);
        Assert.True(svc.IsValid("photos", "p1", expires, sig));
    }

    [Fact]
    public void Signature_for_one_photo_does_not_open_another()
    {
        var svc = new SignedFileUrlService("test-key-0123456789", TimeSpan.FromMinutes(10), new FixedClock(T0));
        var (expires, sig) = Parse(svc.CreateUrl("photos", "p1"));

        Assert.False(svc.IsValid("photos", "p2", expires, sig));
        Assert.False(svc.IsValid("documents", "p1", expires, sig));
    }

    [Fact]
    public void Extending_the_expiry_breaks_the_signature()
    {
        var svc = new SignedFileUrlService("test-key-0123456789", TimeSpan.FromMinutes(10), new FixedClock(T0));
        var (expires, sig) = Parse(svc.CreateUrl("photos", "p1"));

        Assert.False(svc.IsValid("photos", "p1", expires + 3600, sig));
    }

    [Fact]
    public void Expired_url_is_rejected()
    {
        var clock = new FixedClock(T0);
        var svc = new SignedFileUrlService("test-key-0123456789", TimeSpan.FromMinutes(10), clock);
        var (expires, sig) = Parse(svc.CreateUrl("photos", "p1"));

        clock.Now = T0.AddMinutes(11);
        Assert.False(svc.IsValid("photos", "p1", expires, sig));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-signature")]
    public void Missing_or_garbage_signature_is_rejected(string? sig)
    {
        var svc = new SignedFileUrlService("test-key-0123456789", TimeSpan.FromMinutes(10), new FixedClock(T0));
        Assert.False(svc.IsValid("photos", "p1", T0.AddMinutes(5).ToUnixTimeSeconds(), sig));
    }

    [Fact]
    public void Different_key_cannot_validate()
    {
        var a = new SignedFileUrlService("key-A-0123456789", TimeSpan.FromMinutes(10), new FixedClock(T0));
        var b = new SignedFileUrlService("key-B-0123456789", TimeSpan.FromMinutes(10), new FixedClock(T0));
        var (expires, sig) = Parse(a.CreateUrl("photos", "p1"));

        Assert.False(b.IsValid("photos", "p1", expires, sig));
    }
}
