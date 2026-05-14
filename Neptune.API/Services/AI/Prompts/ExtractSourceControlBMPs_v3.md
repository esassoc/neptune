{{EvidenceInstructions}}

<task>
Extract all **Source Control BMPs** from the uploaded Water Quality Management Plan PDF (Orange County, CA).

Source Control BMPs in OC WQMPs fall into three categories. Look for evidence of ALL of them:

1. **Hydrologic Source Control and Site Design BMPs** — Low-Impact Development (LID) features that reduce runoff at the source. Examples: Impervious Area Dispersion, Localized On-lot Infiltration, Street Trees, Residential Rain Barrels, Harvest and Use Systems.

2. **Applicable Routine Non-Structural Source Control BMPs** — operational / programmatic practices the property's owner/operator commits to perform. Examples: storm-drain stenciling, employee training, trash management procedures, spill response plans, sweeping schedules.

3. **Applicable Routine Structural Source Control BMPs** — fixed structures that prevent pollutants from entering the storm-drain system. Examples: trash enclosures with roofs, fueling area canopies, designated vehicle wash areas, dock-loading containment.

WQMPs typically present Source Control BMPs as a **checklist table** where each attribute is listed alongside a "Yes/No" / "Applicable / Not Applicable" / checked / unchecked indicator and an optional note. Extract every row of every such checklist you find — both items marked YES and items explicitly marked NO. Treat each row as a separate `SourceControlBMPAttribute`.

When you cannot find an attribute mentioned at all in the document, do **not** emit a row for it. Only emit attributes the document explicitly addresses (either as present, absent, or with a note).
</task>

For each emitted item:

- `SourceControlBMPAttribute.Value` — the attribute name. Match to a name from DomainContext's `SourceControlBMPAttributes` (grouped by category) when a close match exists; otherwise emit the document's wording verbatim.
- `IsPresent.Value` — `"Yes"` if the document indicates the attribute is implemented / applicable / checked. `"No"` if the document explicitly indicates the attribute is NOT implemented / NOT applicable / unchecked. `null` only if the document mentions the attribute but is genuinely silent on its presence.
- `SourceControlBMPNote.Value` — any explanatory note or condition the document associates with the attribute. `null` if there is no note.

When a field's Value is null, also set its `ExtractionEvidence`, `DocumentSource`, and `BoundingBox` to null.

The schema for each emitted item:
{{Schema}}

Return a JSON object containing an `"items"` array (empty only if no Source Control BMP checklist or discussion is present in this document).
