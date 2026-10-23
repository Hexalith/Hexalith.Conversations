# Conversations 1.3.1 candidate notes

Advisory metadata queries accept the ordinary allowed Tenant Read decision when its requirement, Tenant and actor match the verified request. Native permission evidence remains required by the strict metadata query.

A Conversation with neither a stored label nor a scoring label now projects complete label absence, with a null safe label. A stored label without scoring classification remains incomplete. Existing rows receive this correction on reprojection or their next update; this release does not repair stored rows during reads.

These are local candidate notes. Publication and the Projects consumer pin require separate approval and verified release evidence.
