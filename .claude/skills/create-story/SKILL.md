---
name: create-story
description: Create or flesh out a Jira story with structured sections for POs, Devs, and Claude. Accepts an optional Jira key to update an existing card.
allowed-tools: [mcp__atlassian__getAccessibleAtlassianResources, mcp__atlassian__getJiraIssue, mcp__atlassian__createJiraIssue, mcp__atlassian__editJiraIssue, mcp__atlassian__getJiraIssueTypeMetaWithFields, mcp__atlassian__getJiraProjectIssueTypesMetadata, mcp__atlassian__searchJiraIssuesUsingJql, AskUserQuestion, Read, Glob, Grep]
---

Create a well-structured Jira story card that serves three audiences: **Product Owners** (clear requirements and business value), **Developers** (technical context and unambiguous scope), and **Claude** (structured ACs that map cleanly to implementation plans and e2e tests via `/plan-story`).

## Arguments

- **Optional**: A Jira issue key (e.g. `NPT-100`) to fetch and flesh out an existing card, **or** a free-text description of the feature to create a new card from scratch.
- **Optional**: `--dry-run` — draft the card without pushing to Jira. The final output is displayed for review only.

## Workflow

### Phase 1: Gather Context

**If a Jira key was provided:**
1. Call `getAccessibleAtlassianResources` to get the cloud ID.
2. Fetch the issue with `getJiraIssue` (use `responseContentFormat: "markdown"`).
3. Present the user with a summary of what's already on the card.
4. Identify gaps (missing ACs, vague requirements, no parent epic, etc.) and call them out.
5. Proceed to Phase 2 to fill the gaps.

**If a free-text description was provided:**
1. Use it as the initial context and proceed to Phase 2.

**If nothing was provided:**
1. Ask the user for a brief description of the feature or change they want to build.
2. Wait for their response before proceeding.

### Phase 2: Interactive Questionnaire

After you have the initial context (either from an existing card, the user's description, or their reply), use `AskUserQuestion` to fill remaining gaps **one question at a time**. This keeps the conversation focused and lets each answer inform the next question.

**Topics to cover** (skip any already answered by prior context):

- **User story**: Who is this for and what problem does it solve?
- **Parent Epic**: Which epic should this be billed under? If unsure, fetch active epics via JQL (`project = NPT AND issuetype = Epic AND status != Done ORDER BY updated DESC`) and present them as `AskUserQuestion` suggestions so the user can pick.
- **Scope**: What specific behaviors or screens are involved? What is explicitly out of scope?
- **Acceptance criteria**: What are the testable conditions for "done"? Walk through user flows and edge cases.
- **Data model**: Are there new entities, fields, lookup tables, or relationships?
- **Permissions/roles**: Does this feature have role-based access differences?
- **UI/UX**: Any wireframes, Figma links, or specific UI patterns to follow? Which existing page/component is this most similar to?
- **Dependencies**: Does this depend on or block other work?

**How to ask:**
- Use `AskUserQuestion` to ask up to 4 related questions at a time. Batch questions that are thematically related (e.g. scope + out-of-scope, or data model + permissions). Use the `suggestions` parameter to offer likely answers when you can infer them from context (e.g. epic names, role names, existing pages).
- After each round of answers, evaluate whether you have enough context to draft the card. Stop asking as soon as you do — don't interrogate the user through every topic if the picture is already clear.
- Let each round of answers shape the next questions. If the user describes a new grid page, follow up about column definitions and filters — don't mechanically march through the list above.

### Phase 3: Draft the Card

Once you have enough context, draft the Jira card with these sections in the **description** field:

```markdown
## Context
[1-2 sentences: why this work matters, who it's for]

## Requirements
[Numbered list of specific, unambiguous requirements]

## Open Questions
[Only if there are genuine unresolved questions -- omit this section if there are none]

## Technical Notes
[Only if the conversation surfaced specific technical considerations worth capturing -- omit if there are none]
```

For the **Acceptance Criteria** field (separate from description), write structured, numbered criteria organized into logical sections. Each AC should be:
- **Testable**: A developer or QA person can verify it passes/fails
- **Specific**: References concrete roles, pages, actions, and expected outcomes
- **Grouped**: Under thematic headings (e.g. "### Permissions", "### UI Behavior", "### Data Validation")

Each AC section should include a **PO Testing** checklist right after the heading, before the numbered criteria. These are spot-check items a product owner can walk through to verify that section — not exhaustive test plans, just the key things to click on, look at, and compare against source data.

Use this format:
```markdown
### [Section Name]

**PO Testing:**
- [ ] [Spot-check item a PO can verify for this section]
- [ ] [Spot-check item a PO can verify for this section]

1. [Specific testable criterion]
2. [Specific testable criterion]

### [Section Name]

**PO Testing:**
- [ ] [Spot-check item a PO can verify for this section]

3. [Specific testable criterion]
4. [Specific testable criterion]
```

Number the ACs sequentially across sections so they can be referenced as "AC 1", "AC 2", etc. in implementation plans and e2e tests.

### Phase 4: Review and Create/Update

1. Present the full draft to the user for review (show both description and ACs clearly).
2. Incorporate any feedback.
3. **If `--dry-run` was specified**, stop here. The draft is the final output — do not create or update any Jira card.
4. **Otherwise, create or update the Jira card:**
   - Call `getAccessibleAtlassianResources` to get the cloud ID (if not already obtained).
   - Discover custom fields: call `getJiraIssueTypeMetaWithFields` for the Story issue type in the target project. Find the field keys for "Acceptance criteria" (or similar) and "Sprint".
   - Discover the active sprint: use `searchJiraIssuesUsingJql` with JQL `project = NPT AND sprint in openSprints() ORDER BY created DESC` (limit 1) to find an issue in the current sprint. Extract the sprint ID from that issue's sprint field. If no open sprint exists, skip sprint assignment and let the user know.
   - **Custom rich-text fields require ADF, not markdown.** `contentFormat: "markdown"` applies only to the main `description` argument; the Acceptance Criteria field (and any other custom rich-text field) must be passed as an Atlassian Document Format JSON object inside `additional_fields`, or the call fails with `Operation value must be an Atlassian Document`. Build the AC as `{"version": 1, "type": "doc", "content": [...]}` using node types like `heading`, `paragraph`, `orderedList`/`listItem`, `taskList`/`taskItem`, and text `marks` (e.g., `strong`, `code`). Use `attrs.order` on each `orderedList` to continue AC numbering across sections.
   - **New card**: Use `createJiraIssue` with `projectKey: "NPT"`, `issueTypeName: "Story"`, `contentFormat: "markdown"`, the description, and set the parent epic, AC field, and sprint ID via `additional_fields`.
   - **Existing card**: Use `editJiraIssue` with `contentFormat: "markdown"` to update the description, AC fields, and sprint.
5. Return the issue key and link to the user.

## Guidelines

- **Don't pad sections**: If there are no open questions, omit the Open Questions section. Same for Technical Notes.
- **Reference existing patterns**: When the feature is similar to something already built, mention it (e.g. "Similar to how [existing feature] works on [existing page]").
- **Tables for structured data**: Use markdown tables when describing permission matrices, field mappings, or status transitions -- they're clearer than prose.
- **Keep ACs atomic**: Each AC should test one thing. Don't combine multiple behaviors into a single criterion.
- **Think about `/plan-story`**: Write ACs that map cleanly to implementation steps and e2e test cases. A dev running `/plan-story` on this card should be able to build a plan without ambiguity.
