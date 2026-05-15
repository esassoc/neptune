{{EvidenceInstructions}}

<task>
Extract all **Source Control BMPs** that this Water Quality Management Plan (WQMP) describes — operational practices, site-design features, and structural elements that prevent pollutants from entering the storm-drain system. This is **not** the global Source Control inventory; scope each extraction to the practices this document commits to for this specific Orange County, CA site.
</task>

<where_to_look>
OC WQMPs present Source Control BMPs in one or more of these layouts. Read all of them — they often appear in the same document.

**Pre-printed checklist sections** — typically **Section IV.3.6** in the modern (2013+) OC Model WQMP template. Recognize by structure: three sub-tables (one per category) where each row is a fixed attribute name with an "Included? Yes / No / N/A" indicator and a "Conditions if Yes" note column. A marked Yes or No is positive evidence — extract every marked row from every category sub-table. Skip rows marked N/A or left blank.

The checklist covers three categories, in standard order:

- **Hydrologic Source Control and Site Design BMPs** — LID measures that reduce runoff at the source. Examples: Impervious Area Dispersion, Localized On-lot Infiltration, Street Trees, Harvest and Use Systems, Conserved Natural Areas, Buffer Zones for Natural Water Bodies, Minimized Impervious Areas, Maintained or Restored Natural Drainage Patterns, Distributed Permeable Pavement in Low Traffic Areas, Green Roof / Brown Roof, Absorbent Landscaping with Drought Tolerant Species.
- **Applicable Routine Non-Structural Source Control BMPs** — operational / programmatic practices the owner/operator commits to. Examples: Education for Property Owners/Tenants/Occupants, Activity Restrictions, Landscape Management, BMP Maintenance, Title 22 CCR Compliance, Local Water Quality Permit Compliance, Spill Contingency Plan, Underground Storage Tank Compliance, Hazardous Materials Disclosure Compliance, Uniform Fire Code Implementation, Litter Control, Employee Training, Housekeeping of Loading Docks, Catch Basin Inspection, Street Sweeping Private Streets and Parking Lots, Retail Gasoline Outlets.
- **Applicable Routine Structural Source Control BMPs** — fixed structures. Examples: Provide Storm Drain System Stenciling and Signage, Design Outdoor Hazardous Material Storage Areas, Design Trash Enclosures, Use Efficient Irrigation Systems and Landscape Design, Protect Slopes and Channels, Maintenance Bays, Vehicle Wash Areas, Outdoor Processing Areas, Equipment Wash Areas, Loading Dock Areas, Fueling Areas, Wash Water Controls for Food Preparation Areas, Community Car Wash Racks.

**Older OC BMP Manual templates** (2003-era South-OC and similar) — Source Controls described by code letters in narrative form. Common code schemes across older templates:

- **South-OC 2003 template** uses a flat **S-code** numbering (S1 through ~S20) that mixes Source Controls and Treatment Controls in the same list. **Low-numbered S-codes (roughly S1 – S14) are Source Controls** (Site Design + Structural Source Controls); high-numbered S-codes (≥ S15) are Treatment Controls and are NOT your responsibility — the QuickBMPs extraction handles those.

  Known South-OC 2003 S-code mappings to DomainContext attributes:

  - **S3. Common Area Runoff-Minimizing Landscape Design** → *Maintained or Restored Natural Drainage Patterns* (Site Design)
  - **S6. Trash Container Areas** → *Design Trash Enclosures to Reduce Pollutant Introduction* (Structural)
  - **S9. Concrete Fuel Dispensing Area** / **S10. Motor Fuel Dispensing Area Canopy** → *Fueling Areas* (Structural) — both codes describe parts of the same fueling-area BMP; emit once.
  - **S11. Outdoor Material Storage Area** → *Design Outdoor Hazardous Material Storage Areas to Reduce Pollutant Introduction* (Structural)
  - **S12. Vehicle Wash Area** → *Vehicle Wash Areas* (Structural)
  - Any S-code below S15 with "landscape", "irrigation", "drainage", "trash", "fuel", "vehicle", "material storage", "hazardous", or "wash" in its title is almost certainly a Source Control and you should emit it.

