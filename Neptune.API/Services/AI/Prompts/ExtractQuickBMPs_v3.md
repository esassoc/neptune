{{EvidenceInstructions}}

<task>
Extract all **Simplified Structural BMPs** (a.k.a. "Simple BMPs" or "QuickBMPs") that this Water Quality Management Plan (WQMP) describes — stormwater control measures that this specific Orange County, CA project has committed to. This is **not** the global Treatment BMP inventory; scope each extraction to the BMPs in this document for this site.
</task>

<where_to_look>
The OC WQMP form has two stylistic regions for BMP listings. You must read both. **Section numbers below reflect the standard OC template** — older variants and non-standard documents may use different numbering or omit sections. **Anchor your reading on the content / intent of each section, not on the literal section number.** If you see a section described by content that fits one of the categories below, treat it as that category regardless of how it's numbered.

**Pre-printed checkbox forms** — typically Sections IV.3.1 through IV.3.6 (sometimes also IV.3.8, IV.3.9). Recognize by structure: a fixed list of BMP names with an "Included?" column, where selection is signaled by a graphical mark (☑, ✓, X, filled circle, or similar) next to the name. There is often no underlying text saying "selected." A marked box is positive selection evidence on equal footing with a free-form table row. Skip blank / unchecked rows.

What the checkbox sections typically cover, in standard OC order — match by intent, not number:

- **Hydrologic Source Controls (HSCs):** site-design measures — permeable pavement, planter boxes, green roofs, etc. (typically IV.3.1)
- **Infiltration BMPs:** basins, drywells, trenches (typically IV.3.2)
- **Harvest-and-Use BMPs:** cisterns, rain barrels (typically IV.3.3)
- **Biotreatment BMPs:** bioretention, biofilters, vegetated swales, and most proprietary biotreatment products (typically IV.3.4)
- **Hydromodification Control BMPs:** flow-duration control tanks, underground detention, CMP detention pipes — these BMPs typically *do not* appear in the free-form table; they are selected here (typically IV.3.5)
- **Source Control BMPs (operational/non-structural):** covered by a separate extraction — ignore for QuickBMPs (typically IV.3.6)

**Free-form treatment-control tables** — typically Section IV.3.7. Recognize by structure: two-column "BMP Name | BMP Description" tables filled in by the engineer, often with vendor names, model numbers, and sizing. Extract one BMP per row.

If a free-form table cross-references a checkbox section (e.g. *"See Proprietary Biotreatment BMP table above"*), follow the cross-reference and extract from the referenced content. If the same BMP appears in both a checkbox form and a free-form table, emit it **once** (de-dupe).

Footnotes attached to a BMP table count as part of that table's content — extract BMPs mentioned only in a footnote (e.g. trash-capture pipe screens listed under a biotreatment table footnote).

**Variant templates exist.** The "North OC Priority WQMP Template August 2011" omits the free-form treatment-control table and describes structural treatment BMPs directly in the Biotreatment section. Some other templates use entirely custom numbering or merge categories. In any layout, prioritize content semantics — *is this site committing to this BMP?* — over the document's organizational scheme.

