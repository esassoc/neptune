Create view dbo.vPyQgisWaterQualityManagementPlanLGUInput
as 
Select 
	wqmp.WaterQualityManagementPlanID as WQMPID,
	wqmpb.GeometryNative as WaterQualityManagementPlanBoundary
from dbo.WaterQualityManagementPlanBoundary wqmpb
join dbo.WaterQualityManagementPlan wqmp on wqmpb.WaterQualityManagementPlanID = wqmp.WaterQualityManagementPlanID
Where GeometryNative is not null
	and wqmp.WaterQualityManagementPlanModelingApproachID <> 1 -- exclude detailed WQMPs
	and wqmp.WaterQualityManagementPlanStatusID = 1 -- Active only: the model has no node for an Inactive or Draft WQMP (NPT-1155)
GO