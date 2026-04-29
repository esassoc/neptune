{{EvidenceInstructions}}

<task>
Extract all Parcels (APNs / Assessor Parcel Numbers) from the uploaded Water Quality Management Plan PDF (Orange County, CA). APNs commonly appear on the cover page, the title block, the project-description section, or in a parcel-listing table near the front of the document. Format is typically `XXX-XX-XXX` or `XXX-XXX-XX`.
</task>

When you cannot determine a value from the document, set the field's `Value`, `ExtractionEvidence`, `DocumentSource`, and `BoundingBox` to null. Do not infer.

The schema for each emitted item:
{{Schema}}

Return a JSON object containing an `"items"` array (empty if no parcels are listed in this document).
