namespace Hexalith.Conversations.TestSupport;

/// <summary>The row order of a signable manifest payload.</summary>
public enum SignablePayloadOrder
{
    /// <summary>Canonical ordinal path order for new signed payloads.</summary>
    Canonical,
    /// <summary>Original declaration order, only for payloads signed before canonical ordering.</summary>
    Declared,
}
