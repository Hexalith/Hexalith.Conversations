# Current cross-Tenant rejection evidence — Story 6.4

The retained v2 proof and machine results describe their historical source and remain byte-identical. Their `CrossTenantEventShouldFailWithoutWritingEitherTenantScope` test proved zero writes at that historical revision. It is not a passing assertion at current HEAD.

The actual successor is `ConversationAsyncProjectionHandlerTest.CrossTenantEventShouldFailWithOnlyAnAcceptedRoutePositionBarrier`. Its fresh bound execution passes and verifies a Failed/HandlerFailure rejection, exactly one persisted content-free accepted-route position barrier with no current generation, and no Conversation view. Store count one plus the required proof key prevents an unobserved tenant index or other Conversation view from being persisted.

The JSON successor binds the historical proof/run hashes, current source hash and exact current passing xUnit result. The live validator preserves all historical outcome assertions and validates this explicit successor for the renamed case; it does not add an alias or exclude the guard. Production readiness and release authority remain unestablished.
