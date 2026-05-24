// <copyright file="InvestigationWorkspaceRenderer.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Text;

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Admin.Web.Rendering;

/// <summary>
/// Renders the first-party investigation workspace evidence surface.
/// </summary>
public sealed class InvestigationWorkspaceRenderer
{
    /// <summary>
    /// Renders the workspace as a standalone HTML document.
    /// </summary>
    /// <param name="workspace">The permission-safe workspace data.</param>
    /// <returns>The rendered HTML document.</returns>
    public string Render(InvestigationWorkspaceViewModel workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        StringBuilder html = new();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\" data-viewport=\"unknown\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\">");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        html.AppendLine("  <title>Conversations Investigation Workspace</title>");
        html.AppendLine("  <style>");
        html.AppendLine(Styles);
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<a class=\"skip-link\" href=\"#governed-record\" data-testid=\"skip-to-record\">Skip to governed record</a>");
        RenderBanner(html);
        RenderAnnouncer(html, workspace);
        html.AppendLine($"<main class=\"workspace\" id=\"governed-record\" tabindex=\"-1\" data-testid=\"workspace-root\" aria-labelledby=\"workspace-title\" data-fixture=\"{Attr(workspace.FixtureId)}\" data-current-viewport=\"unknown\" data-telemetry-label-base=\"{Attr(workspace.SafeTelemetryLabel)}\" data-telemetry-label=\"{Attr(workspace.SafeTelemetryLabel)}.unknown\">");
        RenderTrustStack(html, workspace);
        RenderDuplicateSurfaces(html, workspace);
        RenderTimeline(html, workspace);
        html.AppendLine("</main>");
        html.AppendLine("<script>");
        html.AppendLine(Script);
        html.AppendLine("</script>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    private static void RenderBanner(StringBuilder html)
    {
        // The document title heading lives in the banner landmark, giving the page a single
        // <h1> that the <main> region is named by. The search landmark anchors the Find step
        // of the Find -> Read -> Trust workflow.
        html.AppendLine("<header class=\"workspace-banner\" data-testid=\"workspace-banner\">");
        html.AppendLine("  <h1 id=\"workspace-title\">Conversations Investigation Workspace</h1>");
        html.AppendLine("  <div role=\"search\" aria-label=\"Find governed conversations\" data-testid=\"find-landmark\">");
        html.AppendLine("    <p class=\"banner-note\">Find, read, and trust governed evidence. Synthetic demo data only.</p>");
        html.AppendLine("  </div>");
        html.AppendLine("</header>");
    }

    private static void RenderAnnouncer(StringBuilder html, InvestigationWorkspaceViewModel workspace)
    {
        // A polite status region announces the safe trust class plus the safe evidence and
        // command posture. Every value is drawn from the already-authorized safe labels, so
        // the live region never carries protected detail and stays identical for hidden reads.
        string announcement =
            $"{workspace.SafeTrustPostureLabel}. {workspace.SafeEvidenceCompletenessLabel}. {workspace.SafeCommandEligibilityLabel}.";
        html.AppendLine($"<div class=\"visually-hidden\" role=\"status\" aria-live=\"polite\" aria-atomic=\"true\" data-testid=\"trust-announcer\">{Text(announcement)}</div>");
    }

    private static void RenderTrustStack(StringBuilder html, InvestigationWorkspaceViewModel workspace)
    {
        html.AppendLine("<section class=\"trust-stack\" aria-labelledby=\"trust-order-heading\" data-testid=\"trust-order\">");
        html.AppendLine("  <h2 id=\"trust-order-heading\" class=\"section-heading\">Trust order</h2>");
        RenderRankedPanel(html, "tenant-scope", 1, "Tenant Scope", workspace.SafeTenantScopeLabel, workspace);
        RenderRankedPanel(html, "record-identity", 2, "Record Identity", workspace.SafeRecordIdentityLabel, workspace);
        RenderRankedPanel(html, "trust-posture", 3, "Trust Posture", workspace.SafeTrustPostureLabel, workspace);
        RenderRankedPanel(html, "evidence-completeness", 4, "Evidence Completeness", workspace.SafeEvidenceCompletenessLabel, workspace);
        RenderCommandEligibility(html, workspace);
        html.AppendLine("</section>");
    }

    private static void RenderRankedPanel(
        StringBuilder html,
        string testId,
        int rank,
        string kicker,
        string text,
        InvestigationWorkspaceViewModel workspace)
    {
        string headingId = $"{testId}-heading";
        html.AppendLine($"  <section class=\"trust-panel\" data-testid=\"{testId}\" data-trust-rank=\"{rank}\" data-telemetry-label-base=\"{Attr(workspace.SafeTelemetryLabel)}.{testId}\" data-telemetry-label=\"{Attr(workspace.SafeTelemetryLabel)}.{testId}.unknown\">");
        html.AppendLine($"    <p class=\"panel-kicker\">{Text(kicker)}</p>");
        html.AppendLine($"    <h3 id=\"{headingId}\">{Text(text)}</h3>");
        html.AppendLine("  </section>");
    }

    private static void RenderCommandEligibility(StringBuilder html, InvestigationWorkspaceViewModel workspace)
    {
        html.AppendLine($"  <section class=\"trust-panel command-panel\" data-testid=\"command-eligibility\" data-trust-rank=\"5\" data-mobile-triage=\"read-only\" data-telemetry-label-base=\"{Attr(workspace.SafeTelemetryLabel)}.command-eligibility\" data-telemetry-label=\"{Attr(workspace.SafeTelemetryLabel)}.command-eligibility.unknown\">");
        html.AppendLine("    <p class=\"panel-kicker\">Command Eligibility</p>");
        html.AppendLine($"    <h3 id=\"command-eligibility-heading\">{Text(workspace.SafeCommandEligibilityLabel)}</h3>");
        html.AppendLine("    <ul class=\"command-list\" data-testid=\"command-list\">");
        int index = 0;
        foreach (ConversationCommandAvailabilityV1 command in workspace.CommandEligibility)
        {
            bool disabled = command.AvailabilityState != ProjectionTrustState.Current
                || command.ActionClassification == ConversationCommandAvailabilityV1.GovernanceChangingActionClassification;
            string disabledAttribute = disabled ? " disabled aria-disabled=\"true\"" : string.Empty;
            string state = disabled ? "Blocked" : "Available";
            string reasonId = $"command-reason-{index}";

            // The safe blocked reason is wired into the accessible description (aria-describedby)
            // and rendered as visible text, so keyboard and assistive-technology users receive the
            // same governed reason a sighted user sees — without revealing whether a protected
            // conversation, participant, provider, file, or event exists.
            html.AppendLine("      <li class=\"command-item\">");
            html.AppendLine($"        <button type=\"button\" data-testid=\"command-action\" data-action=\"{Attr(command.ActionName)}\" data-action-classification=\"{Attr(command.ActionClassification)}\" data-blocked-reason=\"{Attr(command.BlockedReason)}\" aria-describedby=\"{reasonId}\"{disabledAttribute}>{Text(state)}: {Text(command.ActionName)}</button>");
            html.AppendLine($"        <span class=\"command-reason\" id=\"{reasonId}\" data-testid=\"command-reason\">{Text(command.BlockedReason)}</span>");
            html.AppendLine("      </li>");
            index++;
        }

        html.AppendLine("    </ul>");
        html.AppendLine("  </section>");
    }

    private static void RenderDuplicateSurfaces(StringBuilder html, InvestigationWorkspaceViewModel workspace)
    {
        // These surfaces duplicate already-rendered trust information for responsive visual
        // layout only. They are hidden from the accessibility tree (aria-hidden) so screen-reader
        // users do not hear the same trust posture two or three times; the canonical accessible
        // content remains the trust panels and timeline above and below.
        html.AppendLine("<section class=\"responsive-duplicates\" aria-hidden=\"true\" data-testid=\"responsive-summaries\">");
        html.AppendLine($"  <aside class=\"duplicate-surface sticky-summary\" data-testid=\"sticky-summary\" data-responsive-duplicate=\"true\" data-telemetry-label-base=\"{Attr(workspace.SafeTelemetryLabel)}.sticky-summary\" data-telemetry-label=\"{Attr(workspace.SafeTelemetryLabel)}.sticky-summary.unknown\">");
        html.AppendLine("    <p class=\"panel-kicker\">Sticky Summary</p>");
        html.AppendLine($"    <p>{Text(workspace.SafeTrustPostureLabel)}</p>");
        html.AppendLine($"    <p>{Text(workspace.SafeEvidenceCompletenessLabel)}</p>");
        html.AppendLine("  </aside>");
        html.AppendLine($"  <aside class=\"duplicate-surface drawer-summary\" data-testid=\"authorized-drawer-summary\" data-responsive-duplicate=\"true\" data-telemetry-label-base=\"{Attr(workspace.SafeTelemetryLabel)}.drawer-summary\" data-telemetry-label=\"{Attr(workspace.SafeTelemetryLabel)}.drawer-summary.unknown\">");
        html.AppendLine("    <p class=\"panel-kicker\">Drawer Summary</p>");
        html.AppendLine($"    <p>{Text(workspace.SafeTenantScopeLabel)}</p>");
        html.AppendLine($"    <p>{Text(workspace.SafeRecordIdentityLabel)}</p>");
        html.AppendLine("  </aside>");
        html.AppendLine($"  <div class=\"duplicate-surface skeleton\" data-testid=\"safe-skeleton\" data-responsive-duplicate=\"true\" data-telemetry-label-base=\"{Attr(workspace.SafeTelemetryLabel)}.skeleton\" data-telemetry-label=\"{Attr(workspace.SafeTelemetryLabel)}.skeleton.unknown\"></div>");
        html.AppendLine("</section>");
    }

    private static void RenderTimeline(StringBuilder html, InvestigationWorkspaceViewModel workspace)
    {
        html.AppendLine($"<section class=\"timeline\" data-testid=\"timeline\" data-trust-rank=\"6\" data-telemetry-label-base=\"{Attr(workspace.SafeTelemetryLabel)}.timeline\" data-telemetry-label=\"{Attr(workspace.SafeTelemetryLabel)}.timeline.unknown\" aria-labelledby=\"timeline-heading\">");
        html.AppendLine("  <h2 id=\"timeline-heading\" class=\"section-heading\">Evidence timeline</h2>");
        if (workspace.IsHiddenRead)
        {
            // The denial message and its accessible name are identical for an unauthorized
            // existing record and a nonexistent one, so the accessibility tree cannot reveal
            // whether a governed record exists for this tenant scope.
            html.AppendLine("  <p class=\"timeline-row denied\" data-testid=\"timeline-row\" role=\"status\">No governed record is visible for this tenant scope.</p>");
        }
        else
        {
            int index = 0;
            foreach (ConversationEvidenceEntryV1 entry in workspace.EvidenceEntries)
            {
                string visible = entry.VisibleText ?? entry.SafeSummaryLabel ?? entry.Kind;
                string safeNextAction = entry.SafeNextAction ?? "Continue with governed evidence.";
                string headingId = $"evidence-{index}-heading";
                html.AppendLine($"  <article class=\"timeline-row\" data-testid=\"timeline-row\" data-evidence-kind=\"{Attr(entry.Kind)}\" data-trust-state=\"{Attr(entry.TrustState.Value)}\" data-citation-state=\"{Attr(entry.CitationAvailability.Value)}\" aria-labelledby=\"{headingId}\">");
                html.AppendLine($"    <h3 id=\"{headingId}\">{Text(entry.Kind)}</h3>");
                html.AppendLine($"    <p>{Text(visible)}</p>");
                html.AppendLine($"    <p class=\"safe-next-action\">{Text(safeNextAction)}</p>");
                html.AppendLine("  </article>");
                index++;
            }

            int messageIndex = 0;
            foreach (ConversationTimelineMessageProjectionV1 message in workspace.Detail?.Messages ?? [])
            {
                string headingId = $"message-{messageIndex}-heading";
                html.AppendLine($"  <article class=\"timeline-row message-row\" data-testid=\"timeline-message\" data-message-id=\"{Attr(message.MessageId.Value)}\" aria-labelledby=\"{headingId}\">");
                html.AppendLine($"    <h3 id=\"{headingId}\">Visible message</h3>");
                html.AppendLine($"    <p>{Text(message.Text)}</p>");
                html.AppendLine("  </article>");
                messageIndex++;
            }
        }

        html.AppendLine("</section>");
    }

    private static string Attr(string value) => WebUtility.HtmlEncode(value);

    private static string Text(string value) => WebUtility.HtmlEncode(value);

    private const string Styles = """
body {
    margin: 0;
    font-family: "Segoe UI", Arial, sans-serif;
    color: #171717;
    background: #f8fafc;
}

.skip-link {
    position: absolute;
    left: -9999px;
    top: 0;
    z-index: 20;
    background: #1d4ed8;
    color: #ffffff;
    padding: 8px 14px;
    border-radius: 0 0 6px 0;
    text-decoration: none;
}

.skip-link:focus {
    left: 0;
}

.visually-hidden {
    position: absolute;
    width: 1px;
    height: 1px;
    margin: -1px;
    padding: 0;
    border: 0;
    overflow: hidden;
    clip: rect(0 0 0 0);
    clip-path: inset(50%);
    white-space: nowrap;
}

.workspace-banner {
    max-width: 1180px;
    margin: 0 auto;
    padding: 16px 16px 0 16px;
    box-sizing: border-box;
}

.workspace-banner h1 {
    margin: 0 0 4px 0;
    font-size: 1.3rem;
    line-height: 1.3;
}

.banner-note {
    margin: 0;
    font-size: 0.85rem;
    color: #475569;
}

.workspace {
    min-height: 100vh;
    padding: 16px;
    box-sizing: border-box;
}

.trust-stack,
.responsive-duplicates,
.timeline {
    max-width: 1180px;
    margin: 0 auto 14px auto;
}

.trust-stack {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 10px;
}

.section-heading {
    grid-column: 1 / -1;
    margin: 0 0 2px 0;
    font-size: 0.95rem;
}

.timeline .section-heading {
    margin-bottom: 8px;
}

.trust-panel,
.duplicate-surface,
.timeline-row {
    background: #ffffff;
    border: 1px solid #cbd5e1;
    border-radius: 6px;
    padding: 12px;
    box-shadow: 0 1px 2px rgba(15, 23, 42, 0.05);
}

.trust-panel h3,
.timeline-row h3 {
    margin: 4px 0 0 0;
    font-size: 1rem;
    line-height: 1.35;
    font-weight: 650;
}

.panel-kicker {
    margin: 0;
    font-size: 0.74rem;
    text-transform: uppercase;
    color: #475569;
}

.command-list {
    list-style: none;
    margin: 10px 0 0 0;
    padding: 0;
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
}

.command-item {
    display: flex;
    flex-direction: column;
    gap: 2px;
    max-width: 320px;
}

.command-reason {
    font-size: 0.78rem;
    color: #475569;
    line-height: 1.35;
}

button {
    border: 1px solid #64748b;
    border-radius: 5px;
    background: #ffffff;
    color: #0f172a;
    min-height: 36px;
    padding: 6px 10px;
    font: inherit;
}

button:disabled {
    color: #475569;
    border-color: #94a3b8;
    background: #f1f5f9;
}

a:focus-visible,
button:focus-visible,
[tabindex]:focus-visible {
    outline: 3px solid #1d4ed8;
    outline-offset: 2px;
}

.responsive-duplicates {
    display: grid;
    grid-template-columns: 1fr 1fr 90px;
    gap: 10px;
}

.skeleton {
    min-height: 64px;
    background: repeating-linear-gradient(90deg, #f1f5f9, #f1f5f9 10px, #e2e8f0 10px, #e2e8f0 20px);
}

.timeline {
    display: grid;
    gap: 10px;
}

.timeline-row p {
    margin: 6px 0 0 0;
    line-height: 1.45;
}

.safe-next-action {
    color: #475569;
}

@media (max-width: 800px) {
    .trust-stack,
    .responsive-duplicates {
        grid-template-columns: 1fr;
    }

    .workspace {
        padding: 12px;
    }

    .trust-panel h3,
    .timeline-row h3 {
        font-size: 0.96rem;
    }
}

@media (forced-colors: active) {
    .trust-panel,
    .duplicate-surface,
    .timeline-row,
    button {
        border: 1px solid CanvasText;
    }

    a:focus-visible,
    button:focus-visible,
    [tabindex]:focus-visible {
        outline: 3px solid Highlight;
    }
}

@media (prefers-reduced-motion: reduce) {
    * {
        scroll-behavior: auto;
    }
}
""";

    private const string Script = """
(function () {
    function classify() {
        var width = window.innerWidth || document.documentElement.clientWidth;
        if (width <= 480) { return "mobile"; }
        if (width <= 900) { return "tablet"; }
        if (width >= 1400) { return "wide-desktop"; }
        return "desktop";
    }

    var viewport = classify();
    document.documentElement.setAttribute("data-viewport", viewport);
    var root = document.querySelector("[data-testid='workspace-root']");
    if (root) {
        root.setAttribute("data-current-viewport", viewport);
    }

    document.querySelectorAll("[data-telemetry-label-base]").forEach(function (node) {
        var base = node.getAttribute("data-telemetry-label-base");
        node.setAttribute("data-telemetry-label", base + "." + viewport);
    });
}());
""";
}
