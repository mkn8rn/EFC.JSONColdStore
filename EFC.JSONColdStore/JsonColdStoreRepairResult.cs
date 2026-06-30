namespace EFC.JSONColdStore;

/// <summary>
/// Result returned by a JSONColdStore repair pass.
/// </summary>
public sealed record JsonColdStoreRepairResult(
    int VerifiedRecords,
    int QuarantinedRecords);
