using Neptune.EFModels.Entities;

namespace Neptune.EFModels.Nereid
{
    public static class NereidUtilities
    {
        public static string RegionalSubbasinNodeID(int regionalSubbasinCatchmentID)
        {
            return $"RSB_{regionalSubbasinCatchmentID}";
        }

        public static string TreatmentBMPNodeID(int treatmentBMPID)
        {
            return $"BMP_{treatmentBMPID}";
        }

        public static string WaterQualityManagementPlanNodeID(int waterQualityManagementPlanID,
            int regionalSubbasinCatchmentID)
        {
            return $"WQMP_{waterQualityManagementPlanID}_RSB_{regionalSubbasinCatchmentID}";
        }

        public static string WaterQualityManagementPlanTreatmentNodeID(int waterQualityManagementPlanID,
            int regionalSubbasinCatchmentID)
        {
            return $"WQMP_{waterQualityManagementPlanID}_RSB_{regionalSubbasinCatchmentID}-TMNT";
        }

        public static string DelineationNodeID(int delineationID)
        {
            return $"Delineation_{delineationID}";
        }

        public static string LandSurfaceNodeID(vNereidLoadingInput loadGeneratingUnit, List<int> projectDelineationIDs = null)
        {
            // provisional delineations are tracked in the LGU layer, but do not contribute runoff
            // to their respective BMPs in the model therefore those LGUs should fall back to their
            // WQMP if exists, otherwise their RSB.
            // If the associated BMP belongs to a Simple WQMP, it should not be accounted for in the modeling 

            if (loadGeneratingUnit.DelineationID != null &&
                (loadGeneratingUnit.DelineationIsVerified == true || (projectDelineationIDs != null && projectDelineationIDs.Contains(loadGeneratingUnit.DelineationID.Value))) &&
                loadGeneratingUnit.RelationallyAssociatedModelingApproach != WaterQualityManagementPlanModelingApproach.Simplified.WaterQualityManagementPlanModelingApproachID)
            {
                return DelineationNodeID(loadGeneratingUnit.DelineationID.Value);
            }

            // Parcel Boundaries of Detailed WQMPs should not be considered.
            // NPT-1155: neither should those of a WQMP that is not Active. The graph has no node for it
            // (NereidService.GetWaterQualityManagementPlanNodes), and Nereid silently discards a land
            // surface whose node it does not know, so the ground goes to its Regional Subbasin instead.
            if (loadGeneratingUnit.WaterQualityManagementPlanID != null &&
                loadGeneratingUnit.SpatiallyAssociatedWaterQualityManagementPlanStatusID == WaterQualityManagementPlanStatus.Active.WaterQualityManagementPlanStatusID &&
                loadGeneratingUnit.SpatiallyAssociatedModelingApproach != WaterQualityManagementPlanModelingApproach.Detailed.WaterQualityManagementPlanModelingApproachID)
            {
                return WaterQualityManagementPlanNodeID(loadGeneratingUnit.WaterQualityManagementPlanID.Value,
                    loadGeneratingUnit.OCSurveyCatchmentID);
            }

            return RegionalSubbasinNodeID(loadGeneratingUnit.OCSurveyCatchmentID);
        }

        public static async Task MarkTreatmentBMPDirty(TreatmentBMP treatmentBMP, NeptuneDbContext dbContext)
        {
            var dirtyModelNode = new DirtyModelNode()
            {
                CreateDate = DateTime.UtcNow,
                TreatmentBMPID = treatmentBMP.TreatmentBMPID
            };

            await dbContext.DirtyModelNodes.AddAsync(dirtyModelNode);
            await dbContext.SaveChangesAsync();
        }

        public static async Task MarkTreatmentBMPDirty(IEnumerable<TreatmentBMP> treatmentBmpsUpdated, NeptuneDbContext dbContext)
        {
            var dirtyModelNodes = treatmentBmpsUpdated.Select(x=> new DirtyModelNode()
            {
                CreateDate = DateTime.UtcNow,
                TreatmentBMPID = x.TreatmentBMPID
            });

            await dbContext.DirtyModelNodes.AddRangeAsync(dirtyModelNodes);
            await dbContext.SaveChangesAsync();
        }

        public static async Task MarkDownstreamNodeDirty(TreatmentBMP treatmentBMP, NeptuneDbContext dbContext)
        {
            // if this bmp is an upstream, then its downstream node is, obviously...
            if (treatmentBMP.InverseUpstreamBMP.Any())
            {
                await MarkTreatmentBMPDirty(treatmentBMP.InverseUpstreamBMP.ToList(), dbContext);
                return;
            }

            // otherwise, we're looking for either the Regional Subbasin or the Centralized BMP of the Regional Subbasin
            var regionalSubbasinID = treatmentBMP.RegionalSubbasinID;

            var centralizedBMP = dbContext.vNereidRegionalSubbasinCentralizedBMPs.SingleOrDefault(x => x.RegionalSubbasinID == regionalSubbasinID && x.RowNumber == 1);
            if (centralizedBMP != null)
            {
                await MarkTreatmentBMPDirty(centralizedBMP, dbContext);
                return;
            }

            // no centralized BMPs there, just go ahead and mark the regional subbasin
            await MarkRegionalSubbasinDirty(regionalSubbasinID, dbContext);

        }