- **Other older templates** sometimes use letter-prefixed codes: **N1 – N16** for Routine Non-Structural Source Controls (e.g., "N1. Education for Property Owners and Tenants"; "N7. Catch Basin Inspection"); **SD-1 to SD-32** for Site Design BMPs (e.g., "SD-12. Efficient Irrigation"); **SC-10 to SC-44** for Structural Source Controls (e.g., "SC-15. Outdoor Material Storage Areas"; "SC-30. Fueling Areas"; "SC-32. Trash Storage Areas").

When a code appears as a section heading with an implementation description (even one sentence), treat it as committed and extract `IsPresent: "Yes"`. Put the code in the `SourceControlBMPNote` for traceability if helpful, but map `SourceControlBMPAttribute.Value` to the matching DomainContext name — not to the code.

**Narrative O&M / BMP sections** — some WQMPs lack any structured checklist or code list and only describe Source Controls inside an Operation & Maintenance, Best Management Practices, or Pollution Prevention narrative. Pull each practice the document describes as implemented on this site. Common narrative phrasings and the DomainContext attribute they map to:

- "storm drain stenciling", "no-dumping markers", "drain marking" → **Provide Storm Drain System Stenciling and Signage**
- "trash enclosure", "trash storage area", "roofed dumpster" → **Design Trash Enclosures to Reduce Pollutant Introduction**
- "fueling area canopy", "fuel dispenser island", "spill containment at fuel pumps" → **Fueling Areas**
- "vehicle wash area", "car wash bay" → **Vehicle Wash Areas**
- "equipment wash area" → **Equipment Wash Areas**
- "maintenance bay", "service bay containment" → **Maintenance Bays**
- "loading dock containment" → **Loading Dock Areas**
- "hazardous material storage", "chemical storage area" → **Design Outdoor Hazardous Material Storage Areas to Reduce Pollutant Introduction**
- "underground storage tank", "UST" → **Underground Storage Tank Compliance**
- "spill plan", "spill response", "spill kit" → **Spill Contingency Plan**
- "employee training", "operator training" → **Employee Training**
- "tenant education", "property owner pamphlet" → **Education for Property Owners, Tenants and Occupants**
- "catch basin inspection", "drainage facility inspection" → **Catch Basin Inspection**
- "street sweeping", "parking lot sweeping" → **Street Sweeping Private Streets and Parking Lots**
- "efficient irrigation", "drip irrigation", "smart controller" → **Use Efficient Irrigation Systems and Landscape Design**
- "retail gasoline outlet practices", "Chevron / Shell / 76 / Mobil service station SOPs" → **Retail Gasoline Outlets**
- "hazardous materials business plan", "HMDC" → **Hazardous Materials Disclosure Compliance**
- "Uniform Fire Code compliance", "UFC" → **Uniform Fire Code Implementation**
- "Title 22 hazardous waste compliance" → **Title 22 CCR Compliance**
- "drought-tolerant landscaping", "absorbent soil amendments" → **Absorbent Landscaping with Drought Tolerant Species**

