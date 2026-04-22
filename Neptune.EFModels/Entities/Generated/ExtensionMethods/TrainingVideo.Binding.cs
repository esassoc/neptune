//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[TrainingVideo]
namespace Neptune.EFModels.Entities
{
    public partial class TrainingVideo : IHavePrimaryKey
    {
        public int PrimaryKey => TrainingVideoID;
        public NeptuneArea? NeptuneArea => NeptuneAreaID.HasValue ? NeptuneArea.AllLookupDictionary[NeptuneAreaID.Value] : null;

        public static class FieldLengths
        {
            public const int VideoName = 100;
            public const int VideoDescription = 500;
            public const int VideoURL = 100;
        }
    }
}