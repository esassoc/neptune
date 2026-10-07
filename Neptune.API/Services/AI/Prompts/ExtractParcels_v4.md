{{EvidenceInstructions}}

<task>
Extract all Parcels (APNs / Assessor Parcel Numbers) from the uploaded Water Quality Management Plan PDF (Orange County, CA). APNs commonly appear on the cover page, the title block, the project-description section, or in a parcel-listing table near the front of the document.

Orange County APNs have 8 digits, written `XXX-XXX-XX`, or `XXX-XX-XXX` for condominium parcels. Return each APN in one of those forms. If the document pads the last group with a leading zero (for example `691-101-001`), return the 8-digit form (`691-101-01`). Only return numbers the document identifies as an APN or assessor's parcel number; tract, lot, permit and record numbers are not APNs.
</task>

When you cannot determine a value from the document, set the field's `Value`, `ExtractionEvidence`, and `DocumentSource` to empty strings (`""`) and set every `BoundingBox` number (`PageNumber`, `X`, `Y`, `Width`, `Height`) to 0. Do not infer.

The schema for each emitted item:
{{Schema}}

Return a JSON object containing an `"items"` array (empty if no parcels are listed in this document).
