//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[PriorityLandUseType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class PriorityLandUseType : IHavePrimaryKey
    {
        public static readonly PriorityLandUseTypeCommercial Commercial = PriorityLandUseTypeCommercial.Instance;
        public static readonly PriorityLandUseTypeHighDensityResidential HighDensityResidential = PriorityLandUseTypeHighDensityResidential.Instance;
        public static readonly PriorityLandUseTypeIndustrial Industrial = PriorityLandUseTypeIndustrial.Instance;
        public static readonly PriorityLandUseTypeMixedUrban MixedUrban = PriorityLandUseTypeMixedUrban.Instance;
        public static readonly PriorityLandUseTypeCommercialRetail CommercialRetail = PriorityLandUseTypeCommercialRetail.Instance;
        public static readonly PriorityLandUseTypePublicTransportationStations PublicTransportationStations = PriorityLandUseTypePublicTransportationStations.Instance;
        public static readonly PriorityLandUseTypeALU ALU = PriorityLandUseTypeALU.Instance;

        public static readonly List<PriorityLandUseType> All;
        public static readonly ReadOnlyDictionary<int, PriorityLandUseType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static PriorityLandUseType()
        {
            All = new List<PriorityLandUseType> { Commercial, HighDensityResidential, Industrial, MixedUrban, CommercialRetail, PublicTransportationStations, ALU };
            AllLookupDictionary = new ReadOnlyDictionary<int, PriorityLandUseType>(All.ToDictionary(x => x.PriorityLandUseTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected PriorityLandUseType(int priorityLandUseTypeID, string priorityLandUseTypeName, string priorityLandUseTypeDisplayName, string mapColorHexCode)
        {
            PriorityLandUseTypeID = priorityLandUseTypeID;
            PriorityLandUseTypeName = priorityLandUseTypeName;
            PriorityLandUseTypeDisplayName = priorityLandUseTypeDisplayName;
            MapColorHexCode = mapColorHexCode;
        }

        [Key]
        public int PriorityLandUseTypeID { get; private set; }
        public string PriorityLandUseTypeName { get; private set; }
        public string PriorityLandUseTypeDisplayName { get; private set; }
        public string MapColorHexCode { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return PriorityLandUseTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(PriorityLandUseType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.PriorityLandUseTypeID == PriorityLandUseTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as PriorityLandUseType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return PriorityLandUseTypeID;
        }

        public static bool operator ==(PriorityLandUseType left, PriorityLandUseType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PriorityLandUseType left, PriorityLandUseType right)
        {
            return !Equals(left, right);
        }

        public PriorityLandUseTypeEnum ToEnum => (PriorityLandUseTypeEnum)GetHashCode();

        public static PriorityLandUseType ToType(int enumValue)
        {
            return ToType((PriorityLandUseTypeEnum)enumValue);
        }

        public static PriorityLandUseType ToType(PriorityLandUseTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case PriorityLandUseTypeEnum.ALU:
                    return ALU;
                case PriorityLandUseTypeEnum.Commercial:
                    return Commercial;
                case PriorityLandUseTypeEnum.CommercialRetail:
                    return CommercialRetail;
                case PriorityLandUseTypeEnum.HighDensityResidential:
                    return HighDensityResidential;
                case PriorityLandUseTypeEnum.Industrial:
                    return Industrial;
                case PriorityLandUseTypeEnum.MixedUrban:
                    return MixedUrban;
                case PriorityLandUseTypeEnum.PublicTransportationStations:
                    return PublicTransportationStations;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum PriorityLandUseTypeEnum
    {
        Commercial = 1,
        HighDensityResidential = 2,
        Industrial = 3,
        MixedUrban = 4,
        CommercialRetail = 5,
        PublicTransportationStations = 6,
        ALU = 7
    }

    public partial class PriorityLandUseTypeCommercial : PriorityLandUseType
    {
        private PriorityLandUseTypeCommercial(int priorityLandUseTypeID, string priorityLandUseTypeName, string priorityLandUseTypeDisplayName, string mapColorHexCode) : base(priorityLandUseTypeID, priorityLandUseTypeName, priorityLandUseTypeDisplayName, mapColorHexCode) {}
        public static readonly PriorityLandUseTypeCommercial Instance = new PriorityLandUseTypeCommercial(1, @"Commercial", @"Commercial", @"#c2fbfc");
    }

    public partial class PriorityLandUseTypeHighDensityResidential : PriorityLandUseType
    {
        private PriorityLandUseTypeHighDensityResidential(int priorityLandUseTypeID, string priorityLandUseTypeName, string priorityLandUseTypeDisplayName, string mapColorHexCode) : base(priorityLandUseTypeID, priorityLandUseTypeName, priorityLandUseTypeDisplayName, mapColorHexCode) {}
        public static readonly PriorityLandUseTypeHighDensityResidential Instance = new PriorityLandUseTypeHighDensityResidential(2, @"HighDensityResidential", @"High Density Residential", @"#c0d6fc");
    }

    public partial class PriorityLandUseTypeIndustrial : PriorityLandUseType
    {
        private PriorityLandUseTypeIndustrial(int priorityLandUseTypeID, string priorityLandUseTypeName, string priorityLandUseTypeDisplayName, string mapColorHexCode) : base(priorityLandUseTypeID, priorityLandUseTypeName, priorityLandUseTypeDisplayName, mapColorHexCode) {}
        public static readonly PriorityLandUseTypeIndustrial Instance = new PriorityLandUseTypeIndustrial(3, @"Industrial", @"Industrial", @"#b4fcb3");
    }

    public partial class PriorityLandUseTypeMixedUrban : PriorityLandUseType
    {
        private PriorityLandUseTypeMixedUrban(int priorityLandUseTypeID, string priorityLandUseTypeName, string priorityLandUseTypeDisplayName, string mapColorHexCode) : base(priorityLandUseTypeID, priorityLandUseTypeName, priorityLandUseTypeDisplayName, mapColorHexCode) {}
        public static readonly PriorityLandUseTypeMixedUrban Instance = new PriorityLandUseTypeMixedUrban(4, @"MixedUrban", @"Mixed Urban", @"#fcb6b9");
    }

    public partial class PriorityLandUseTypeCommercialRetail : PriorityLandUseType
    {
        private PriorityLandUseTypeCommercialRetail(int priorityLandUseTypeID, string priorityLandUseTypeName, string priorityLandUseTypeDisplayName, string mapColorHexCode) : base(priorityLandUseTypeID, priorityLandUseTypeName, priorityLandUseTypeDisplayName, mapColorHexCode) {}
        public static readonly PriorityLandUseTypeCommercialRetail Instance = new PriorityLandUseTypeCommercialRetail(5, @"CommercialRetail", @"Commercial-Retail", @"#f2cafc");
    }

    public partial class PriorityLandUseTypePublicTransportationStations : PriorityLandUseType
    {
        private PriorityLandUseTypePublicTransportationStations(int priorityLandUseTypeID, string priorityLandUseTypeName, string priorityLandUseTypeDisplayName, string mapColorHexCode) : base(priorityLandUseTypeID, priorityLandUseTypeName, priorityLandUseTypeDisplayName, mapColorHexCode) {}
        public static readonly PriorityLandUseTypePublicTransportationStations Instance = new PriorityLandUseTypePublicTransportationStations(6, @"Public Transportation Stations", @"Public Transportation Stations", @"#fcd6b6");
    }

    public partial class PriorityLandUseTypeALU : PriorityLandUseType
    {
        private PriorityLandUseTypeALU(int priorityLandUseTypeID, string priorityLandUseTypeName, string priorityLandUseTypeDisplayName, string mapColorHexCode) : base(priorityLandUseTypeID, priorityLandUseTypeName, priorityLandUseTypeDisplayName, mapColorHexCode) {}
        public static readonly PriorityLandUseTypeALU Instance = new PriorityLandUseTypeALU(7, @"ALU", @"ALU", @"#ffffff");
    }
}