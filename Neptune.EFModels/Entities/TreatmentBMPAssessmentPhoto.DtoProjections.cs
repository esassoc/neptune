using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPAssessmentPhotoProjections
{
    public static readonly Expression<Func<TreatmentBMPAssessmentPhoto, TreatmentBMPAssessmentPhotoDto>> AsDto =
        x => new TreatmentBMPAssessmentPhotoDto
        {
            TreatmentBMPAssessmentPhotoID = x.TreatmentBMPAssessmentPhotoID,
            TreatmentBMPAssessmentID = x.TreatmentBMPAssessmentID,
            FileResourceID = x.FileResourceID,
            FileResourceGUID = x.FileResource.FileResourceGUID.ToString(),
            Caption = x.Caption,
        };
}
