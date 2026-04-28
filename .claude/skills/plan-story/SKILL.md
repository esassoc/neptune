---
name: plan-story
description: Fetch a Jira card by key (e.g. NPT-100) and produce a detailed implementation plan. Use this when starting work on a new story.
allowed-tools: [mcp__atlassian__getAccessibleAtlassianResources, mcp__atlassian__getJiraIssue, Read, Glob, Grep, Agent, AskUserQuestion, EnterPlanMode, ExitPlanMode]
user-invocable: true
---

Given a Jira issue key passed as an argument (e.g. `NPT-100`), fetch the story details and produce a full implementation plan.

## Steps

1. **Fetch the Jira issue.** Call `getAccessibleAtlassianResources` to obtain the cloud ID, then call `getJiraIssue` with the issue key provided as the argument. Extract the summary, description, status, and any linked issues.

2. **Summarize the requirements.** Parse the Jira description into a clear list of requirements, acceptance criteria, assumptions, and out-of-scope items. Present this summary to the user so they can confirm understanding before planning.

3. **Explore the codebase.** Based on the requirements, use the Explore agent (Task tool with subagent_type=Explore) to investigate:
   - Existing entities, DTOs, controllers, and EF business-logic classes related to the feature
   - Similar features already implemented (e.g. if the story is about OVTA, look at how Projects or TreatmentBMPs were built end-to-end)
   - Database tables, lookup tables, and seed data relevant to the domain
   - Frontend components, routes, and services for the related area
   - Any generated files that will need regeneration after backend changes

4. **Enter plan mode.** Call `EnterPlanMode` and write a detailed, step-by-step implementation plan covering:

   ### Plan structure
   - **Database layer** — new tables, columns, lookup tables, seed data, foreign keys, views (including `vGeoServer*` views if spatial)
   - **EF Models layer** — new entities (will be scaffolded), extension methods, business logic classes in `Neptune.EFModels/Entities/`, validation
   - **DTO layer** — new DTOs in `Neptune.Models/DataTransferObjects/`
   - **API layer** — new controllers, routes, authorization attributes (`[AdminFeature]`, `[JurisdictionEditFeature]`, `[UserViewFeature]`, `[SitkaAdminFeature]`), any new permissions/rights
   - **Tests** — integration/unit tests in `Neptune.Tests` (placed here so the user can run them while frontend work proceeds)
   - **Code generation** — run `/codegen` to regenerate TypeScript models/services
   - **Frontend layer** — new standalone components, lazy-loaded routes, services, ag-Grid grids, reactive forms, modals

   For each step, reference specific existing files as templates to follow (e.g. "Model after `TreatmentBMPController.cs`") and call out the exact file paths where new code should go.

5. **Present the plan** for user approval via `ExitPlanMode`.

## Notes

- The cloud ID for the Jira instance is obtained dynamically via `getAccessibleAtlassianResources`.
- Always follow existing patterns in the codebase — find the closest analogous feature and replicate its structure.
- The plan should be detailed enough that each step can be executed independently.
- Reference the CLAUDE.md architecture patterns (authorization attributes, jurisdiction-scoped routes, Observable + async pipe, standalone components, etc.).
- Do NOT start writing code — this skill only produces a plan.
