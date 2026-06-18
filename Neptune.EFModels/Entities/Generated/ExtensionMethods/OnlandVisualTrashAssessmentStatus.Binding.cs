//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[OnlandVisualTrashAssessmentStatus]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class OnlandVisualTrashAssessmentStatus : IHavePrimaryKey
    {
        public static readonly OnlandVisualTrashAssessmentStatusInProgress InProgress = OnlandVisualTrashAssessmentStatusInProgress.Instance;
        public static readonly OnlandVisualTrashAssessmentStatusComplete Complete = OnlandVisualTrashAssessmentStatusComplete.Instance;

        public static readonly List<OnlandVisualTrashAssessmentStatus> All;
        public static readonly ReadOnlyDictionary<int, OnlandVisualTrashAssessmentStatus> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static OnlandVisualTrashAssessmentStatus()
        {
            All = new List<OnlandVisualTrashAssessmentStatus> { InProgress, Complete };
            AllLookupDictionary = new ReadOnlyDictionary<int, OnlandVisualTrashAssessmentStatus>(All.ToDictionary(x => x.OnlandVisualTrashAssessmentStatusID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected OnlandVisualTrashAssessmentStatus(int onlandVisualTrashAssessmentStatusID, string onlandVisualTrashAssessmentStatusName, string onlandVisualTrashAssessmentStatusDisplayName)
        {
            OnlandVisualTrashAssessmentStatusID = onlandVisualTrashAssessmentStatusID;
            OnlandVisualTrashAssessmentStatusName = onlandVisualTrashAssessmentStatusName;
            OnlandVisualTrashAssessmentStatusDisplayName = onlandVisualTrashAssessmentStatusDisplayName;
        }

        [Key]
        public int OnlandVisualTrashAssessmentStatusID { get; private set; }
        public string OnlandVisualTrashAssessmentStatusName { get; private set; }
        public string OnlandVisualTrashAssessmentStatusDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return OnlandVisualTrashAssessmentStatusID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(OnlandVisualTrashAssessmentStatus other)
        {
            if (other == null)
            {
                return false;
            }
            return other.OnlandVisualTrashAssessmentStatusID == OnlandVisualTrashAssessmentStatusID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as OnlandVisualTrashAssessmentStatus);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return OnlandVisualTrashAssessmentStatusID;
        }

        public static bool operator ==(OnlandVisualTrashAssessmentStatus left, OnlandVisualTrashAssessmentStatus right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OnlandVisualTrashAssessmentStatus left, OnlandVisualTrashAssessmentStatus right)
        {
            return !Equals(left, right);
        }

        public OnlandVisualTrashAssessmentStatusEnum ToEnum => (OnlandVisualTrashAssessmentStatusEnum)GetHashCode();

        public static OnlandVisualTrashAssessmentStatus ToType(int enumValue)
        {
            return ToType((OnlandVisualTrashAssessmentStatusEnum)enumValue);
        }

        public static OnlandVisualTrashAssessmentStatus ToType(OnlandVisualTrashAssessmentStatusEnum enumValue)
        {
            switch (enumValue)
            {
                case OnlandVisualTrashAssessmentStatusEnum.Complete:
                    return Complete;
                case OnlandVisualTrashAssessmentStatusEnum.InProgress:
                    return InProgress;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum OnlandVisualTrashAssessmentStatusEnum
    {
        InProgress = 1,
        Complete = 2
    }

    public partial class OnlandVisualTrashAssessmentStatusInProgress : OnlandVisualTrashAssessmentStatus
    {
        private OnlandVisualTrashAssessmentStatusInProgress(int onlandVisualTrashAssessmentStatusID, string onlandVisualTrashAssessmentStatusName, string onlandVisualTrashAssessmentStatusDisplayName) : base(onlandVisualTrashAssessmentStatusID, onlandVisualTrashAssessmentStatusName, onlandVisualTrashAssessmentStatusDisplayName) {}
        public static readonly OnlandVisualTrashAssessmentStatusInProgress Instance = new OnlandVisualTrashAssessmentStatusInProgress(1, @"InProgress", @"In Progress");
    }

    public partial class OnlandVisualTrashAssessmentStatusComplete : OnlandVisualTrashAssessmentStatus
    {
        private OnlandVisualTrashAssessmentStatusComplete(int onlandVisualTrashAssessmentStatusID, string onlandVisualTrashAssessmentStatusName, string onlandVisualTrashAssessmentStatusDisplayName) : base(onlandVisualTrashAssessmentStatusID, onlandVisualTrashAssessmentStatusName, onlandVisualTrashAssessmentStatusDisplayName) {}
        public static readonly OnlandVisualTrashAssessmentStatusComplete Instance = new OnlandVisualTrashAssessmentStatusComplete(2, @"Complete", @"Complete");
    }
}