For each extracted item, set `SourceControlBMPAttribute.Value` to the matching DomainContext attribute name (see DomainContext's grouped `SourceControlBMPAttributes`) when a close conceptual match exists. If no match, emit the document's wording verbatim — staff will remap during review.
</where_to_look>

<positive_selection_rule>
Emit a Source Control BMP when at least one positive signal is present:

- A checked / Yes-marked row in a Section IV.3.6 checklist.
- An explicitly No-marked row (extract as `IsPresent: "No"`).
- A BMP code (N1, SD-x, SC-x) appearing as a section heading or list entry with an implementation description.
- A narrative paragraph in an O&M / BMP / Pollution Prevention section describing implementation of a practice that maps to a DomainContext attribute.

Do **not** emit when:

- The row is marked N/A or left blank.
- The document references a practice only as "could apply" without committing to it.
- The mention appears only in a generic glossary or appendix without project-specific commitment.

**Be permissive on narrative-style matches.** A missed Source Control is worse than a mismapped one — staff reviews and corrects every extraction. If a document describes a practice that fits a DomainContext attribute even loosely, extract it.
</positive_selection_rule>

<example>
Modern checklist row (Section IV.3.6.a): "Localized On-lot Infiltration … [✓] Yes  [ ] No  [ ] N/A     Conditions if Yes: Bioretention area at SW corner". Emit one item with `SourceControlBMPAttribute.Value = "Localized On-lot Infiltration"`, `IsPresent.Value = "Yes"`, `SourceControlBMPNote.Value = "Bioretention area at SW corner"`.
</example>

<example>
Modern checklist row marked No: "Street Trees … [ ] Yes  [✓] No  [ ] N/A". Emit one item with `SourceControlBMPAttribute.Value = "Street Trees"`, `IsPresent.Value = "No"`, `SourceControlBMPNote.Value = null`.
</example>

<example>
Older BMP-code style: "N1. Education for Property Owners and Tenants. Tenants will receive a stormwater pollution prevention pamphlet at lease signing." Emit one item with `SourceControlBMPAttribute.Value = "Education for Property Owners, Tenants and Occupants"`, `IsPresent.Value = "Yes"`, `SourceControlBMPNote.Value = "Pamphlet at lease signing"`.
</example>

<example>
Older BMP-code style: "SC-32. Trash Storage Areas. Trash enclosure to be roofed with side walls on three sides." Emit one item with `SourceControlBMPAttribute.Value = "Design Trash Enclosures to Reduce Pollutant Introduction"`, `IsPresent.Value = "Yes"`, `SourceControlBMPNote.Value = "Roofed with side walls on three sides"`.
</example>

<example>
Narrative O&M paragraph: "Storm drain inlets on the property are stenciled with 'No Dumping — Flows to Ocean'." Emit one item with `SourceControlBMPAttribute.Value = "Provide Storm Drain System Stenciling and Signage"`, `IsPresent.Value = "Yes"`, `SourceControlBMPNote.Value = "'No Dumping - Flows to Ocean' stencils"`. (Likewise: "Catch basins inspected monthly during the rainy season" → `Catch Basin Inspection`; "Spill kits at fueling islands" → `Spill Contingency Plan`; "Underground storage tanks inspected per CUPA" → `Underground Storage Tank Compliance`.)
</example>

<output_fields>
For each Source Control BMP found, emit one object containing the following ExtractedValue fields:

| Field | Guidance |
|---|---|
| `SourceControlBMPAttribute` | The attribute name. Prefer a name from DomainContext's grouped `SourceControlBMPAttributes` when a close conceptual match exists; otherwise emit the document's wording verbatim. |
| `IsPresent` | `"Yes"` if the document indicates the attribute is implemented / applicable / checked / committed-to. `"No"` if the document explicitly indicates the attribute is NOT implemented / NOT applicable / unchecked. `null` only when the document mentions the attribute but is genuinely silent on its presence. |
| `SourceControlBMPNote` | Free-form note (≤200 chars) — the document's "Conditions if Yes" entry, a brief description, sizing detail, or any qualifier. `null` if there is no note. |
</output_fields>

<edge_cases>
- The same attribute mentioned in multiple sections → emit once.
- BMP codes like "N7" or "SC-32" should NOT appear in `SourceControlBMPAttribute.Value` — map them to the DomainContext attribute name. Codes can appear in the note if helpful.
- When a field's `Value` is null, also set `ExtractionEvidence`, `DocumentSource`, and `BoundingBox` to null.
- If a BMP only appears in a generic glossary or template appendix without project-specific commitment, do not extract.
</edge_cases>

The schema for each emitted item:
{{Schema}}

Return a JSON object containing an `"items"` array (empty only if no Source Control BMP discussion is present anywhere in the document).
