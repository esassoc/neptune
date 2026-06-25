using Neptune.Models.DataTransferObjects;
using System.Linq;

namespace Neptune.EFModels.Entities
{
    public static class FundingEventExtensionMethods
    {
        public static FundingEventDto AsDto(this FundingEvent entity)
        {
            return new FundingEventDto
            {
                FundingEventID = entity.FundingEventID,
                TreatmentBMPID = entity.TreatmentBMPID,
                FundingEventTypeID = entity.FundingEventTypeID,
                Year = entity.Year,
                Description = entity.Description,
                FundingEventFundingSources = entity.FundingEventFundingSources?.Select(x => x.AsSimpleDto()).OrderBy(x => x.FundingSourceName).ThenByDescending(x => x.Amount).ToList() ?? new List<FundingEventFundingSourceSimpleDto>(),
                DisplayName = $"{entity.Year} {(entity.FundingEventType?.FundingEventTypeDisplayName ?? string.Empty)}"
            };
        }

        public static FundingEvent AsEntity(this FundingEventUpsertDto dto, int treatmentBMPID)
        {
            return new FundingEvent
            {
                TreatmentBMPID = treatmentBMPID,
                FundingEventTypeID = dto.FundingEventTypeID,
                Year = dto.Year,
                Description = dto.Description
            };
        }

        public static void UpdateFromUpsertDto(this FundingEvent entity, FundingEventUpsertDto dto)
        {
            entity.FundingEventTypeID = dto.FundingEventTypeID;
            entity.Year = dto.Year;
            entity.Description = dto.Description;
        }
    }
}
