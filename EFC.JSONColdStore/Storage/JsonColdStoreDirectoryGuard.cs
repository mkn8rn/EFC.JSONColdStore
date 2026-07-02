namespace EFC.JSONColdStore.Storage;

internal static class JsonColdStoreDirectoryGuard
{
    internal static string CreateDirectory(
        string databaseDirectory,
        params ReadOnlySpan<string> pathSegments)
    {
        var root = CreateDatabaseRoot(databaseDirectory);

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

    internal static string CreateDatabaseRoot(string databaseDirectory)
    {
        var root = JsonColdStorePathValidator.NormalizeDatabaseDirectory(databaseDirectory);
        Directory.CreateDirectory(root);
        ThrowIfDatabaseRootIsReparsePoint(root);
        return root;
    }

    internal static bool ExistingDatabaseRootIsSafe(string databaseDirectory)
    {
        var root = JsonColdStorePathValidator.NormalizeDatabaseDirectory(databaseDirectory);
        return Directory.Exists(root) && !JsonColdStoreDirectoryWalker.IsReparsePoint(root);
    }

    internal static void ThrowIfExistingDatabaseRootIsReparsePoint(string databaseDirectory)
    {
        var root = JsonColdStorePathValidator.NormalizeDatabaseDirectory(databaseDirectory);
        if (!Directory.Exists(root))
            return;

        ThrowIfDatabaseRootIsReparsePoint(root);
    }

    internal static void ThrowIfReparsePoint(string path)
    {
        if (JsonColdStoreDirectoryWalker.IsReparsePoint(path))
        {
            throw new JsonColdStoreUnsafePathException(
                "The JSONColdStore path cannot contain reparse-point child directories.");
        }
    }

    private static void ThrowIfDatabaseRootIsReparsePoint(string root)
    {
        if (JsonColdStoreDirectoryWalker.IsReparsePoint(root))
        {
            throw new JsonColdStoreUnsafePathException(
                "The configured JSONColdStore database directory cannot be a reparse point.");
        }
    }
}
