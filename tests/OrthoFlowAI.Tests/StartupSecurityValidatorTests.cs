using Microsoft.Extensions.Configuration;
using OrthoFlowAI.Api.Security;

namespace OrthoFlowAI.Tests;

public class StartupSecurityValidatorTests
{
    private const string StrongKey = "a-strong-randomly-generated-key-for-tests-0123456789";
    private const string Db = "Host=db;Database=x;Username=u;Password=p";

    private static IConfiguration Config(params (string key, string? value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.key, v.value)))
            .Build();

    [Fact]
    public void Valid_production_config_passes()
    {
        var cfg = Config(("Jwt:SecretKey", StrongKey), ("ConnectionStrings:DefaultConnection", Db));
        Assert.Empty(StartupSecurityValidator.Validate(cfg, isDevelopment: false));
    }

    [Fact]
    public void Missing_jwt_key_fails()
    {
        var cfg = Config(("ConnectionStrings:DefaultConnection", Db));
        Assert.Contains(StartupSecurityValidator.Validate(cfg, false), e => e.Contains("Jwt:SecretKey"));
    }

    [Fact]
    public void Short_jwt_key_fails()
    {
        var cfg = Config(("Jwt:SecretKey", "short"), ("ConnectionStrings:DefaultConnection", Db));
        Assert.Contains(StartupSecurityValidator.Validate(cfg, true), e => e.Contains("at least"));
    }

    [Theory]
    [MemberData(nameof(PublicKeys))]
    public void Repository_keys_are_rejected_in_production(string key)
    {
        var cfg = Config(("Jwt:SecretKey", key), ("ConnectionStrings:DefaultConnection", Db));
        Assert.Contains(StartupSecurityValidator.Validate(cfg, isDevelopment: false), e => e.Contains("published"));
    }

    public static TheoryData<string> PublicKeys()
    {
        var data = new TheoryData<string>();
        foreach (var k in StartupSecurityValidator.PublicKeys.Where(k => k.Length >= 32)) data.Add(k);
        return data;
    }

    [Fact]
    public void Dev_key_is_allowed_in_development()
    {
        var cfg = Config(("Jwt:SecretKey", "OrthoFlowAI_DEV_ONLY_Key_NotForProduction_0123456789"),
                         ("ConnectionStrings:DefaultConnection", Db));
        Assert.Empty(StartupSecurityValidator.Validate(cfg, isDevelopment: true));
    }

    [Fact]
    public void Demo_seed_data_is_rejected_in_production()
    {
        var cfg = Config(("Jwt:SecretKey", StrongKey), ("ConnectionStrings:DefaultConnection", Db), ("Seed:DemoData", "true"));
        Assert.Contains(StartupSecurityValidator.Validate(cfg, false), e => e.Contains("DemoData"));
    }

    [Fact]
    public void Demo_seed_data_is_allowed_on_an_explicitly_confirmed_demo_site()
    {
        var cfg = Config(("Jwt:SecretKey", StrongKey), ("ConnectionStrings:DefaultConnection", Db),
                         ("Seed:DemoData", "true"), ("Seed:AllowDemoDataOutsideDevelopment", "true"));
        Assert.Empty(StartupSecurityValidator.Validate(cfg, false));
    }

    [Fact]
    public void Missing_connection_string_fails()
    {
        var cfg = Config(("Jwt:SecretKey", StrongKey));
        Assert.Contains(StartupSecurityValidator.Validate(cfg, true), e => e.Contains("DefaultConnection"));
    }

    [Fact]
    public void Committed_appsettings_contain_no_secrets()
    {
        // Guards against someone putting real secrets back into the shared appsettings.json.
        var root = FindRepoRoot();
        var cfg = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(root, "src", "OrthoFlowAI.Api", "appsettings.json"))
            .Build();

        Assert.True(string.IsNullOrEmpty(cfg["Jwt:SecretKey"]), "Jwt:SecretKey must be empty in appsettings.json");
        Assert.True(string.IsNullOrEmpty(cfg.GetConnectionString("DefaultConnection")), "Connection string must be empty in appsettings.json");
        Assert.True(string.IsNullOrEmpty(cfg["Seed:SuperAdminPassword"]), "SuperAdmin password must be empty in appsettings.json");
        Assert.False(cfg.GetValue<bool>("Seed:DemoData"));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OrthoFlowAI.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found");
    }
}
