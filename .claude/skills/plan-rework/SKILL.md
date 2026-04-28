---
name: plan-rework
description: Extract rework items from a Jira card's Acceptance Criteria and produce a fix plan. Auto-detects issue key from branch or accepts one as argument.
allowed-tools: [mcp__atlassian__getAccessibleAtlassianResources, mcp__atlassian__searchJiraIssuesUsingJql, mcp__atlassian__getJiraIssue, Bash(git branch*), Bash(git rev-parse*), Read, Glob, Grep, Agent, AskUserQuestion, EnterPlanMode, ExitPlanMode]
user-invocable: true
---

Given a Jira issue key (e.g. `NPT-1015`) — either passed as an argument or auto-detected from the current git branch — fetch the issue's Acceptance Criteria, identify rework items, and produce a detailed fix plan.

## Steps

1. **Determine the Jira issue key.**
   - If an argument was provided (e.g. `/plan-rework NPT-1015`), use it directly.
   - Otherwise, parse the current git branch name using the regex `/features\/(NPT-\d+)/` (e.g. `features/NPT-1015-2` -> `NPT-1015`).
   - If neither works, use `AskUserQuestion` to ask the user for the key.

2. **Fetch the Jira issue in ADF format.**
   - Call `getAccessibleAtlassianResources` to obtain the cloud ID.
   - Call `searchJiraIssuesUsingJql` with:
     - `jql`: `"key = <ISSUE_KEY>"`
     - `fields`: `["customfield_10034", "summary", "status", "description"]`
     - `responseContentFormat`: `"adf"`
   - ADF format is **required** because Markdown strips the status lozenge colors, making it impossible to distinguish pass from fail.

3. **Parse Acceptance Criteria (`customfield_10034`) for rework items.**
   Walk the ADF document tree and find all `status` nodes. Each status node has `attrs.color`:
   - **`"red"`** = needs rework (these are the items we care about)
   - **`"green"`** = passed
   - **`"yellow"`** = optional or to-test

   For each **red** status node, capture:
   - The surrounding text context (the acceptance criteria item it belongs to)
   - The status text (e.g. "KE 3/25/26 needs work")
   - Any tester notes that follow the status lozenge in the same paragraph or list item

   Also scan the `description` field for any additional rework context or notes.

   Skip `customfield_10035` (secondary notes field) — it is rarely useful.

4. **Group and present rework items.**
   - Group items by category/section (derived from bold headings or top-level structure in the ADF).
   - Present all rework items with their context and tester notes.
   - Separately note any green (passed) and yellow (optional/to-test) items for awareness.
   - Ask the user to confirm the rework items before proceeding.

5. **Ask clarifying questions.**
   Use `AskUserQuestion` for any ambiguous rework items where the tester's intent is unclear or where the fix approach isn't obvious from the description alone.

6. **Explore the codebase.**
   Launch Explore agents (via the Agent tool with `subagent_type: "Explore"`) to investigate the code areas affected by each rework item. Look at:
   - The components, services, and templates related to each rework item
   - Similar patterns elsewhere in the codebase that show the correct approach
   - Any relevant backend endpoints, DTOs, or database objects
   - **Existing tests** — find tests in `Neptune.Tests/` that cover the affected code so we know what needs updating
   - **Generated files** — identify if any generated code (DTOs in `Neptune.Models/DataTransferObjects/Generated/`, TypeScript client in `Neptune.Web/src/app/shared/generated/`) will need regeneration
   - **Database layer** — check if rework touches tables, views, lookup data, or migration scripts in `Neptune.Database/`

7. **Enter plan mode.**
   Call `EnterPlanMode` and write a detailed fix plan covering each rework item:
   - Reference the specific rework feedback from the tester
   - Identify the exact files and lines that need changes
   - Describe the fix approach, referencing specific existing files as templates to follow where helpful
   - Each step should be detailed enough to execute independently
   - If a fix spans multiple layers, order changes backend-first (database -> EF/DTO -> API -> codegen -> frontend)
   - Note any existing tests that need updating and whether `/codegen` is needed

8. **Present the plan** for user approval via `ExitPlanMode`.

## Notes

- The cloud ID for the Jira instance is obtained dynamically via `getAccessibleAtlassianResources`.
- ADF (Atlassian Document Format) is a JSON tree structure. Status lozenges appear as nodes with `type: "status"` and `attrs: { text: "...", color: "red"|"green"|"yellow"|"neutral" }`.
- Field mapping: `customfield_10034` = Acceptance Criteria, `customfield_10035` = secondary notes (skipped).
- This skill only produces a plan — do NOT start writing code.
