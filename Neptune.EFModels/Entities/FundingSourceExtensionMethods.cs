using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities
{
    public static class FundingSourceExtensionMethods
    {

        public static FundingSourceDto AsDto(this FundingSource entity)
        {
            return new FundingSourceDto
            {
                FundingSourceID = entity.FundingSourceID,
                OrganizationID = entity.OrganizationID,
                FundingSourceName = entity.FundingSourceName,
                OrganizationName = entity.Organization.OrganizationName,
                IsActive = entity.IsActive,
                FundingSourceDescription = entity.FundingSourceDescription,
                DisplayName = entity.GetDisplayName()
            };
        }

        public static void UpdateFromUpsertDto(this FundingSource entity, FundingSourceUpsertDto dto)
        {
            entity.OrganizationID = dto.OrganizationID;
            entity.FundingSourceName = dto.FundingSourceName;
            entity.IsActive = dto.IsActive;
            entity.FundingSourceDescription = dto.FundingSourceDescription;
        }

        public static FundingSource AsEntity(this FundingSourceUpsertDto dto)
        {
            return new FundingSource
            {
                OrganizationID = dto.OrganizationID,
                FundingSourceName = dto.FundingSourceName,
                IsActive = dto.IsActive,
                FundingSourceDescription = dto.FundingSourceDescription
            };
        }
    }
}