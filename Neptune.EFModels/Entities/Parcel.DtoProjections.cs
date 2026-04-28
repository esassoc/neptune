using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class ParcelProjections
{
    public static readonly Expression<Func<Parcel, ParcelDisplayDto>> AsDisplayDto = parcel => new ParcelDisplayDto
    {
        ParcelID = parcel.ParcelID,
        ParcelNumber = parcel.ParcelNumber,
        ParcelAddress = parcel.ParcelAddress,
    };
}
