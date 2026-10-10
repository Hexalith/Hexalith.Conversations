namespace Hexalith.Conversations.TestSupport;

/// <summary>A raw Git diff entry with both file modes and object identifiers.</summary>
/// <param name="SourceMode">Source tree mode.</param>
/// <param name="DestinationMode">Destination tree mode.</param>
/// <param name="SourceObject">Source object identifier.</param>
/// <param name="DestinationObject">Destination object identifier.</param>
/// <param name="Status">Raw status including optional similarity score.</param>
/// <param name="Path">Destination path, decoded strictly as UTF-8.</param>
/// <param name="SourcePath">Original path for a rename or copy.</param>
public sealed record GitDiffEntry(string SourceMode, string DestinationMode, string SourceObject, string DestinationObject, string Status, string Path, string? SourcePath);
