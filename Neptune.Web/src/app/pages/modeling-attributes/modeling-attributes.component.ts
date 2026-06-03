import { Component } from "@angular/core";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { AsyncPipe } from "@angular/common";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { TreatmentBMPModelingAttributesDto } from "src/app/shared/generated/model/treatment-bmp-modeling-attributes-dto";
import { ColDef } from "ag-grid-community";
import { Observable } from "rxjs";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";

@Component({
    selector: "modeling-attributes",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    templateUrl: "./modeling-attributes.component.html",
})
export class ModelingAttributesComponent {
    public modelingAttributes$: Observable<TreatmentBMPModelingAttributesDto[]>;
    public columnDefs: ColDef[];
    public customRichTextTypeID = NeptunePageTypeEnum.ViewTreatmentBMPModelingAttributes;

    constructor(private treatmentBMPService: TreatmentBMPService, private utilityFunctionsService: UtilityFunctionsService) {}

    ngOnInit(): void {
        this.columnDefs = [
            this.utilityFunctionsService.createLinkColumnDef("Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
                FieldDefinitionLabelOverride: "BMP Name",
            }),
            this.utilityFunctionsService.createBooleanColumnDef("Fully Parameterized?", "IsFullyParameterized", {
                FieldDefinitionType: "FullyParameterized",
                FieldDefinitionLabelOverride: "Fully Parameterized?",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Delineation Type", "DelineationTypeName", {
                FieldDefinitionType: "DelineationType",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Delineation Status", "DelineationStatus"),
            this.utilityFunctionsService.createBasicColumnDef("Type", "TreatmentBMPTypeName", {
                FieldDefinitionType: "TreatmentBMPType",
                FieldDefinitionLabelOverride: "Type",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Delineation Area (ac)", "DelineationAreaAcres"),
            this.utilityFunctionsService.createDecimalColumnDef("Modeled Land Use Area (ac)", "ModeledLandUseAreaAcres", {
                FieldDefinitionType: "Area",
                FieldDefinitionLabelOverride: "Land Use Area (ac)",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Jurisdiction", "StormwaterJurisdictionName", "StormwaterJurisdictionID", {
                InRouterLink: "/jurisdictions/",
                FieldDefinitionType: "Jurisdiction",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Watershed", "WatershedName", {
                FieldDefinitionType: "Watershed",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Design Stormwater Depth (in)", "DesignStormwaterDepthInInches", {
                FieldDefinitionType: "DesignStormwaterDepth",
                FieldDefinitionLabelOverride: "Design Stormwater Depth (in)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Average Diverted Flowrate (gpd)", "AverageDivertedFlowrate", {
                FieldDefinitionType: "AverageDivertedFlowrate",
                FieldDefinitionLabelOverride: "Average Diverted Flow Rate (gpd)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Average Treatment Flowrate (cfs)", "AverageTreatmentFlowrate", {
                FieldDefinitionType: "AverageTreatmentFlowrate",
                FieldDefinitionLabelOverride: "Average Treatment Flow Rate (cfs)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Design Dry Weather Treatment Capacity (cfs)", "DesignDryWeatherTreatmentCapacity", {
                FieldDefinitionType: "DesignDryWeatherTreatmentCapacity",
                FieldDefinitionLabelOverride: "Design Dry Weather Treatment Capacity (cfs)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Design Low Flow Diversion Capacity (gpd)", "DesignLowFlowDiversionCapacity", {
                FieldDefinitionType: "DesignLowFlowDiversionCapacity",
                FieldDefinitionLabelOverride: "Design Low Flow Diversion Capacity (gpd)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Design Media Filtration Rate (in/hr)", "DesignMediaFiltrationRate", {
                FieldDefinitionType: "DesignMediaFiltrationRate",
                FieldDefinitionLabelOverride: "Design Media Filtration Rate (in/hr)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Drawdown Time for WQ Detention Volume", "DrawdownTimeForWQDetentionVolume", {
                FieldDefinitionType: "DrawdownTimeForWQDetentionVolume",
                FieldDefinitionLabelOverride: "Drawdown Time For WQ Detention Volume (hours)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Effective Footprint", "EffectiveFootprint", {
                FieldDefinitionType: "EffectiveFootprint",
                FieldDefinitionLabelOverride: "Effective Footprint (sq ft)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Effective Retention Depth", "EffectiveRetentionDepth", {
                FieldDefinitionType: "EffectiveRetentionDepth",
                FieldDefinitionLabelOverride: "Effective Retention Depth (ft)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Infiltration Discharge Rate", "InfiltrationDischargeRate", {
                FieldDefinitionType: "InfiltrationDischargeRate",
                FieldDefinitionLabelOverride: "Infiltration Discharge Rate (cfs)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Infiltration Surface Area", "InfiltrationSurfaceArea", {
                FieldDefinitionType: "InfiltrationSurfaceArea",
                FieldDefinitionLabelOverride: "Infiltration Surface Area (sq ft)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Media Bed Footprint", "MediaBedFootprint", {
                FieldDefinitionType: "MediaBedFootprint",
                FieldDefinitionLabelOverride: "Media Bed Footprint (sq ft)",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Months Operational", "MonthsOperational", {
                FieldDefinitionType: "MonthsOfOperationID",
                FieldDefinitionLabelOverride: "Months Operational",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Permanent Pool/Wetland Volume", "PermanentPoolOrWetlandVolume", {
                FieldDefinitionType: "PermanentPoolOrWetlandVolume",
                FieldDefinitionLabelOverride: "Permanent Pool Or Wetland Volume (cu ft)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Storage Volume Below Lowest Outlet Elevation", "StorageVolumeBelowLowestOutletElevation", {
                FieldDefinitionType: "StorageVolumeBelowLowestOutletElevation",
                FieldDefinitionLabelOverride: "Storage Volume Below Lowest Outlet Elevation (cu ft)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Summer Harvested Water Demand", "SummerHarvestedWaterDemand", {
                FieldDefinitionType: "SummerHarvestedWaterDemand",
                FieldDefinitionLabelOverride: "Summer Harvested Water Demand (gpd)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Time of Concentration", "TimeOfConcentration", {
                FieldDefinitionType: "TimeOfConcentrationID",
                FieldDefinitionLabelOverride: "Time Of Concentration (mins)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Total Effective BMP Volume", "TotalEffectiveBMPVolume", {
                FieldDefinitionType: "TotalEffectiveBMPVolume",
                FieldDefinitionLabelOverride: "Total Effective BMP Volume (cu ft)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Total Effective Drywell BMP Volume", "TotalEffectiveDrywellBMPVolume", {
                FieldDefinitionType: "TotalEffectiveDrywellBMPVolume",
                FieldDefinitionLabelOverride: "Total Effective Drywell BMP Volume (cu ft)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Treatment Rate", "TreatmentRate", {
                FieldDefinitionType: "TreatmentRate",
                FieldDefinitionLabelOverride: "Treatment Rate (cfs)",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Underlying Hydrologic Soil Group", "UnderlyingHydrologicSoilGroup", {
                FieldDefinitionType: "UnderlyingHydrologicSoilGroupID",
                FieldDefinitionLabelOverride: "Underlying Hydrologic Soil Group",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Underlying Infiltration Rate", "UnderlyingInfiltrationRate", {
                FieldDefinitionType: "UnderlyingInfiltrationRate",
                FieldDefinitionLabelOverride: "Underlying Infiltration Rate (in/hr)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Extended Detention Surcharge Volume", "ExtendedDetentionSurchargeVolume", {
                FieldDefinitionType: "ExtendedDetentionSurchargeVolume",
                FieldDefinitionLabelOverride: "Extended Detention Surcharge Volume (cu ft)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Wetted Footprint", "WettedFootprint", {
                FieldDefinitionType: "WettedFootprint",
                FieldDefinitionLabelOverride: "Wetted Footprint (sq ft)",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Winter Harvested Water Demand", "WinterHarvestedWaterDemand", {
                FieldDefinitionType: "WinterHarvestedWaterDemand",
                FieldDefinitionLabelOverride: "Winter Harvested Water Demand (gpd)",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Upstream BMP Name", "UpstreamBMPName", "UpstreamBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "UpstreamBMP",
                FieldDefinitionLabelOverride: "Upstream BMP",
            }),
            this.utilityFunctionsService.createBooleanColumnDef("Downstream of Non-Modeled BMP?", "DownstreamOfNonModeledBMP", {
                FieldDefinitionType: "DownstreamOfNonModeledBMP",
                FieldDefinitionLabelOverride: "Downstream of Non-Modeled BMP?",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Dry Weather Flow Override", "DryWeatherFlowOverride", {
                FieldDefinitionType: "DryWeatherFlowOverrideID",
                FieldDefinitionLabelOverride: "Dry Weather Flow Override",
            }),
        ];
        this.modelingAttributes$ = this.treatmentBMPService.listWithModelingAttributesTreatmentBMP();
    }
}
