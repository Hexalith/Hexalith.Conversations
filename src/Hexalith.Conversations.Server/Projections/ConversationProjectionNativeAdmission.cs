using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Freezes the first SDK admission decision separately from immutable producer input.</summary>
/// <param name="ProducerHint">The original supplied native cut, including absence.</param>
/// <param name="AdmittedProof">The first SDK-verified cut, including a lasting unproven decision.</param>
public sealed record ConversationProjectionNativeAdmission(NativeSourceProof? ProducerHint, NativeSourceProof? AdmittedProof);
