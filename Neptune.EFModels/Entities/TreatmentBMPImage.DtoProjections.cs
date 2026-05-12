using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPImageProjections
{
    /// <summary>
    /// Expression projection from a <see cref="TreatmentBMPImage"/> entity to
    /// <see cref="TreatmentBMPImageDto"/>. Used by the BMP image list endpoints so EF emits a
    /// focused SELECT that joins FileResource for the GUID instead of materializing the full
    /// entity graph and mapping in C# memory.
    /// </summary>
    public static readonly Expression<Func<TreatmentBMPImage, TreatmentBMPImageDto>> AsDto = x => new TreatmentBMPImageDto
    {
        TreatmentBMPImageID = x.TreatmentBMPImageID,
        TreatmentBMPID = x.TreatmentBMPID,
        FileResourceID = x.FileResourceID,
        FileResourceGUID = x.FileResource.FileResourceGUID.ToString(),
        Caption = x.Caption,
        UploadDate = x.UploadDate,
    };

    /// <summary>
    /// Carousel-shape projection for assessment photos. Maps a
    /// <see cref="TreatmentBMPAssessmentPhoto"/> into the same <see cref="TreatmentBMPImageDto"/>
    /// shape so the BMP detail carousel can render inventory images and assessment photos through
    /// a single read path. The synthetic negative <c>TreatmentBMPImageID</c> keeps assessment rows
    /// distinguishable from real <see cref="TreatmentBMPImage"/> rows so they never collide and
    /// can never be routed back through the inventory delete/update endpoints by mistake.
    /// UploadDate is set to the parent FieldVisit's VisitDate (date-only) since assessment photos
    /// don't carry their own upload timestamp.
    /// </summary>
    public static Expression<Func<TreatmentBMPAssessmentPhoto, TreatmentBMPImageDto>> AssessmentPhotoAsCarouselDto(int treatmentBMPID) =>
        p => new TreatmentBMPImageDto
        {
            TreatmentBMPImageID = -p.TreatmentBMPAssessmentPhotoID,
            TreatmentBMPID = treatmentBMPID,
            FileResourceID = p.FileResourceID,
            FileResourceGUID = p.FileResource.FileResourceGUID.ToString(),
            Caption = p.Caption,
            UploadDate = DateOnly.FromDateTime(p.TreatmentBMPAssessment.FieldVisit.VisitDate),
        };
}
