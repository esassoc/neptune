//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[FieldVisitStatus]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class FieldVisitStatus : IHavePrimaryKey
    {
        public static readonly FieldVisitStatusInProgress InProgress = FieldVisitStatusInProgress.Instance;
        public static readonly FieldVisitStatusComplete Complete = FieldVisitStatusComplete.Instance;
        public static readonly FieldVisitStatusUnresolved Unresolved = FieldVisitStatusUnresolved.Instance;
        public static readonly FieldVisitStatusReturnedToEdit ReturnedToEdit = FieldVisitStatusReturnedToEdit.Instance;

        public static readonly List<FieldVisitStatus> All;
        public static readonly ReadOnlyDictionary<int, FieldVisitStatus> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static FieldVisitStatus()
        {
            All = new List<FieldVisitStatus> { InProgress, Complete, Unresolved, ReturnedToEdit };
            AllLookupDictionary = new ReadOnlyDictionary<int, FieldVisitStatus>(All.ToDictionary(x => x.FieldVisitStatusID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected FieldVisitStatus(int fieldVisitStatusID, string fieldVisitStatusName, string fieldVisitStatusDisplayName)
        {
            FieldVisitStatusID = fieldVisitStatusID;
            FieldVisitStatusName = fieldVisitStatusName;
            FieldVisitStatusDisplayName = fieldVisitStatusDisplayName;
        }

        [Key]
        public int FieldVisitStatusID { get; private set; }
        public string FieldVisitStatusName { get; private set; }
        public string FieldVisitStatusDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return FieldVisitStatusID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(FieldVisitStatus other)
        {
            if (other == null)
            {
                return false;
            }
            return other.FieldVisitStatusID == FieldVisitStatusID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as FieldVisitStatus);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return FieldVisitStatusID;
        }

        public static bool operator ==(FieldVisitStatus left, FieldVisitStatus right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FieldVisitStatus left, FieldVisitStatus right)
        {
            return !Equals(left, right);
        }

        public FieldVisitStatusEnum ToEnum => (FieldVisitStatusEnum)GetHashCode();

        public static FieldVisitStatus ToType(int enumValue)
        {
            return ToType((FieldVisitStatusEnum)enumValue);
        }

        public static FieldVisitStatus ToType(FieldVisitStatusEnum enumValue)
        {
            switch (enumValue)
            {
                case FieldVisitStatusEnum.Complete:
                    return Complete;
                case FieldVisitStatusEnum.InProgress:
                    return InProgress;
                case FieldVisitStatusEnum.ReturnedToEdit:
                    return ReturnedToEdit;
                case FieldVisitStatusEnum.Unresolved:
                    return Unresolved;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum FieldVisitStatusEnum
    {
        InProgress = 1,
        Complete = 2,
        Unresolved = 3,
        ReturnedToEdit = 4
    }

    public partial class FieldVisitStatusInProgress : FieldVisitStatus
    {
        private FieldVisitStatusInProgress(int fieldVisitStatusID, string fieldVisitStatusName, string fieldVisitStatusDisplayName) : base(fieldVisitStatusID, fieldVisitStatusName, fieldVisitStatusDisplayName) {}
        public static readonly FieldVisitStatusInProgress Instance = new FieldVisitStatusInProgress(1, @"InProgress", @"In Progress");
    }

    public partial class FieldVisitStatusComplete : FieldVisitStatus
    {
        private FieldVisitStatusComplete(int fieldVisitStatusID, string fieldVisitStatusName, string fieldVisitStatusDisplayName) : base(fieldVisitStatusID, fieldVisitStatusName, fieldVisitStatusDisplayName) {}
        public static readonly FieldVisitStatusComplete Instance = new FieldVisitStatusComplete(2, @"Complete", @"Complete");
    }

    public partial class FieldVisitStatusUnresolved : FieldVisitStatus
    {
        private FieldVisitStatusUnresolved(int fieldVisitStatusID, string fieldVisitStatusName, string fieldVisitStatusDisplayName) : base(fieldVisitStatusID, fieldVisitStatusName, fieldVisitStatusDisplayName) {}
        public static readonly FieldVisitStatusUnresolved Instance = new FieldVisitStatusUnresolved(3, @"Unresolved", @"Unresolved");
    }

    public partial class FieldVisitStatusReturnedToEdit : FieldVisitStatus
    {
        private FieldVisitStatusReturnedToEdit(int fieldVisitStatusID, string fieldVisitStatusName, string fieldVisitStatusDisplayName) : base(fieldVisitStatusID, fieldVisitStatusName, fieldVisitStatusDisplayName) {}
        public static readonly FieldVisitStatusReturnedToEdit Instance = new FieldVisitStatusReturnedToEdit(4, @"ReturnedToEdit", @"Returned to Edit");
    }
}