# Current cross-Tenant rejection evidence — Story 6.4 v4

This successor retains the v2 proof and the v3 successor and machine execution unchanged.
The current source revision is `0471c46e296658f2308175eac9e7c7a76596da9c`.

The public `hexalith-module prepare-build --mode source` command prepared the flat
workspace. The affected Server.Tests project built in Debug with zero warnings or
errors. A fresh native xUnit invocation of
`ConversationAsyncProjectionHandlerTest.CrossTenantEventShouldFailWithOnlyAnAcceptedRoutePositionBarrier`
passed exactly one test, with zero errors, failures, skips, or tests not run. Its XML
is retained byte for byte and bound alongside the exact current test source.

The test still requires Failed/HandlerFailure, exactly one persisted content-free
accepted-route position barrier with no current generation, and no Conversation view.
The total store count also rules out an extra tenant index or foreign view.

The JSON binds its unchanged v3 predecessor, historical v2 proof/run, current source,
and fresh execution. The validator recomputes these hashes. This isolated source/Debug
observation establishes no package parity, external owner qualification, production
readiness, or release authority. Package preparation still refuses the untagged
Platform identity with HXW011; that guard remains unchanged.
