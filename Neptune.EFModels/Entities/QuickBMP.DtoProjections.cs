using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class QuickBMPProjections
{
    public static readonly Expression<Func<QuickBMP, QuickBMPDto>> AsDto = x => new QuickBMPDto
    {
        QuickBMPID = x.QuickBMPID,
        QuickBMPName = x.QuickBMPName,
        TreatmentBMPTypeID = x.TreatmentBMPTypeID,
        TreatmentBMPTypeName = x.TreatmentBMPType.TreatmentBMPTypeName,
        QuickBMPNote = x.QuickBMPNote,
        NumberOfIndividualBMPs = x.NumberOfIndividualBMPs,
        PercentOfSiteTreated = x.PercentOfSiteTreated,
        PercentCaptured = x.PercentCaptured,
        PercentRetained = x.PercentRetained,
        DryWeatherFlowOverrideID = x.DryWeatherFlowOverrideID,
    };
}