        public static async Task MarkDownstreamNodeDirty(WaterQualityManagementPlan waterQualityManagementPlan, NeptuneDbContext dbContext)
        {
            // otherwise, we're looking for either the Regional Subbasin or the Centralized BMP of the Regional Subbasin
            var regionalSubbasinIDs = waterQualityManagementPlan.LoadGeneratingUnits.Select(x => x.RegionalSubbasinID)
                .Distinct().ToList();

            await MarkRegionalSubbasinsAndTheirCentralizedBMPsDirty(regionalSubbasinIDs, dbContext);
        }

        /// <summary>
        /// Marks each regional subbasin, and its centralized BMP if it has one, as needing a re-solve.
        /// For a change that adds or removes a node upstream of them (a WQMP entering or leaving the
        /// model, NPT-1159): marking only the node that changed does nothing once that node is gone.
        /// </summary>
        public static async Task MarkRegionalSubbasinsAndTheirCentralizedBMPsDirty(List<int?> regionalSubbasinIDs, NeptuneDbContext dbContext)
        {
            var centralizedBMP = dbContext.vNereidRegionalSubbasinCentralizedBMPs.Where(x =>
                regionalSubbasinIDs.Contains(x.RegionalSubbasinID) && x.RowNumber == 1).ToList();

            foreach (var bmp in centralizedBMP)
            {
                await MarkTreatmentBMPDirty(bmp, dbContext);
            }

            foreach (var regionalSubbasinID in regionalSubbasinIDs)
            {
                await MarkRegionalSubbasinDirty(regionalSubbasinID, dbContext);
            }
        }

        private static async Task MarkRegionalSubbasinDirty(int? regionalSubbasinID, NeptuneDbContext dbContext)
        {
            var dirtyModelNode = new DirtyModelNode()
            {
                CreateDate = DateTime.UtcNow,
                RegionalSubbasinID = regionalSubbasinID
            };

            await dbContext.DirtyModelNodes.AddAsync(dirtyModelNode);
            await dbContext.SaveChangesAsync();
        }

        private static async Task MarkTreatmentBMPDirty(vNereidRegionalSubbasinCentralizedBMP treatmentBMP, NeptuneDbContext dbContext)
        {
            var dirtyModelNode = new DirtyModelNode()
            {
                CreateDate = DateTime.UtcNow,
                TreatmentBMPID = treatmentBMP.TreatmentBMPID
            };

            await dbContext.DirtyModelNodes.AddAsync(dirtyModelNode);
            await dbContext.SaveChangesAsync();
        }

        public static async Task MarkDelineationDirty(IEnumerable<Delineation> delineations, NeptuneDbContext dbContext)
        {
            foreach (var delineation in delineations)
            {
                var dirtyModelNode = new DirtyModelNode()
                {
                    CreateDate = DateTime.UtcNow,
                    DelineationID = delineation.DelineationID
                };

                await dbContext.DirtyModelNodes.AddAsync(dirtyModelNode);
            }

            await dbContext.SaveChangesAsync();
        }

        public static async Task MarkDelineationDirty(Delineation delineation, NeptuneDbContext dbContext)
        {
            var dirtyModelNode = new DirtyModelNode()
            {
                CreateDate = DateTime.UtcNow,
                DelineationID = delineation.DelineationID
            };

            await dbContext.DirtyModelNodes.AddAsync(dirtyModelNode);
            await dbContext.SaveChangesAsync();
        }

        public static async Task MarkWqmpDirty(WaterQualityManagementPlan waterQualityManagementPlan, NeptuneDbContext dbContext)
        {
            var dirtyModelNode = new DirtyModelNode()
            {
                CreateDate = DateTime.UtcNow,
                WaterQualityManagementPlanID = waterQualityManagementPlan.WaterQualityManagementPlanID
            };

            await dbContext.DirtyModelNodes.AddAsync(dirtyModelNode);

            await dbContext.SaveChangesAsync();
        }
    }
    ;
    public class DuplicateNodeException : Exception
    {
        public DuplicateNodeException(string nodeID, Exception exception) : base(
            $"Duplicate Node ID in solution graph: {nodeID}", exception)
        {

        }
    }

    public class WaterQualityManagementPlanNode
    {
        public int WaterQualityManagementPlanID { get; set; }
        public int RegionalSubbasinID { get; set; }
        public int OCSurveyCatchmentID { get; set; }
        public DateTime? DateOfConstruction { get; set; }

        public string UniqueID => $"{WaterQualityManagementPlanID}_{OCSurveyCatchmentID}";
    }

    public class WaterQualityManagementPlanNodeComparer : IEqualityComparer<WaterQualityManagementPlanNode>
    {
        public bool Equals(WaterQualityManagementPlanNode x, WaterQualityManagementPlanNode y)
        {
            return x.UniqueID.Equals(y.UniqueID, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(WaterQualityManagementPlanNode obj)
        {
            return obj.UniqueID.GetHashCode() ^ obj.UniqueID.GetHashCode();
        }
    }
}
