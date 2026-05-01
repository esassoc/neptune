using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Neptune.Common;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using Neptune.WebMvc.Common;
using Neptune.WebMvc.Common.Models;
using Neptune.WebMvc.Common.Mvc;
using Neptune.WebMvc.Services;

namespace Neptune.WebMvc.Views.WaterQualityManagementPlan
{
    public class NewWqmpVerifyViewModel : FormViewModel, IValidatableObject
    {
        [Required]
        [DisplayName("Type of Verification")]
        public int WaterQualityManagementPlanVerifyTypeID { get; set; }

        [Required]
        [DisplayName("Visit Status")]
        public int WaterQualityManagementPlanVisitStatusID { get; set; }

        [DisplayName("Structural BMP O&M Document")]
        [SitkaFileExtensions("pdf|zip|doc|docx|xls|xlsx|jpg|png")]
        public IFormFile StructuralDocumentFile { get; set; }

        [Required]
        [DisplayName("Verification Date")]
        public DateOnly VerificationDate { get; set; }

        public int? WaterQualityManagementPlanVerifyStatusID { get; set; }
        public string EnforcementOrFollowupActions { get; set; }
        public string SourceControlCondition { get; set; }

        public List<WaterQualityManagementPlanVerifyQuickBMPDto> WaterQualityManagementPlanVerifyQuickBMPs { get; set; }
        public List<WaterQualityManagementPlanVerifyTreatmentBMPSimpleDto> WaterQualityManagementPlanVerifyTreatmentBMPSimples { get; set; }

        public bool HiddenIsFinalizeVerificationInput { get; set; }

        /// <summary>
        /// Needed by model binder
        /// </summary>
        public NewWqmpVerifyViewModel()
        {
        }

        public NewWqmpVerifyViewModel(EFModels.Entities.WaterQualityManagementPlan waterQualityManagementPlan, WaterQualityManagementPlanVerify waterQualityManagementPlanVerify, List<QuickBMP> quickBMPs, List<EFModels.Entities.TreatmentBMP> treatmentBMPs)
        {
            WaterQualityManagementPlanVerifyTypeID = waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyTypeID;
            WaterQualityManagementPlanVisitStatusID = waterQualityManagementPlanVerify.WaterQualityManagementPlanVisitStatusID;
            WaterQualityManagementPlanVerifyStatusID = waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyStatusID;
            VerificationDate = waterQualityManagementPlanVerify.VerificationDate;
            EnforcementOrFollowupActions = waterQualityManagementPlanVerify.EnforcementOrFollowupActions;
            SourceControlCondition = waterQualityManagementPlanVerify.SourceControlCondition;

            WaterQualityManagementPlanVerifyQuickBMPs = quickBMPs?.Select(x => 
                x.AsWaterQualityManagementPlanVerifyQuickBMPDto()).OrderBy(x => x.QuickBMPName).ToList();
            WaterQualityManagementPlanVerifyTreatmentBMPSimples = treatmentBMPs?.Select(x => x.AsWaterQualityManagementPlanVerifyTreatmentBMPSimpleDto()).OrderBy(x => x.TreatmentBMPName).ToList();
        }

