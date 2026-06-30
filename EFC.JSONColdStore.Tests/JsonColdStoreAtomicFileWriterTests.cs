using EFC.JSONColdStore.Storage;

namespace EFC.JSONColdStore.Tests;

public sealed class JsonColdStoreAtomicFileWriterTests
{
    [Fact]
    public async Task WriteAsyncPublishesReadableBytes()
    {
        var root = NewTempDirectory();
        var payload = "stored bytes"u8.ToArray();

        await JsonColdStoreAtomicFileWriter.WriteAsync(
            root,
            ["Entity", "record.jcs"],
            payload,
            fsync: false);

        var read = await JsonColdStoreAtomicFileWriter.ReadAsync(
            root,
            ["Entity", "record.jcs"]);

        Assert.Equal(payload, read);
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "Entity"), "*.tmp-*"));
    }

    [Fact]
    public async Task WriteAsyncRejectsTraversalOutsideDatabaseDirectory()
    {
        var root = NewTempDirectory();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => JsonColdStoreAtomicFileWriter.WriteAsync(
                root,
                ["..", "escaped.jcs"],
                "nope"u8.ToArray(),
                fsync: false));
    }

    private static string NewTempDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "jsoncoldstore-writer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
