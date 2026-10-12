namespace Hexalith.Conversations.TestSupport.Tests;

internal sealed record EvidenceMutationRow(string Id, string ExpectedBlocker, string[] ObservedBlockers,
    string BeforeSha256, string AfterSha256, bool Detected, bool Restored);
