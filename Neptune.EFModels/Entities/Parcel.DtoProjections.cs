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

    public static readonly Expression<Func<Parcel, ParcelGridDto>> AsGridDto = parcel => new ParcelGridDto
    {
        ParcelID = parcel.ParcelID,
        ParcelNumber = parcel.ParcelNumber,
        ParcelAddress = parcel.ParcelAddress,
        ParcelCityState = parcel.ParcelCityState,
        ParcelZipCode = parcel.ParcelZipCode,
        ParcelAreaInAcres = Math.Round(parcel.ParcelAreaInAcres, 2),
    };
}
