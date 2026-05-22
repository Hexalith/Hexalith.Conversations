// <copyright file="ClosedVocabularyJsonConverters.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Results;

namespace Hexalith.Conversations.Contracts.Serialization;

internal sealed class ConversationErrorCodeJsonConverter : ConversationStringValueJsonConverter<ConversationErrorCode>
{
    protected override ConversationErrorCode Create(string value) => ConversationErrorCode.Parse(value);

    protected override string GetValue(ConversationErrorCode value) => value.Value;
}

internal sealed class ConversationErrorCategoryJsonConverter : ConversationStringValueJsonConverter<ConversationErrorCategory>
{
    protected override ConversationErrorCategory Create(string value) => ConversationErrorCategory.Parse(value);

    protected override string GetValue(ConversationErrorCategory value) => value.Value;
}

internal sealed class ConversationCommandTypeJsonConverter : ConversationStringValueJsonConverter<ConversationCommandType>
{
    protected override ConversationCommandType Create(string value) => ConversationCommandType.Parse(value);

    protected override string GetValue(ConversationCommandType value) => value.Value;
}

internal sealed class ConversationEventTypeJsonConverter : ConversationStringValueJsonConverter<ConversationEventType>
{
    protected override ConversationEventType Create(string value) => ConversationEventType.Parse(value);

    protected override string GetValue(ConversationEventType value) => value.Value;
}

internal sealed class ConversationLifecycleStatusJsonConverter : ConversationStringValueJsonConverter<ConversationLifecycleStatus>
{
    protected override ConversationLifecycleStatus Create(string value) => ConversationLifecycleStatus.Parse(value);

    protected override string GetValue(ConversationLifecycleStatus value) => value.Value;
}

internal sealed class ParticipantTypeJsonConverter : ConversationStringValueJsonConverter<ParticipantType>
{
    protected override ParticipantType Create(string value) => ParticipantType.Parse(value);

    protected override string GetValue(ParticipantType value) => value.Value;
}

internal sealed class ParticipantRoleJsonConverter : ConversationStringValueJsonConverter<ParticipantRole>
{
    protected override ParticipantRole Create(string value) => ParticipantRole.Parse(value);

    protected override string GetValue(ParticipantRole value) => value.Value;
}
