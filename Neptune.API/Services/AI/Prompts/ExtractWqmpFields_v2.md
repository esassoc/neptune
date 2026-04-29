{{EvidenceInstructions}}

<task>
Extract root-level WQMP attributes from the uploaded Water Quality Management Plan PDF (Orange County, CA). For fields whose value should match a controlled vocabulary — `Jurisdiction`, `HydrologicSubarea`, `WaterQualityManagementPlanLandUse`, `WaterQualityManagementPlanPriority`, `WaterQualityManagementPlanStatus`, `WaterQualityManagementPlanDevelopmentType`, `WaterQualityManagementPlanPermitTerm`, `TrashCaptureStatusType`, `HydromodificationAppliesType` — match the document's text to the corresponding list in DomainContext when a close match exists.
</task>

When you cannot determine a field from the document, set its `Value`, `ExtractionEvidence`, `DocumentSource`, and `BoundingBox` to null. Do not infer plausible values from defaults, general knowledge, or related fields — null is the correct answer when the document does not specify.

The schema:
{{Schema}}

Return a single JSON object matching the schema.
