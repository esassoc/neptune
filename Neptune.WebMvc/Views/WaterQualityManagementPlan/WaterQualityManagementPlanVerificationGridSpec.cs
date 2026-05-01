using Neptune.WebMvc.Security;
using Microsoft.AspNetCore.Html;
using Neptune.EFModels.Entities;
using Neptune.WebMvc.Common;
using Neptune.WebMvc.Common.DhtmlWrappers;
using Neptune.WebMvc.Controllers;

namespace Neptune.WebMvc.Views.WaterQualityManagementPlan
{
    public class WaterQualityManagementPlanVerificationGridSpec : GridSpec<WaterQualityManagementPlanVerify>
    {
        public WaterQualityManagementPlanVerificationGridSpec(LinkGenerator linkGenerator, Person currentPerson)
        {
            // Reusing permissions checks from WQMPs for WQMP Verifications
            var waterQualityManagementPlanManageFeature = new WaterQualityManagementPlanManageFeature();
            var waterQualityManagementPlanDeleteFeature = new WaterQualityManagementPlanDeleteFeature();

            var currentUserCanManage = waterQualityManagementPlanManageFeature.HasPermissionByPerson(currentPerson);

            ObjectNameSingular = "Water Quality Management Plan O&M Verification";
            ObjectNamePlural = "Water Quality Management Plan O&M Verifications";
            SaveFiltersInCookie = true;
            var isAnonymousOrUnassigned = currentPerson.IsAnonymousOrUnassigned();

            var wqmpDetailUrlTemplate = new UrlTemplate<int>(SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(linkGenerator, x => x.Detail(UrlTemplate.Parameter1Int)));
            var detailUrlTemplate = new UrlTemplate<int>(SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(linkGenerator, x => x.WqmpVerify(UrlTemplate.Parameter1Int)));
            var deleteUrlTemplate = new UrlTemplate<int>(SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(linkGenerator, x => x.DeleteVerify(UrlTemplate.Parameter1Int)));
            var stormwaterJurisdictionDetailUrlTemplate = new UrlTemplate<int>(SitkaRoute<JurisdictionController>.BuildUrlFromExpression(linkGenerator, x => x.Detail(UrlTemplate.Parameter1Int)));

            if (currentUserCanManage)
            {
                Add(string.Empty, x =>
                    {
                        var userHasDeletePermission = waterQualityManagementPlanDeleteFeature
                            .HasPermission(currentPerson, x.WaterQualityManagementPlan).HasPermission;
                        return DhtmlxGridHtmlHelpers.MakeDeleteIconAndLinkBootstrap(deleteUrlTemplate.ParameterReplace(x.WaterQualityManagementPlanVerifyID),
                            userHasDeletePermission);
                    }, 26,
                    DhtmlxGridColumnFilterType.None);
            }

            if (!isAnonymousOrUnassigned)
            {
                Add(string.Empty,
                    x => UrlTemplate.MakeHrefString(detailUrlTemplate.ParameterReplace(x.WaterQualityManagementPlanVerifyID), "View",
                        new Dictionary<string, string> {{"class", "gridButton"}}), 60,
                    DhtmlxGridColumnFilterType.None);
            }
            Add("WQMP Name", x => UrlTemplate.MakeHrefString(wqmpDetailUrlTemplate.ParameterReplace(x.WaterQualityManagementPlanID), x.WaterQualityManagementPlan.WaterQualityManagementPlanName), 300, DhtmlxGridColumnFilterType.Text);
            Add("Jurisdiction", x => isAnonymousOrUnassigned ? new HtmlString(x.WaterQualityManagementPlan.StormwaterJurisdiction.GetOrganizationDisplayName()) :
                UrlTemplate.MakeHrefString(stormwaterJurisdictionDetailUrlTemplate.ParameterReplace(x.WaterQualityManagementPlan.StormwaterJurisdictionID), x.WaterQualityManagementPlan.StormwaterJurisdiction.GetOrganizationDisplayName()), 150);
            Add("Verification Date", x => x.VerificationDate.ToDateTime(TimeOnly.MinValue), 150);
            Add("Last Edited Date", x => x.LastEditedDate, 150);
            Add("Last Edited By", x => x.LastEditedByPerson.GetFullNameFirstLast(), 150, DhtmlxGridColumnFilterType.SelectFilterStrict);
            Add("Type of Verification",
                x => x.WaterQualityManagementPlanVerifyType.WaterQualityManagementPlanVerifyTypeName, 150, DhtmlxGridColumnFilterType.SelectFilterStrict);
            Add("Visit Status", x => x.WaterQualityManagementPlanVisitStatus.WaterQualityManagementPlanVisitStatusName,
                150, DhtmlxGridColumnFilterType.SelectFilterStrict);
            Add("Verification Status",
                x => x.WaterQualityManagementPlanVerifyStatus?.WaterQualityManagementPlanVerifyStatusName, 200, DhtmlxGridColumnFilterType.SelectFilterStrict);
            Add("Source Control Condition", x => x.SourceControlCondition, 150);
            Add("Enforcement of Follow-up Actions", x => x.EnforcementOrFollowupActions, 150);
            Add("Draft or Finalized",
                x => x.IsDraft ? "Draft" : "Finalized", 100, DhtmlxGridColumnFilterType.SelectFilterStrict);
        }
    }
}

 