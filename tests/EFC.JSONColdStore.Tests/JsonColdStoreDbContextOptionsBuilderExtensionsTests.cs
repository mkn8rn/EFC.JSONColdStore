using EFC.JSONColdStore;
using EFC.JSONColdStore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EFC.JSONColdStore.Tests;

public sealed class JsonColdStoreDbContextOptionsBuilderExtensionsTests
{
    [Fact]
    public void UseJsonColdStoreDatabaseStoresValidatedProviderOptions()
    {
        var directory = TestDirectory("provider-options");
        var builder = new DbContextOptionsBuilder();

        builder.UseJsonColdStoreDatabase(directory);

        var extension = builder.Options.FindExtension<JsonColdStoreOptionsExtension>();

        Assert.NotNull(extension);
        Assert.Equal(Path.GetFullPath(directory), extension.Options.DatabaseDirectory);
        Assert.Equal(JsonColdStoreCompression.Auto, extension.Options.Compression);
        Assert.Equal(JsonColdStoreStartupMode.MetadataOnly, extension.Options.StartupMode);
        Assert.Equal(JsonColdStoreScanPolicy.FailUnlessExplicit, extension.Options.FullScanPolicy);
        Assert.True(extension.Info.IsDatabaseProvider);
        Assert.Equal("using JSONColdStore ", extension.Info.LogFragment);
    }

    [Fact]
    public void UseJsonColdStoreDatabaseAppliesAdvancedConfigurationDelegate()
    {
        using var key = JsonColdStoreEncryptionKey.FromBytes(new byte[32]);
        var builder = new DbContextOptionsBuilder<TestDbContext>();

        builder.UseJsonColdStoreDatabase(
            TestDirectory("advanced-provider-options"),
            store => store
                .UseCompression(JsonColdStoreCompression.Brotli)
                .UseEncryption(new JsonColdStoreEncryptionOptions
                {
                    Key = key,
                    KeyId = "test-key",
                    RequireEncryptedStore = true,
                })
                .UseFullScanPolicy(JsonColdStoreScanPolicy.AllowExplicitScans));

        var extension = builder.Options.FindExtension<JsonColdStoreOptionsExtension>();

        Assert.NotNull(extension);
        Assert.Equal(JsonColdStoreCompression.Brotli, extension.Options.Compression);
        Assert.Equal("test-key", extension.Options.Encryption?.KeyId);
        Assert.True(extension.Options.Encryption?.RequireEncryptedStore);
        Assert.Equal(JsonColdStoreScanPolicy.AllowExplicitScans, extension.Options.FullScanPolicy);
    }

    [Fact]
    public void UseJsonColdStoreDatabaseRejectsUnsafeDatabaseDirectory()
    {
        var builder = new DbContextOptionsBuilder();

        Assert.Throws<ArgumentException>(() => builder.UseJsonColdStoreDatabase("   "));
    }

    [Fact]
    public void DebugInfoDoesNotExposeDirectoryOrKeyId()
    {
        using var key = JsonColdStoreEncryptionKey.FromBytes(new byte[32]);
        var directory = TestDirectory("sensitive-provider-options");
        var builder = new DbContextOptionsBuilder();
        builder.UseJsonColdStoreDatabase(
            directory,
            store => store.UseEncryption(new JsonColdStoreEncryptionOptions
            {
                Key = key,
                KeyId = "do-not-log",
            }));

        var extension = builder.Options.FindExtension<JsonColdStoreOptionsExtension>();
        var debugInfo = new Dictionary<string, string>();

        extension!.Info.PopulateDebugInfo(debugInfo);
        var debugText = string.Join(' ', debugInfo.Keys.Concat(debugInfo.Values));

        Assert.DoesNotContain(directory, debugText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("do-not-log", debugText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("True", debugInfo["JSONColdStore:Encrypted"]);
    }

    private static string TestDirectory(string name) =>
        Path.Combine(Path.GetTempPath(), "jsoncoldstore-tests", name);

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
