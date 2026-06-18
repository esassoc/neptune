//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[OvtaAreaSourceType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class OvtaAreaSourceType : IHavePrimaryKey
    {
        public static readonly OvtaAreaSourceTypeParcel Parcel = OvtaAreaSourceTypeParcel.Instance;
        public static readonly OvtaAreaSourceTypeLandUseBlock LandUseBlock = OvtaAreaSourceTypeLandUseBlock.Instance;

        public static readonly List<OvtaAreaSourceType> All;
        public static readonly ReadOnlyDictionary<int, OvtaAreaSourceType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static OvtaAreaSourceType()
        {
            All = new List<OvtaAreaSourceType> { Parcel, LandUseBlock };
            AllLookupDictionary = new ReadOnlyDictionary<int, OvtaAreaSourceType>(All.ToDictionary(x => x.OvtaAreaSourceTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected OvtaAreaSourceType(int ovtaAreaSourceTypeID, string ovtaAreaSourceTypeName, string ovtaAreaSourceTypeDisplayName)
        {
            OvtaAreaSourceTypeID = ovtaAreaSourceTypeID;
            OvtaAreaSourceTypeName = ovtaAreaSourceTypeName;
            OvtaAreaSourceTypeDisplayName = ovtaAreaSourceTypeDisplayName;
        }

        [Key]
        public int OvtaAreaSourceTypeID { get; private set; }
        public string OvtaAreaSourceTypeName { get; private set; }
        public string OvtaAreaSourceTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return OvtaAreaSourceTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(OvtaAreaSourceType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.OvtaAreaSourceTypeID == OvtaAreaSourceTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as OvtaAreaSourceType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return OvtaAreaSourceTypeID;
        }

        public static bool operator ==(OvtaAreaSourceType left, OvtaAreaSourceType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OvtaAreaSourceType left, OvtaAreaSourceType right)
        {
            return !Equals(left, right);
        }

        public OvtaAreaSourceTypeEnum ToEnum => (OvtaAreaSourceTypeEnum)GetHashCode();

        public static OvtaAreaSourceType ToType(int enumValue)
        {
            return ToType((OvtaAreaSourceTypeEnum)enumValue);
        }

        public static OvtaAreaSourceType ToType(OvtaAreaSourceTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case OvtaAreaSourceTypeEnum.LandUseBlock:
                    return LandUseBlock;
                case OvtaAreaSourceTypeEnum.Parcel:
                    return Parcel;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum OvtaAreaSourceTypeEnum
    {
        Parcel = 1,
        LandUseBlock = 2
    }

    public partial class OvtaAreaSourceTypeParcel : OvtaAreaSourceType
    {
        private OvtaAreaSourceTypeParcel(int ovtaAreaSourceTypeID, string ovtaAreaSourceTypeName, string ovtaAreaSourceTypeDisplayName) : base(ovtaAreaSourceTypeID, ovtaAreaSourceTypeName, ovtaAreaSourceTypeDisplayName) {}
        public static readonly OvtaAreaSourceTypeParcel Instance = new OvtaAreaSourceTypeParcel(1, @"Parcel", @"Parcels");
    }

    public partial class OvtaAreaSourceTypeLandUseBlock : OvtaAreaSourceType
    {
        private OvtaAreaSourceTypeLandUseBlock(int ovtaAreaSourceTypeID, string ovtaAreaSourceTypeName, string ovtaAreaSourceTypeDisplayName) : base(ovtaAreaSourceTypeID, ovtaAreaSourceTypeName, ovtaAreaSourceTypeDisplayName) {}
        public static readonly OvtaAreaSourceTypeLandUseBlock Instance = new OvtaAreaSourceTypeLandUseBlock(2, @"LandUseBlock", @"Land Use Blocks");
    }
}