**Non-standard or unrecognized BMPs.** If the document selects a BMP that doesn't fit any vendor alias or canonical category cleanly, extract it anyway: put the document's name verbatim in `QuickBMPName`, put your best canonical guess (or the document's own type label if it gave one) in `TreatmentBMPType`, and leave staff to remap during review. Do not skip a BMP because it doesn't fit the template — a missed BMP is worse than a mis-typed one.

Other supporting evidence: BMP fact sheets / cut-sheets in WQMP appendices, sizing calculation pages, treatment train diagrams, Section V (Operation & Maintenance) narratives that enumerate the project's BMPs. These often disambiguate which checkboxes were marked when a scanned page is unclear.
</where_to_look>

<positive_selection_rule>
Some WQMPs include catalog or reference tables listing BMP types that are **not** selected for this project — every row may be marked "N/A" or left blank. Do **not** extract these.

Emit a BMP only when at least one of the following positive signals is present for that specific row/instance:

- A checked / marked / X'd / filled box next to the BMP name in a Section IV.3.x form.
- An explicit "Selected: Yes" / "Included" / "Provided" mark.
- An assigned `% Site Treated`, drainage area assignment, or sizing calculation for this site.
- A project-specific fact sheet, sizing page, or O&M narrative naming this BMP as installed on the project.

If a row is marked "N/A", left blank, or appears in a generic reference appendix without any of the above, do **not** extract it.
</positive_selection_rule>

<vendor_aliases>
Engineers commonly write vendor product names rather than the canonical `TreatmentBMPType`. Map vendor names to the canonical type when populating the `TreatmentBMPType` field — use a name from DomainContext's `TreatmentBMPTypes` list when available. Keep the vendor name in `QuickBMPName`.

| Vendor name in document | Canonical `TreatmentBMPType` |
|---|---|
| Modular Wetlands / Modular Wetland System / MWS / MWS-L-* | Proprietary Biotreatment |
| UrbanGreen Biofilter | Proprietary Biotreatment |
| Filterra (any model, incl. Roofdrain System) | Proprietary Biotreatment |
| Contech StormFilter | Proprietary Biotreatment |
| Americast (vendor of Filterra) | Proprietary Biotreatment |
| Bioclean Modular Connector / Connector Pipe Screen | Inlet and Pipe Screens |
| Bioclean Grate Inlet Filter | Catch Basin / Inlet (unscreened) |
| FloGard (catch basin filter insert) | Catch Basin / Inlet (unscreened) |
| Kristar | Catch Basin / Inlet (unscreened) — verify in document |
| CMP Underground Detention / corrugated metal pipe detention | Flow Duration Control Tank |
| Stormwater Planter Box (with or without underdrains) | Bioinfiltration (bioretention with underdrain) |
| Bioswale | Vegetated Swale |
| Hydrodynamic Separator | Hydrodynamic Separator |

<example>
Document text:
"1) 4x4 Filterra Roofdrain System: LAT: 33°38'51.42"; LONG: 117° 44'07.38""

Correct extraction (vendor name in QuickBMPName, canonical type in TreatmentBMPType):

```
{
  "QuickBMPName":     "4x4 Filterra Roofdrain System",
  "TreatmentBMPType": "Proprietary Biotreatment"
}
```

Wrong: leaving `TreatmentBMPType` as "Filterra Roof Drain System" (a vendor name — does not match any canonical `TreatmentBMPType` and will fail dropdown bind during staff review).
</example>

<example>
Document mentions two distinct Bioclean products:

- "Bioclean Modular Connector Pipe Screens" — trash-capture screens at storm-drain pipe inlets
- "Bioclean Grate Inlet Filter" — filter insert that sits inside a catch basin grate

Correct extraction (two separate items, different canonical types — same vendor, different BMP categories):

```
[
  { "QuickBMPName": "Bioclean Modular Connector Pipe Screen", "TreatmentBMPType": "Inlet and Pipe Screens" },
  { "QuickBMPName": "Bioclean Grate Inlet Filter",            "TreatmentBMPType": "Catch Basin / Inlet (unscreened)" }
]
```
</example>
</vendor_aliases>

<output_fields>
For each Simple BMP found, emit one object containing the following ExtractedValue fields:

| Field | Guidance |
|---|---|
| `QuickBMPName` | The name as written in the WQMP (e.g. "Bioretention Area #1", "MWS DMA D1 (MWS-L-6-8 Unit A)", "4x4 Filterra Roofdrain System"). Keep vendor names, numbering, and DMA/subarea identifiers. If individual instances are distinguished by drainage area or model (e.g. MWS 1 in DA 1, MWS 2 in DA 2), emit one object per instance — these are distinct BMPs. |
| `TreatmentBMPType` | Canonical BMP type/category. Apply the vendor-alias table above. Prefer a name from DomainContext's `TreatmentBMPTypes` list. |
| `NumberOfIndividualBMPs` | Integer count of physical units this row represents. **Defer to nearby quantity language** ("two planter boxes", "three 4'x6' units", "the 4 MWSs and 2 bioswales") rather than guessing. Default to 1 only if the document gives no count signal at all. If a single BMP type covers multiple identical instances enumerable as one row (e.g. "10 catch basin inserts at the perimeter"), emit one object with `NumberOfIndividualBMPs=10`. |
| `PercentOfSiteTreated` | Numeric percent (0–100) of the WQMP site this BMP treats. Often shown in BMP sizing tables or treatment train diagrams. |
| `PercentCaptured` | Numeric percent (0–100) of the design storm captured/intercepted by this BMP. If given as a range ("80–90% capture"), emit the higher value. |
| `PercentRetained` | Numeric percent (0–100) retained on-site (infiltrated, evapotranspired, harvested). Must be ≤ `PercentCaptured` — anything captured but not retained is treated and discharged. If the source document violates this constraint, still extract both verbatim — staff will reconcile during review. |
| `QuickBMPNote` | Free-form note (≤200 chars) — sizing rationale, model number, drainage area assignment, or qualifiers like *"Supplemental Only. Not part of the Design Volume"*. |
</output_fields>

<edge_cases>
- If the document discusses BMPs in the abstract — a list of "could be used" measures with no commitment to specific instances on this site — do not extract them.
- A BMP qualified as "Supplemental Only" or "Not part of Design Volume" should still be extracted; surface the qualifier in `QuickBMPNote`.
- If the same BMP is described in multiple sections (e.g. IV.3.4 and IV.3.7), emit it once.
- **When uncertain about a value, set the field's `Value`, `ExtractionEvidence`, `DocumentSource`, and `BoundingBox` to null.** Do not infer plausible values from defaults, general knowledge, or other documents — null is the correct answer when the document does not specify.
</edge_cases>

The schema for each emitted item:
{{Schema}}

Return a JSON object containing an `"items"` array (empty if no Simple BMPs are described in this document).
