using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1137: Simple BMP grid rows. The server calculates Calculated Boundary Acreage and Drainage Area Treated
    /// so the grid and its CSV export agree (AC 7-9). DB-independent: builds the entities in memory.
    /// </summary>
    [TestClass]
    public class QuickBMPGridDtoTests
    {
        private static QuickBMP BuildQuickBMP(decimal? percentOfSiteTreated = 50m, int? dryWeatherFlowOverrideID = null) => new()
        {
            QuickBMPID = 11,
            WaterQualityManagementPlanID = 22,
            TreatmentBMPTypeID = 33,
            QuickBMPName = "Bioretention A",
            QuickBMPNote = "North lot",
            NumberOfIndividualBMPs = 3,
            PercentOfSiteTreated = percentOfSiteTreated,
            PercentCaptured = 80m,
            PercentRetained = 40m,
            DryWeatherFlowOverrideID = dryWeatherFlowOverrideID,
        };

        private static vWaterQualityManagementPlanDetailed BuildWqmp(decimal? recordedAcres = null, double? calculatedAcres = 0) => new()
        {
            WaterQualityManagementPlanID = 22,
            WaterQualityManagementPlanName = "Newport Plaza",
            StormwaterJurisdictionID = 44,
            StormwaterJurisdictionName = "Newport Beach",
            WaterQualityManagementPlanStatusDisplayName = "Active",
            HydrologicSubareaName = "Newport Bay",
            RecordedWQMPAreaInAcres = recordedAcres,
            CalculatedWQMPAcreage = calculatedAcres,
        };

        [TestMethod]
        public void AsGridDto_MapsQuickBMPAndWqmpFields()
        {
            var dto = BuildQuickBMP(dryWeatherFlowOverrideID: (int)DryWeatherFlowOverrideEnum.Yes).AsGridDto("Bioretention", BuildWqmp(recordedAcres: 2m));

            Assert.AreEqual(11, dto.QuickBMPID);
            Assert.AreEqual("Bioretention A", dto.QuickBMPName);
            Assert.AreEqual(33, dto.TreatmentBMPTypeID);
            Assert.AreEqual("Bioretention", dto.TreatmentBMPTypeName);
            Assert.AreEqual(3, dto.NumberOfIndividualBMPs);
            Assert.AreEqual(22, dto.WaterQualityManagementPlanID);
            Assert.AreEqual("Newport Plaza", dto.WaterQualityManagementPlanName);
            Assert.AreEqual(44, dto.StormwaterJurisdictionID);
            Assert.AreEqual("Newport Beach", dto.StormwaterJurisdictionName);
            Assert.AreEqual("Active", dto.WaterQualityManagementPlanStatusDisplayName);
            Assert.AreEqual("Newport Bay", dto.HydrologicSubareaName);
            Assert.AreEqual(50m, dto.PercentOfSiteTreated);
            Assert.AreEqual(80m, dto.PercentCaptured);
            Assert.AreEqual(40m, dto.PercentRetained);
            Assert.AreEqual(DryWeatherFlowOverride.Yes.DryWeatherFlowOverrideDisplayName, dto.DryWeatherFlowOverrideDisplayName);
            Assert.AreEqual("North lot", dto.QuickBMPNote);
        }

        [TestMethod]
        public void BothAreas_ShownSeparately_DrainageAreaUsesCalculatedArea()
        {
            var dto = BuildQuickBMP().AsGridDto("Bioretention", BuildWqmp(recordedAcres: 2m, calculatedAcres: 5.5));

            Assert.AreEqual(2m, dto.RecordedWQMPAreaInAcres);
            Assert.AreEqual(5.5m, dto.CalculatedBoundaryAcreage);
            Assert.AreEqual(2.75m, dto.DrainageAreaTreatedAcres);
        }

        [TestMethod]
        public void CalculatedAreaOnly_DrainageAreaUsesCalculatedArea()
        {
            var dto = BuildQuickBMP().AsGridDto("Bioretention", BuildWqmp(recordedAcres: null, calculatedAcres: 5.5));

            Assert.IsNull(dto.RecordedWQMPAreaInAcres);
            Assert.AreEqual(5.5m, dto.CalculatedBoundaryAcreage);
            Assert.AreEqual(2.75m, dto.DrainageAreaTreatedAcres);
        }

        [TestMethod]
        public void NoBoundary_CalculatedAreaBlank_DrainageAreaFallsBackToRecordedArea()
        {
            // The view reports a WQMP with no boundary as 0 calculated acres.
            var dto = BuildQuickBMP().AsGridDto("Bioretention", BuildWqmp(recordedAcres: 2m, calculatedAcres: 0));

            Assert.AreEqual(2m, dto.RecordedWQMPAreaInAcres);
            Assert.IsNull(dto.CalculatedBoundaryAcreage);
            Assert.AreEqual(1m, dto.DrainageAreaTreatedAcres);
        }

        [TestMethod]
        public void NoRecordedAreaAndNoBoundary_AreasAndDrainageAreaAreBlank()
        {
            var dto = BuildQuickBMP().AsGridDto("Bioretention", BuildWqmp(recordedAcres: null, calculatedAcres: 0));

            Assert.IsNull(dto.RecordedWQMPAreaInAcres);
            Assert.IsNull(dto.CalculatedBoundaryAcreage);
            Assert.IsNull(dto.DrainageAreaTreatedAcres);
        }

        [TestMethod]
        public void NullPercentOfSiteTreated_DrainageAreaIsBlank()
        {
            var dto = BuildQuickBMP(percentOfSiteTreated: null).AsGridDto("Bioretention", BuildWqmp(recordedAcres: 2m, calculatedAcres: 5.5));

            Assert.AreEqual(5.5m, dto.CalculatedBoundaryAcreage);
            Assert.IsNull(dto.DrainageAreaTreatedAcres);
        }

        [TestMethod]
        public void DrainageArea_RoundsToTwoDecimalPlaces()
        {
            // 33.33% of 1.005 ac = 0.33496... -> 0.33
            Assert.AreEqual(0.33m, QuickBMPExtensionMethods.GetDrainageAreaTreatedAcres(33.33m, 1.005m));
            // 50% of 0.25 ac = 0.125 -> 0.13 (midpoint away from zero, not banker's 0.12)
            Assert.AreEqual(0.13m, QuickBMPExtensionMethods.GetDrainageAreaTreatedAcres(50m, 0.25m));
        }

        [TestMethod]
        public void DryWeatherFlowOverride_DisplayNameForEachValue()
        {
            var wqmp = BuildWqmp(recordedAcres: 1m);

            Assert.AreEqual(DryWeatherFlowOverride.No.DryWeatherFlowOverrideDisplayName,
                BuildQuickBMP(dryWeatherFlowOverrideID: (int)DryWeatherFlowOverrideEnum.No).AsGridDto("Type", wqmp).DryWeatherFlowOverrideDisplayName);
            Assert.AreEqual(DryWeatherFlowOverride.Yes.DryWeatherFlowOverrideDisplayName,
                BuildQuickBMP(dryWeatherFlowOverrideID: (int)DryWeatherFlowOverrideEnum.Yes).AsGridDto("Type", wqmp).DryWeatherFlowOverrideDisplayName);
            Assert.IsNull(BuildQuickBMP(dryWeatherFlowOverrideID: null).AsGridDto("Type", wqmp).DryWeatherFlowOverrideDisplayName);
        }
    }
}
