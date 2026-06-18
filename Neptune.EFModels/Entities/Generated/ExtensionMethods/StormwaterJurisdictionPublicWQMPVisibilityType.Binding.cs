//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[StormwaterJurisdictionPublicWQMPVisibilityType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class StormwaterJurisdictionPublicWQMPVisibilityType : IHavePrimaryKey
    {
        public static readonly StormwaterJurisdictionPublicWQMPVisibilityTypeActiveAndInactive ActiveAndInactive = StormwaterJurisdictionPublicWQMPVisibilityTypeActiveAndInactive.Instance;
        public static readonly StormwaterJurisdictionPublicWQMPVisibilityTypeActiveOnly ActiveOnly = StormwaterJurisdictionPublicWQMPVisibilityTypeActiveOnly.Instance;
        public static readonly StormwaterJurisdictionPublicWQMPVisibilityTypeNone None = StormwaterJurisdictionPublicWQMPVisibilityTypeNone.Instance;

        public static readonly List<StormwaterJurisdictionPublicWQMPVisibilityType> All;
        public static readonly ReadOnlyDictionary<int, StormwaterJurisdictionPublicWQMPVisibilityType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static StormwaterJurisdictionPublicWQMPVisibilityType()
        {
            All = new List<StormwaterJurisdictionPublicWQMPVisibilityType> { ActiveAndInactive, ActiveOnly, None };
            AllLookupDictionary = new ReadOnlyDictionary<int, StormwaterJurisdictionPublicWQMPVisibilityType>(All.ToDictionary(x => x.StormwaterJurisdictionPublicWQMPVisibilityTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected StormwaterJurisdictionPublicWQMPVisibilityType(int stormwaterJurisdictionPublicWQMPVisibilityTypeID, string stormwaterJurisdictionPublicWQMPVisibilityTypeName, string stormwaterJurisdictionPublicWQMPVisibilityTypeDisplayName)
        {
            StormwaterJurisdictionPublicWQMPVisibilityTypeID = stormwaterJurisdictionPublicWQMPVisibilityTypeID;
            StormwaterJurisdictionPublicWQMPVisibilityTypeName = stormwaterJurisdictionPublicWQMPVisibilityTypeName;
            StormwaterJurisdictionPublicWQMPVisibilityTypeDisplayName = stormwaterJurisdictionPublicWQMPVisibilityTypeDisplayName;
        }

        [Key]
        public int StormwaterJurisdictionPublicWQMPVisibilityTypeID { get; private set; }
        public string StormwaterJurisdictionPublicWQMPVisibilityTypeName { get; private set; }
        public string StormwaterJurisdictionPublicWQMPVisibilityTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return StormwaterJurisdictionPublicWQMPVisibilityTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(StormwaterJurisdictionPublicWQMPVisibilityType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.StormwaterJurisdictionPublicWQMPVisibilityTypeID == StormwaterJurisdictionPublicWQMPVisibilityTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as StormwaterJurisdictionPublicWQMPVisibilityType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return StormwaterJurisdictionPublicWQMPVisibilityTypeID;
        }

        public static bool operator ==(StormwaterJurisdictionPublicWQMPVisibilityType left, StormwaterJurisdictionPublicWQMPVisibilityType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StormwaterJurisdictionPublicWQMPVisibilityType left, StormwaterJurisdictionPublicWQMPVisibilityType right)
        {
            return !Equals(left, right);
        }

        public StormwaterJurisdictionPublicWQMPVisibilityTypeEnum ToEnum => (StormwaterJurisdictionPublicWQMPVisibilityTypeEnum)GetHashCode();

        public static StormwaterJurisdictionPublicWQMPVisibilityType ToType(int enumValue)
        {
            return ToType((StormwaterJurisdictionPublicWQMPVisibilityTypeEnum)enumValue);
        }

        public static StormwaterJurisdictionPublicWQMPVisibilityType ToType(StormwaterJurisdictionPublicWQMPVisibilityTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case StormwaterJurisdictionPublicWQMPVisibilityTypeEnum.ActiveAndInactive:
                    return ActiveAndInactive;
                case StormwaterJurisdictionPublicWQMPVisibilityTypeEnum.ActiveOnly:
                    return ActiveOnly;
                case StormwaterJurisdictionPublicWQMPVisibilityTypeEnum.None:
                    return None;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum StormwaterJurisdictionPublicWQMPVisibilityTypeEnum
    {
        ActiveAndInactive = 1,
        ActiveOnly = 2,
        None = 3
    }

    public partial class StormwaterJurisdictionPublicWQMPVisibilityTypeActiveAndInactive : StormwaterJurisdictionPublicWQMPVisibilityType
    {
        private StormwaterJurisdictionPublicWQMPVisibilityTypeActiveAndInactive(int stormwaterJurisdictionPublicWQMPVisibilityTypeID, string stormwaterJurisdictionPublicWQMPVisibilityTypeName, string stormwaterJurisdictionPublicWQMPVisibilityTypeDisplayName) : base(stormwaterJurisdictionPublicWQMPVisibilityTypeID, stormwaterJurisdictionPublicWQMPVisibilityTypeName, stormwaterJurisdictionPublicWQMPVisibilityTypeDisplayName) {}
        public static readonly StormwaterJurisdictionPublicWQMPVisibilityTypeActiveAndInactive Instance = new StormwaterJurisdictionPublicWQMPVisibilityTypeActiveAndInactive(1, @"ActiveAndInactive", @"Active and Inactive");
    }

    public partial class StormwaterJurisdictionPublicWQMPVisibilityTypeActiveOnly : StormwaterJurisdictionPublicWQMPVisibilityType
    {
        private StormwaterJurisdictionPublicWQMPVisibilityTypeActiveOnly(int stormwaterJurisdictionPublicWQMPVisibilityTypeID, string stormwaterJurisdictionPublicWQMPVisibilityTypeName, string stormwaterJurisdictionPublicWQMPVisibilityTypeDisplayName) : base(stormwaterJurisdictionPublicWQMPVisibilityTypeID, stormwaterJurisdictionPublicWQMPVisibilityTypeName, stormwaterJurisdictionPublicWQMPVisibilityTypeDisplayName) {}
        public static readonly StormwaterJurisdictionPublicWQMPVisibilityTypeActiveOnly Instance = new StormwaterJurisdictionPublicWQMPVisibilityTypeActiveOnly(2, @"ActiveOnly", @"Active Only");
    }

    public partial class StormwaterJurisdictionPublicWQMPVisibilityTypeNone : StormwaterJurisdictionPublicWQMPVisibilityType
    {
        private StormwaterJurisdictionPublicWQMPVisibilityTypeNone(int stormwaterJurisdictionPublicWQMPVisibilityTypeID, string stormwaterJurisdictionPublicWQMPVisibilityTypeName, string stormwaterJurisdictionPublicWQMPVisibilityTypeDisplayName) : base(stormwaterJurisdictionPublicWQMPVisibilityTypeID, stormwaterJurisdictionPublicWQMPVisibilityTypeName, stormwaterJurisdictionPublicWQMPVisibilityTypeDisplayName) {}
        public static readonly StormwaterJurisdictionPublicWQMPVisibilityTypeNone Instance = new StormwaterJurisdictionPublicWQMPVisibilityTypeNone(3, @"None", @"None");
    }
}