using System.Text;
using EFC.JSONColdStore;
using EFC.JSONColdStore.Storage;

namespace EFC.JSONColdStore.Tests;

public sealed class JsonColdStoreIndexStoreTests
{
    [Fact]
    public async Task ProtectedReadAcceptsPlaintextDocumentAndNextWriteProtectsIt()
    {
        var root = NewTempDirectory();
        using var key = JsonColdStoreEncryptionKey.FromBytes(new byte[32]);
        var options = new JsonColdStoreOptionsBuilder(root)
            .UseEncryptionKey(key)
            .UseFsyncOnWrite(false)
            .Build();
        var plaintextStore = new JsonColdStoreIndexStore(options, protectDocuments: false);
        await plaintextStore.ReplaceAsync(
            "Entity",
            "Value",
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["consumer-1"] = ["1"],
            });

        var indexPath = IndexPath(root, "Entity", "Value");
        Assert.Contains("consumer-1", await File.ReadAllTextAsync(indexPath));

        var protectedStore = new JsonColdStoreIndexStore(options, protectDocuments: true);
        var recordIds = await protectedStore.ReadRecordIdsAsync("Entity", "Value", "consumer-1");
        await protectedStore.UpsertAsync("Entity", "Value", "consumer-2", "2");
        var protectedBytes = await File.ReadAllBytesAsync(indexPath);
        var rewrittenRecordIds = await protectedStore.ReadRecordIdsAsync("Entity", "Value", "consumer-2");

        Assert.Equal(["1"], recordIds);
        Assert.True(JsonColdStorePayloadCodec.IsEnvelope(protectedBytes));
        Assert.DoesNotContain("consumer-1", Encoding.UTF8.GetString(protectedBytes));
        Assert.Equal(["2"], rewrittenRecordIds);
    }

    private static string IndexPath(string root, string entityName, string indexName) =>
        JsonColdStorePathValidator.GetSafeChildPath(
            root,
            "entities",
            JsonColdStoreNameEncoder.EncodePathSegment(entityName),
            "indexes",
            JsonColdStoreNameEncoder.EncodePathSegment(indexName) + ".json");

    private static string NewTempDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "jsoncoldstore-index-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
