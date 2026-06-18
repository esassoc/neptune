//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[RegionalSubbasinRevisionRequestStatus]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class RegionalSubbasinRevisionRequestStatus : IHavePrimaryKey
    {
        public static readonly RegionalSubbasinRevisionRequestStatusOpen Open = RegionalSubbasinRevisionRequestStatusOpen.Instance;
        public static readonly RegionalSubbasinRevisionRequestStatusClosed Closed = RegionalSubbasinRevisionRequestStatusClosed.Instance;

        public static readonly List<RegionalSubbasinRevisionRequestStatus> All;
        public static readonly ReadOnlyDictionary<int, RegionalSubbasinRevisionRequestStatus> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static RegionalSubbasinRevisionRequestStatus()
        {
            All = new List<RegionalSubbasinRevisionRequestStatus> { Open, Closed };
            AllLookupDictionary = new ReadOnlyDictionary<int, RegionalSubbasinRevisionRequestStatus>(All.ToDictionary(x => x.RegionalSubbasinRevisionRequestStatusID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected RegionalSubbasinRevisionRequestStatus(int regionalSubbasinRevisionRequestStatusID, string regionalSubbasinRevisionRequestStatusName, string regionalSubbasinRevisionRequestStatusDisplayName)
        {
            RegionalSubbasinRevisionRequestStatusID = regionalSubbasinRevisionRequestStatusID;
            RegionalSubbasinRevisionRequestStatusName = regionalSubbasinRevisionRequestStatusName;
            RegionalSubbasinRevisionRequestStatusDisplayName = regionalSubbasinRevisionRequestStatusDisplayName;
        }

        [Key]
        public int RegionalSubbasinRevisionRequestStatusID { get; private set; }
        public string RegionalSubbasinRevisionRequestStatusName { get; private set; }
        public string RegionalSubbasinRevisionRequestStatusDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return RegionalSubbasinRevisionRequestStatusID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(RegionalSubbasinRevisionRequestStatus other)
        {
            if (other == null)
            {
                return false;
            }
            return other.RegionalSubbasinRevisionRequestStatusID == RegionalSubbasinRevisionRequestStatusID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as RegionalSubbasinRevisionRequestStatus);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return RegionalSubbasinRevisionRequestStatusID;
        }

        public static bool operator ==(RegionalSubbasinRevisionRequestStatus left, RegionalSubbasinRevisionRequestStatus right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RegionalSubbasinRevisionRequestStatus left, RegionalSubbasinRevisionRequestStatus right)
        {
            return !Equals(left, right);
        }

        public RegionalSubbasinRevisionRequestStatusEnum ToEnum => (RegionalSubbasinRevisionRequestStatusEnum)GetHashCode();

        public static RegionalSubbasinRevisionRequestStatus ToType(int enumValue)
        {
            return ToType((RegionalSubbasinRevisionRequestStatusEnum)enumValue);
        }

        public static RegionalSubbasinRevisionRequestStatus ToType(RegionalSubbasinRevisionRequestStatusEnum enumValue)
        {
            switch (enumValue)
            {
                case RegionalSubbasinRevisionRequestStatusEnum.Closed:
                    return Closed;
                case RegionalSubbasinRevisionRequestStatusEnum.Open:
                    return Open;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum RegionalSubbasinRevisionRequestStatusEnum
    {
        Open = 1,
        Closed = 2
    }

    public partial class RegionalSubbasinRevisionRequestStatusOpen : RegionalSubbasinRevisionRequestStatus
    {
        private RegionalSubbasinRevisionRequestStatusOpen(int regionalSubbasinRevisionRequestStatusID, string regionalSubbasinRevisionRequestStatusName, string regionalSubbasinRevisionRequestStatusDisplayName) : base(regionalSubbasinRevisionRequestStatusID, regionalSubbasinRevisionRequestStatusName, regionalSubbasinRevisionRequestStatusDisplayName) {}
        public static readonly RegionalSubbasinRevisionRequestStatusOpen Instance = new RegionalSubbasinRevisionRequestStatusOpen(1, @"Open", @"Open");
    }

    public partial class RegionalSubbasinRevisionRequestStatusClosed : RegionalSubbasinRevisionRequestStatus
    {
        private RegionalSubbasinRevisionRequestStatusClosed(int regionalSubbasinRevisionRequestStatusID, string regionalSubbasinRevisionRequestStatusName, string regionalSubbasinRevisionRequestStatusDisplayName) : base(regionalSubbasinRevisionRequestStatusID, regionalSubbasinRevisionRequestStatusName, regionalSubbasinRevisionRequestStatusDisplayName) {}
        public static readonly RegionalSubbasinRevisionRequestStatusClosed Instance = new RegionalSubbasinRevisionRequestStatusClosed(2, @"Closed", @"Closed");
    }
}