        public async Task UpdateModel(WaterQualityManagementPlanVerify waterQualityManagementPlanVerify, Person currentPerson, NeptuneDbContext dbContext, FileResourceService fileResourceService, List<WaterQualityManagementPlanVerifyQuickBMP> waterQualityManagementPlanVerifyQuickBMPs, List<WaterQualityManagementPlanVerifyTreatmentBMP> waterQualityManagementPlanVerifyTreatmentBMPs)
        {
            if (StructuralDocumentFile != null)
            {
                var fileResource = await fileResourceService.CreateNewFromIFormFile(StructuralDocumentFile, currentPerson);
                if (waterQualityManagementPlanVerify.FileResource == null)
                {
                    waterQualityManagementPlanVerify.FileResource = fileResource;
                }
                else
                {
                    await fileResourceService.DeleteBlobForFileResource(waterQualityManagementPlanVerify.FileResource);
                    dbContext.FileResources.Remove(waterQualityManagementPlanVerify.FileResource);
                    waterQualityManagementPlanVerify.FileResource = fileResource;
                }
            }

            waterQualityManagementPlanVerify.EnforcementOrFollowupActions = EnforcementOrFollowupActions;
            waterQualityManagementPlanVerify.SourceControlCondition = SourceControlCondition;
            waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyStatusID = WaterQualityManagementPlanVerifyStatusID;
            waterQualityManagementPlanVerify.LastEditedByPersonID = currentPerson.PersonID;
            waterQualityManagementPlanVerify.LastEditedDate = DateTime.UtcNow;
            waterQualityManagementPlanVerify.VerificationDate = VerificationDate;

            var allWaterQualityManagementPlanVerifyQuickBMPsInDatabase = dbContext.WaterQualityManagementPlanVerifyQuickBMPs;
            var waterQualityManagementPlanVerifyQuickBMPsToUpdate = WaterQualityManagementPlanVerifyQuickBMPs
                ?.Select(x => new WaterQualityManagementPlanVerifyQuickBMP
                    {
                        WaterQualityManagementPlanVerifyID =
                            waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyID,
                        QuickBMPID = x.QuickBMPID,
                        IsAdequate = x.IsAdequate,
                        WaterQualityManagementPlanVerifyQuickBMPNote = x.WaterQualityManagementPlanVerifyQuickBMPNote

                    }
                ).ToList() ?? new List<WaterQualityManagementPlanVerifyQuickBMP>();

            waterQualityManagementPlanVerifyQuickBMPs.Merge(waterQualityManagementPlanVerifyQuickBMPsToUpdate, allWaterQualityManagementPlanVerifyQuickBMPsInDatabase, (x, y) => x.WaterQualityManagementPlanVerifyID == y.WaterQualityManagementPlanVerifyID && x.QuickBMPID == y.QuickBMPID,
                (x, y) =>
                {
                    x.IsAdequate = y.IsAdequate;
                    x.WaterQualityManagementPlanVerifyQuickBMPNote = y.WaterQualityManagementPlanVerifyQuickBMPNote;
                });


            var allWaterQualityManagementPlanVerifyTreatmentBMPsInDatabase = dbContext.WaterQualityManagementPlanVerifyTreatmentBMPs;
            var waterQualityManagementPlanVerifyTreatmentBMPsToUpdate =
                WaterQualityManagementPlanVerifyTreatmentBMPSimples?.Select(x =>
                    new WaterQualityManagementPlanVerifyTreatmentBMP
                    {
                        WaterQualityManagementPlanVerifyID =
                            waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyID,
                        TreatmentBMPID = x.TreatmentBMPID,
                        IsAdequate = x.IsAdequate,
                        WaterQualityManagementPlanVerifyTreatmentBMPNote =
                            x.WaterQualityManagementPlanVerifyTreatmentBMPNote
                    }).ToList() ?? new List<WaterQualityManagementPlanVerifyTreatmentBMP>();


            waterQualityManagementPlanVerifyTreatmentBMPs.Merge(waterQualityManagementPlanVerifyTreatmentBMPsToUpdate, allWaterQualityManagementPlanVerifyTreatmentBMPsInDatabase, (x, y) => x.WaterQualityManagementPlanVerifyID == y.WaterQualityManagementPlanVerifyID && x.TreatmentBMPID == y.TreatmentBMPID,
                (x, y) =>
                {
                    x.IsAdequate = y.IsAdequate;
                    x.WaterQualityManagementPlanVerifyTreatmentBMPNote = y.WaterQualityManagementPlanVerifyTreatmentBMPNote;
                });
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validationResults = new List<ValidationResult>();
            var waterQualityManagementPlanVerifyQuickBMPNoteMaxLength = WaterQualityManagementPlanVerifyQuickBMP.FieldLengths.WaterQualityManagementPlanVerifyQuickBMPNote;
            var waterQualityManagementPlanVerifyTreatmentBMPNoteMaxLength = WaterQualityManagementPlanVerifyTreatmentBMP.FieldLengths.WaterQualityManagementPlanVerifyTreatmentBMPNote;
            var sourceControlConditionMaxLength = WaterQualityManagementPlanVerify.FieldLengths.SourceControlCondition;
            var enforcementOrFollowupActionsMaxLength = WaterQualityManagementPlanVerify.FieldLengths.EnforcementOrFollowupActions;

            foreach (var waterQualityManagementPlanVerifyQuickBMPDto in WaterQualityManagementPlanVerifyQuickBMPs?? new List<WaterQualityManagementPlanVerifyQuickBMPDto>())
            {
                var waterQualityManagementPlanVerifyQuickBMPSimpleNoteLength = waterQualityManagementPlanVerifyQuickBMPDto.WaterQualityManagementPlanVerifyQuickBMPNote?.Length;
                if (waterQualityManagementPlanVerifyQuickBMPSimpleNoteLength != null && waterQualityManagementPlanVerifyQuickBMPSimpleNoteLength > waterQualityManagementPlanVerifyQuickBMPNoteMaxLength)
                {
                    validationResults.Add(new ValidationResult($"\"{waterQualityManagementPlanVerifyQuickBMPDto.QuickBMPName}\"'s note is too long. Notes have a maximum of {waterQualityManagementPlanVerifyQuickBMPNoteMaxLength} characters and is {waterQualityManagementPlanVerifyQuickBMPSimpleNoteLength - waterQualityManagementPlanVerifyQuickBMPNoteMaxLength} over the limit."));

                }
            }

            foreach (var waterQualityManagementPlanVerifyTreatmentBMPSimple in WaterQualityManagementPlanVerifyTreatmentBMPSimples ?? new List<WaterQualityManagementPlanVerifyTreatmentBMPSimpleDto>() )
            {
                var waterQualityManagementPlanVerifyTreatmentBMPSimpleNoteLength = waterQualityManagementPlanVerifyTreatmentBMPSimple.WaterQualityManagementPlanVerifyTreatmentBMPNote?.Length;
                if (waterQualityManagementPlanVerifyTreatmentBMPSimpleNoteLength != null && waterQualityManagementPlanVerifyTreatmentBMPSimpleNoteLength > waterQualityManagementPlanVerifyTreatmentBMPNoteMaxLength)
                {
                    validationResults.Add(new ValidationResult($"\"{waterQualityManagementPlanVerifyTreatmentBMPSimple.TreatmentBMPName}\"'s note is too long. Notes have a maximum of {waterQualityManagementPlanVerifyTreatmentBMPNoteMaxLength} characters and is {waterQualityManagementPlanVerifyTreatmentBMPSimpleNoteLength - waterQualityManagementPlanVerifyTreatmentBMPNoteMaxLength} over the limit."));

                }
            }

            if (SourceControlCondition != null && SourceControlCondition.Length > sourceControlConditionMaxLength)
            {
                validationResults.Add(new ValidationResult($"\"Source control BMPs\"'s message is too long. It has a maximum of {sourceControlConditionMaxLength} characters and is {SourceControlCondition.Length - sourceControlConditionMaxLength} over the limit."));
            }

            if (EnforcementOrFollowupActions != null && EnforcementOrFollowupActions.Length > enforcementOrFollowupActionsMaxLength)
            {
                validationResults.Add(new ValidationResult($"\"Enforcement or Follow-up Actions\"'s message is too long. It has a maximum of {enforcementOrFollowupActionsMaxLength} characters and is {EnforcementOrFollowupActions.Length - enforcementOrFollowupActionsMaxLength} over the limit."));
            }

            if (HiddenIsFinalizeVerificationInput && WaterQualityManagementPlanVerifyStatusID == WaterQualityManagementPlanVerifyStatus.AdequateOAndMofWQMPisVerified.WaterQualityManagementPlanVerifyStatusID &&
                ((WaterQualityManagementPlanVerifyQuickBMPs != null && WaterQualityManagementPlanVerifyQuickBMPs.Any(x => x.IsAdequate == false)) ||
                 WaterQualityManagementPlanVerifyTreatmentBMPSimples != null && WaterQualityManagementPlanVerifyTreatmentBMPSimples.Any(x => x.IsAdequate == false))) 
            {
                validationResults.Add(new ValidationResult($"Cannot Finalize Verification because Verification of O&M Status is \"{WaterQualityManagementPlanVerifyStatus.AdequateOAndMofWQMPisVerified.WaterQualityManagementPlanVerifyStatusDisplayName}\" but at least one BMP has \"O&M Adequate\" is set to \"No\"."));
            }

            return validationResults;
        }
    }
}
