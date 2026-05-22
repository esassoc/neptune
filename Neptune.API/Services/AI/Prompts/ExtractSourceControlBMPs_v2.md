{{EvidenceInstructions}}

<task>
Extract all **Source Control BMPs** from the uploaded Water Quality Management Plan PDF (Orange County, CA). Source Control BMPs are **non-structural / operational measures** — for example storm-drain stenciling, trash management practices, employee training, vehicle wash protocols, spill response. They are distinct from the structural treatment BMPs handled by the QuickBMPs extraction. Match each finding's `SourceControlBMPAttribute` value to a name from DomainContext's `SourceControlBMPAttributes` list when a close match exists.
</task>

When you cannot determine a value from the document, set the field's `Value`, `ExtractionEvidence`, `DocumentSource`, and `BoundingBox` to null. Do not infer.

The schema for each emitted item:
{{Schema}}

Return a JSON object containing an `"items"` array (empty if no Source Control BMPs are described in this document).
