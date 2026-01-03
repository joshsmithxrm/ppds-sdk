using FluentAssertions;
using PPDS.Auth.Profiles;
using Xunit;

namespace PPDS.Auth.Tests.Profiles;

public class ProfileStoreTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly ProfileStore _store;

    public ProfileStoreTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"test_profiles_{Guid.NewGuid()}.json");
        _store = new ProfileStore(_tempFilePath);
    }

    public void Dispose()
    {
        _store.Dispose();
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public async Task LoadAsync_FileDoesNotExist_ReturnsEmptyCollection()
    {
        var collection = await _store.LoadAsync();

        collection.Should().NotBeNull();
        collection.Count.Should().Be(0);
    }

    [Fact]
    public void Load_FileDoesNotExist_ReturnsEmptyCollection()
    {
        var collection = _store.Load();

        collection.Should().NotBeNull();
        collection.Count.Should().Be(0);
    }

    [Fact]
    public async Task SaveAsync_CreatesFile()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile { Name = "test" });

        await _store.SaveAsync(collection);

        File.Exists(_tempFilePath).Should().BeTrue();
    }

    [Fact]
    public void Save_CreatesFile()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile { Name = "test" });

        _store.Save(collection);

        File.Exists(_tempFilePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_LoadAsync_RoundTrip()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile
        {
            Name = "test",
            AuthMethod = AuthMethod.ClientSecret,
            ApplicationId = "app-id",
            TenantId = "tenant-id"
        });

        await _store.SaveAsync(collection);
        var loaded = await _store.LoadAsync();

        loaded.Count.Should().Be(1);
        loaded.All.First().Name.Should().Be("test");
        loaded.All.First().AuthMethod.Should().Be(AuthMethod.ClientSecret);
        loaded.All.First().ApplicationId.Should().Be("app-id");
        loaded.All.First().TenantId.Should().Be("tenant-id");
    }

    [Fact]
    public void Save_Load_RoundTrip()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile
        {
            Name = "test",
            AuthMethod = AuthMethod.ClientSecret,
            ApplicationId = "app-id"
        });

        _store.Save(collection);
        var loaded = _store.Load();

        loaded.Count.Should().Be(1);
        loaded.All.First().Name.Should().Be("test");
    }

    [Fact]
    public async Task SaveAsync_EncryptsSensitiveFields()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile
        {
            Name = "test",
            ClientSecret = "my-secret",
            Password = "my-password",
            CertificatePassword = "cert-password"
        });

        await _store.SaveAsync(collection);
        var json = await File.ReadAllTextAsync(_tempFilePath);

        json.Should().Contain("ENCRYPTED:");
        json.Should().NotContain("my-secret");
        json.Should().NotContain("my-password");
        json.Should().NotContain("cert-password");
    }

    [Fact]
    public async Task LoadAsync_DecryptsSensitiveFields()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile
        {
            Name = "test",
            ClientSecret = "my-secret",
            Password = "my-password",
            CertificatePassword = "cert-password"
        });

        await _store.SaveAsync(collection);
        _store.ClearCache();
        var loaded = await _store.LoadAsync();

        var profile = loaded.All.First();
        profile.ClientSecret.Should().Be("my-secret");
        profile.Password.Should().Be("my-password");
        profile.CertificatePassword.Should().Be("cert-password");
    }

    [Fact]
    public async Task SaveAsync_PreservesActiveIndex()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile { Name = "first" });
        collection.Add(new AuthProfile { Name = "second" }, setAsActive: true);

        await _store.SaveAsync(collection);
        var loaded = await _store.LoadAsync();

        loaded.ActiveProfile!.Name.Should().Be("second");
    }

    [Fact]
    public async Task LoadAsync_CachesResult()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile { Name = "test" });
        await _store.SaveAsync(collection);

        var first = await _store.LoadAsync();
        var second = await _store.LoadAsync();

        first.Should().BeSameAs(second);
    }

    [Fact]
    public async Task ClearCache_ForcesReload()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile { Name = "test" });
        await _store.SaveAsync(collection);

        var first = await _store.LoadAsync();
        _store.ClearCache();
        var second = await _store.LoadAsync();

        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public async Task SaveAsync_UpdatesCache()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile { Name = "test" });

        await _store.SaveAsync(collection);
        var loaded = await _store.LoadAsync();

        loaded.Should().BeSameAs(collection);
    }

    [Fact]
    public void Delete_RemovesFile()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile { Name = "test" });
        _store.Save(collection);

        _store.Delete();

        File.Exists(_tempFilePath).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ClearsCache()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile { Name = "test" });
        await _store.SaveAsync(collection);

        _store.Delete();
        var loaded = await _store.LoadAsync();

        loaded.Count.Should().Be(0);
    }

    [Fact]
    public void Delete_FileDoesNotExist_DoesNotThrow()
    {
        var act = () => _store.Delete();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task SaveAsync_NullCollection_Throws()
    {
        var act = async () => await _store.SaveAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Save_NullCollection_Throws()
    {
        var act = () => _store.Save(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveAsync_PreservesEnvironmentInfo()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile
        {
            Name = "test",
            Environment = EnvironmentInfo.Create("env-id", "https://test.crm.dynamics.com", "Test Env")
        });

        await _store.SaveAsync(collection);
        var loaded = await _store.LoadAsync();

        var env = loaded.All.First().Environment;
        env.Should().NotBeNull();
        env!.Id.Should().Be("env-id");
        env.Url.Should().Be("https://test.crm.dynamics.com");
        env.DisplayName.Should().Be("Test Env");
    }

    [Fact]
    public async Task SaveAsync_DoesNotDoubleEncrypt()
    {
        var collection = new ProfileCollection();
        collection.Add(new AuthProfile
        {
            Name = "test",
            ClientSecret = "my-secret"
        });

        await _store.SaveAsync(collection);
        _store.ClearCache();
        var loaded = await _store.LoadAsync();
        loaded.All.First().ClientSecret.Should().Be("my-secret");

        // Save again with the same collection
        await _store.SaveAsync(loaded);
        _store.ClearCache();
        var reloaded = await _store.LoadAsync();

        reloaded.All.First().ClientSecret.Should().Be("my-secret");
    }
}
