namespace EFC.JSONColdStore.Storage;

internal static class JsonColdStoreDirectoryGuard
{
    internal static string CreateDirectory(
        string databaseDirectory,
        params ReadOnlySpan<string> pathSegments)
    {
        var root = JsonColdStorePathValidator.NormalizeDatabaseDirectory(databaseDirectory);
        Directory.CreateDirectory(root);

        if (pathSegments.Length == 0)
            return root;

        var directorySegments = new List<string>(pathSegments.Length);
        foreach (var segment in pathSegments)
        {
            directorySegments.Add(segment);
            var directory = JsonColdStorePathValidator.GetSafeChildPath(
                root,
                [.. directorySegments]);

            if (Directory.Exists(directory))
            {
                ThrowIfReparsePoint(directory);
                continue;
            }

            Directory.CreateDirectory(directory);
            ThrowIfReparsePoint(directory);
        }

        return JsonColdStorePathValidator.GetSafeChildPath(root, pathSegments);
    }

    internal static void ThrowIfReparsePoint(string path)
    {
        if (JsonColdStoreDirectoryWalker.IsReparsePoint(path))
        {
            throw new JsonColdStoreUnsafePathException(
                "The JSONColdStore path cannot contain reparse-point child directories.");
        }
    }
}
