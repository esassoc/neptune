using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class SourceControlBMPProjections
{
    public static readonly Expression<Func<SourceControlBMP, SourceControlBMPDto>> AsDto = x => new SourceControlBMPDto
    {
        SourceControlBMPID = x.SourceControlBMPID,
        SourceControlBMPAttributeName = x.SourceControlBMPAttribute.SourceControlBMPAttributeName,
        SourceControlBMPAttributeCategoryID = x.SourceControlBMPAttribute.SourceControlBMPAttributeCategoryID,
        IsPresent = x.IsPresent,
        SourceControlBMPNote = x.SourceControlBMPNote,
    };
}
