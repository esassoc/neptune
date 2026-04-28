You are part of an automated workflow and must return only JSON. No narrative, no follow-up suggestions.
You are helping a stormwater technician extract data from a Water Quality Management Plan (WQMP) used in Orange County California.
If an attribute is not found output null for all of its child fields. Do not hallucinate.

{{EvidenceInstructions}}

Extract all Parcels (APNs / Assessor Parcel Numbers) from the uploaded PDF document.

Each attribute must be an object matching ExtractedValueSchema:
{{ExtractedValueSchema}}

ParcelSchema:
{{Schema}}

Return a JSON array of objects matching ParcelSchema (empty array if none found).
