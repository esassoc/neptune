/*
Post-Deployment Script
--------------------------------------------------------------------------------------
This file is generated on every build, DO NOT modify.
--------------------------------------------------------------------------------------
*/

PRINT N'Neptune.Database - Script.PostDeployment.ReleaseScripts.sql';
GO

:r ".\001-rename fields on ModelingAttribute table.sql"
GO
:r ".\002- remove excess white space from TreatmentBMPType names.sql"
GO
:r ".\003- Add neptune page for upload simplified bmps.sql"
GO
:r ".\004 - Add neptune page for upload ovtas.sql"
GO
:r ".\005 - Add neptune page for WQMP APN upload.sql"
GO
:r ".\006 - Add neptune pages for data hub.sql"
GO
:r ".\007 - Add neptune page Export BMP Inventory to GIS.sql"
GO
:r ".\008 - Add neptune page for new home page.sql"
GO
:r ".\009 - add neptune page for wqmp modeling options.sql"
GO
:r ".\010 - add field definitions for trash home page.sql"
GO
:r ".\011 - add neptune page for wqmp annual report.sql"
GO
:r ".\012 - recalculate OVTA Area Baseline Score.sql"
GO
:r ".\013 - update neptune page content for wqmp apn uploader.sql"
GO
:r ".\014 - add rte for wqmp map.sql"
GO
:r ".\015 - Add Field Definition for DownstreamOfNonModeledBMP.sql"
GO
:r ".\016 - Load 4326 Geometries into LoadGeneratingUnit table.sql"
GO
:r ".\017 - Recalculate Baseline and Progress OVTA Scores.sql"
GO
:r ".\018 - Add CustomAttributeType, TreatmentBMPCustomAttributeType, CustomAttribute and CustomAttributeValues for modeling attributes.sql"
GO
:r ".\019 - Add Dry Weather Flow Override custom attribute for all Modeled Treatment BMPs without it.sql"
GO
:r ".\020 - Drop WaterQualityManagementPlanDocumentVectorStore table.sql"
GO
:r ".\021 - Make Other Design Attributes Optional.sql"
GO
:r ".\022 - Make All Custom Attributes Optional.sql"
GO
:r ".\023 - Associate TrainingVideos with NeptuneArea modules.sql"
GO
:r ".\024 - MakeValid on invalid Delineation geometries.sql"
GO
:r ".\025 - WQMP Verification step help text.sql"
GO
:r ".\026 - Add AnthropicFileID to WaterQualityManagementPlanDocument.sql"
GO
:r ".\027 - Backfill WQMP Status to Active for null rows.sql"
GO
:r ".\028 - Seed gt_pk_metadata for vGeoServerLandUseBlock.sql"
GO
:r ".\029 - WQMP Verification Supporting Documentation help text.sql"
GO
:r ".\030 - Seed ExportAssessmentGeospatialData rich text.sql"
GO
:r ".\031 - NPT-1078 Make Person.WebServiceAccessToken nullable.sql"
GO
:r ".\032 - NPT-1078 Seed WebServices rich text.sql"
GO
:r ".\033 - NPT-1068 Seed OCTAPrioritizationDataHub rich text.sql"
GO
:r ".\034 - NPT-1078 Update WebServices rich text for PowerBI URL token option.sql"
GO
:r ".\035 - NPT-1078 Rename WebServices query param from x-api-key to token.sql"
GO

