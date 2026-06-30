namespace EFC.JSONColdStore;

/// <summary>
/// Controls behavior when a query cannot use a declared index.
/// </summary>
public enum JsonColdStoreScanPolicy
{
    /// <summary>Fail unsupported full scans unless the query explicitly opts in.</summary>
    FailUnlessExplicit = 0,

    /// <summary>Allow full scans only when provider APIs mark the scan explicitly.</summary>
    AllowExplicitScans = 1,

    /// <summary>Allow scans without a separate opt-in. Useful for small stores only.</summary>
    AllowSilentScans = 2,
}
