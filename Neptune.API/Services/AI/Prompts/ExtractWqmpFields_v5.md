{{EvidenceInstructions}}

<task>
Extract root-level WQMP attributes from the uploaded Water Quality Management Plan PDF (Orange County, CA). For fields whose value should match a controlled vocabulary — `Jurisdiction`, `HydrologicSubarea`, `WaterQualityManagementPlanLandUse`, `WaterQualityManagementPlanPriority`, `WaterQualityManagementPlanStatus`, `WaterQualityManagementPlanDevelopmentType`, `WaterQualityManagementPlanPermitTerm`, `TrashCaptureStatusType`, `HydromodificationAppliesType` — match the document's text to the corresponding list in DomainContext when a close match exists, and return the list entry exactly as written there. A value that isn't one of the listed names can't be used.
</task>

<field_guidance>
ApprovalDate: the date the reviewing agency approved this WQMP, typically an "APPROVED" stamp or signature block from the city or county, or a field labeled "Approval Date". Plans carry several dates; the prepared, revised, updated, submitted, received and review-completed dates are not the approval date. Approval stamps are often handwritten: read each digit carefully, and if the year is cut off or illegible, leave the field empty rather than guessing it.

TrashCaptureStatusType: whether the project has trash capture devices under the State Trash Provisions, which concern full capture systems: devices that trap all trash 5 mm and larger at the specified design storm. Trash enclosures, dumpster and waste storage areas are source-control measures, not trash capture. Inserts placed in a catch basin, curb inlet or trench drain to filter or screen runoff (filter, screen, basket or media inserts, including catch basin StormFilters, FloGard and Triton inserts) are trash capture devices: use "Partial (>5mm but less than full sizing)" for them unless the document describes them as full capture (for example "full capture", "certified full capture", screens of 5 mm or less sized for the design storm, or trash collection devices installed in all of the project's catch basins), in which case use "Full". Standalone treatment BMPs (bioretention, planters, swales, permeable pavement, infiltration, and filter vaults or manholes that aren't catch basin inserts) are not trash capture. Use "Full" only when the document identifies a full capture device; "Partial (>5mm but less than full sizing)" for other trash capture devices, including the inserts above; "No Trash Capture" when the plan has no trash capture devices; and "Not Provided" when the document doesn't address stormwater treatment at all.

HydromodificationAppliesType: whether hydromodification controls apply, usually addressed in a section on Hydrologic Conditions of Concern (HCOC). Use "Applicable" when the plan says an HCOC exists or hydromodification controls are required, and "Exempt" when it says there is no HCOC, the project is exempt, or hydromodification controls are not required.
</field_guidance>

When you cannot determine a field from the document, set its `Value`, `ExtractionEvidence`, and `DocumentSource` to empty strings (`""`) and set every `BoundingBox` number (`PageNumber`, `X`, `Y`, `Width`, `Height`) to 0. Do not infer plausible values from defaults, general knowledge, or related fields — the empty string is the correct answer when the document does not specify.

The schema:
{{Schema}}

Return a single JSON object matching the schema.
