//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[StormwaterJurisdictionPublicBMPVisibilityType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class StormwaterJurisdictionPublicBMPVisibilityType : IHavePrimaryKey
    {
        public static readonly StormwaterJurisdictionPublicBMPVisibilityTypeVerifiedOnly VerifiedOnly = StormwaterJurisdictionPublicBMPVisibilityTypeVerifiedOnly.Instance;
        public static readonly StormwaterJurisdictionPublicBMPVisibilityTypeNone None = StormwaterJurisdictionPublicBMPVisibilityTypeNone.Instance;

        public static readonly List<StormwaterJurisdictionPublicBMPVisibilityType> All;
        public static readonly ReadOnlyDictionary<int, StormwaterJurisdictionPublicBMPVisibilityType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static StormwaterJurisdictionPublicBMPVisibilityType()
        {
            All = new List<StormwaterJurisdictionPublicBMPVisibilityType> { VerifiedOnly, None };
            AllLookupDictionary = new ReadOnlyDictionary<int, StormwaterJurisdictionPublicBMPVisibilityType>(All.ToDictionary(x => x.StormwaterJurisdictionPublicBMPVisibilityTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected StormwaterJurisdictionPublicBMPVisibilityType(int stormwaterJurisdictionPublicBMPVisibilityTypeID, string stormwaterJurisdictionPublicBMPVisibilityTypeName, string stormwaterJurisdictionPublicBMPVisibilityTypeDisplayName)
        {
            StormwaterJurisdictionPublicBMPVisibilityTypeID = stormwaterJurisdictionPublicBMPVisibilityTypeID;
            StormwaterJurisdictionPublicBMPVisibilityTypeName = stormwaterJurisdictionPublicBMPVisibilityTypeName;
            StormwaterJurisdictionPublicBMPVisibilityTypeDisplayName = stormwaterJurisdictionPublicBMPVisibilityTypeDisplayName;
        }

        [Key]
        public int StormwaterJurisdictionPublicBMPVisibilityTypeID { get; private set; }
        public string StormwaterJurisdictionPublicBMPVisibilityTypeName { get; private set; }
        public string StormwaterJurisdictionPublicBMPVisibilityTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return StormwaterJurisdictionPublicBMPVisibilityTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(StormwaterJurisdictionPublicBMPVisibilityType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.StormwaterJurisdictionPublicBMPVisibilityTypeID == StormwaterJurisdictionPublicBMPVisibilityTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as StormwaterJurisdictionPublicBMPVisibilityType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return StormwaterJurisdictionPublicBMPVisibilityTypeID;
        }

        public static bool operator ==(StormwaterJurisdictionPublicBMPVisibilityType left, StormwaterJurisdictionPublicBMPVisibilityType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StormwaterJurisdictionPublicBMPVisibilityType left, StormwaterJurisdictionPublicBMPVisibilityType right)
        {
            return !Equals(left, right);
        }

        public StormwaterJurisdictionPublicBMPVisibilityTypeEnum ToEnum => (StormwaterJurisdictionPublicBMPVisibilityTypeEnum)GetHashCode();

        public static StormwaterJurisdictionPublicBMPVisibilityType ToType(int enumValue)
        {
            return ToType((StormwaterJurisdictionPublicBMPVisibilityTypeEnum)enumValue);
        }

        public static StormwaterJurisdictionPublicBMPVisibilityType ToType(StormwaterJurisdictionPublicBMPVisibilityTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case StormwaterJurisdictionPublicBMPVisibilityTypeEnum.None:
                    return None;
                case StormwaterJurisdictionPublicBMPVisibilityTypeEnum.VerifiedOnly:
                    return VerifiedOnly;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum StormwaterJurisdictionPublicBMPVisibilityTypeEnum
    {
        VerifiedOnly = 1,
        None = 2
    }

    public partial class StormwaterJurisdictionPublicBMPVisibilityTypeVerifiedOnly : StormwaterJurisdictionPublicBMPVisibilityType
    {
        private StormwaterJurisdictionPublicBMPVisibilityTypeVerifiedOnly(int stormwaterJurisdictionPublicBMPVisibilityTypeID, string stormwaterJurisdictionPublicBMPVisibilityTypeName, string stormwaterJurisdictionPublicBMPVisibilityTypeDisplayName) : base(stormwaterJurisdictionPublicBMPVisibilityTypeID, stormwaterJurisdictionPublicBMPVisibilityTypeName, stormwaterJurisdictionPublicBMPVisibilityTypeDisplayName) {}
        public static readonly StormwaterJurisdictionPublicBMPVisibilityTypeVerifiedOnly Instance = new StormwaterJurisdictionPublicBMPVisibilityTypeVerifiedOnly(1, @"VerifiedOnly", @"Verified Only");
    }

    public partial class StormwaterJurisdictionPublicBMPVisibilityTypeNone : StormwaterJurisdictionPublicBMPVisibilityType
    {
        private StormwaterJurisdictionPublicBMPVisibilityTypeNone(int stormwaterJurisdictionPublicBMPVisibilityTypeID, string stormwaterJurisdictionPublicBMPVisibilityTypeName, string stormwaterJurisdictionPublicBMPVisibilityTypeDisplayName) : base(stormwaterJurisdictionPublicBMPVisibilityTypeID, stormwaterJurisdictionPublicBMPVisibilityTypeName, stormwaterJurisdictionPublicBMPVisibilityTypeDisplayName) {}
        public static readonly StormwaterJurisdictionPublicBMPVisibilityTypeNone Instance = new StormwaterJurisdictionPublicBMPVisibilityTypeNone(2, @"None", @"None");
    }
}