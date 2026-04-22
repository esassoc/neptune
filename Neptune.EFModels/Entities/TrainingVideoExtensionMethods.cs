using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities
{
    public static class TrainingVideoExtensionMethods
    {
        public static TrainingVideoDto AsDto(this TrainingVideo entity)
        {
            return new TrainingVideoDto
            {
                TrainingVideoID = entity.TrainingVideoID,
                VideoName = entity.VideoName,
                VideoDescription = entity.VideoDescription,
                VideoURL = entity.VideoURL,
                NeptuneAreaID = entity.NeptuneAreaID,
                NeptuneAreaDisplayName = entity.NeptuneAreaID.HasValue
                    ? NeptuneArea.AllLookupDictionary[entity.NeptuneAreaID.Value].NeptuneAreaDisplayName
                    : null,
            };
        }
    }
}
