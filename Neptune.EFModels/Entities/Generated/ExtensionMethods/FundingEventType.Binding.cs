//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[FundingEventType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class FundingEventType : IHavePrimaryKey
    {
        public static readonly FundingEventTypePlanningAndDesign PlanningAndDesign = FundingEventTypePlanningAndDesign.Instance;
        public static readonly FundingEventTypeCapitalConstruction CapitalConstruction = FundingEventTypeCapitalConstruction.Instance;
        public static readonly FundingEventTypeRoutineMaintenance RoutineMaintenance = FundingEventTypeRoutineMaintenance.Instance;
        public static readonly FundingEventTypeRehabilitativeMaintenance RehabilitativeMaintenance = FundingEventTypeRehabilitativeMaintenance.Instance;
        public static readonly FundingEventTypeRetrofit Retrofit = FundingEventTypeRetrofit.Instance;

        public static readonly List<FundingEventType> All;
        public static readonly ReadOnlyDictionary<int, FundingEventType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static FundingEventType()
        {
            All = new List<FundingEventType> { PlanningAndDesign, CapitalConstruction, RoutineMaintenance, RehabilitativeMaintenance, Retrofit };
            AllLookupDictionary = new ReadOnlyDictionary<int, FundingEventType>(All.ToDictionary(x => x.FundingEventTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected FundingEventType(int fundingEventTypeID, string fundingEventTypeName, string fundingEventTypeDisplayName)
        {
            FundingEventTypeID = fundingEventTypeID;
            FundingEventTypeName = fundingEventTypeName;
            FundingEventTypeDisplayName = fundingEventTypeDisplayName;
        }

        [Key]
        public int FundingEventTypeID { get; private set; }
        public string FundingEventTypeName { get; private set; }
        public string FundingEventTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return FundingEventTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(FundingEventType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.FundingEventTypeID == FundingEventTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as FundingEventType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return FundingEventTypeID;
        }

        public static bool operator ==(FundingEventType left, FundingEventType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FundingEventType left, FundingEventType right)
        {
            return !Equals(left, right);
        }

        public FundingEventTypeEnum ToEnum => (FundingEventTypeEnum)GetHashCode();

        public static FundingEventType ToType(int enumValue)
        {
            return ToType((FundingEventTypeEnum)enumValue);
        }

        public static FundingEventType ToType(FundingEventTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case FundingEventTypeEnum.CapitalConstruction:
                    return CapitalConstruction;
                case FundingEventTypeEnum.PlanningAndDesign:
                    return PlanningAndDesign;
                case FundingEventTypeEnum.RehabilitativeMaintenance:
                    return RehabilitativeMaintenance;
                case FundingEventTypeEnum.Retrofit:
                    return Retrofit;
                case FundingEventTypeEnum.RoutineMaintenance:
                    return RoutineMaintenance;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum FundingEventTypeEnum
    {
        PlanningAndDesign = 1,
        CapitalConstruction = 2,
        RoutineMaintenance = 3,
        RehabilitativeMaintenance = 4,
        Retrofit = 5
    }

    public partial class FundingEventTypePlanningAndDesign : FundingEventType
    {
        private FundingEventTypePlanningAndDesign(int fundingEventTypeID, string fundingEventTypeName, string fundingEventTypeDisplayName) : base(fundingEventTypeID, fundingEventTypeName, fundingEventTypeDisplayName) {}
        public static readonly FundingEventTypePlanningAndDesign Instance = new FundingEventTypePlanningAndDesign(1, @"PlanningAndDesign", @"Planning & Design");
    }

    public partial class FundingEventTypeCapitalConstruction : FundingEventType
    {
        private FundingEventTypeCapitalConstruction(int fundingEventTypeID, string fundingEventTypeName, string fundingEventTypeDisplayName) : base(fundingEventTypeID, fundingEventTypeName, fundingEventTypeDisplayName) {}
        public static readonly FundingEventTypeCapitalConstruction Instance = new FundingEventTypeCapitalConstruction(2, @"CapitalConstruction", @"Capital Construction");
    }

    public partial class FundingEventTypeRoutineMaintenance : FundingEventType
    {
        private FundingEventTypeRoutineMaintenance(int fundingEventTypeID, string fundingEventTypeName, string fundingEventTypeDisplayName) : base(fundingEventTypeID, fundingEventTypeName, fundingEventTypeDisplayName) {}
        public static readonly FundingEventTypeRoutineMaintenance Instance = new FundingEventTypeRoutineMaintenance(3, @"RoutineMaintenance", @"Routine Assessment and Maintenance");
    }

    public partial class FundingEventTypeRehabilitativeMaintenance : FundingEventType
    {
        private FundingEventTypeRehabilitativeMaintenance(int fundingEventTypeID, string fundingEventTypeName, string fundingEventTypeDisplayName) : base(fundingEventTypeID, fundingEventTypeName, fundingEventTypeDisplayName) {}
        public static readonly FundingEventTypeRehabilitativeMaintenance Instance = new FundingEventTypeRehabilitativeMaintenance(4, @"RehabilitativeMaintenance", @"Rehabilitative Maintenance");
    }

    public partial class FundingEventTypeRetrofit : FundingEventType
    {
        private FundingEventTypeRetrofit(int fundingEventTypeID, string fundingEventTypeName, string fundingEventTypeDisplayName) : base(fundingEventTypeID, fundingEventTypeName, fundingEventTypeDisplayName) {}
        public static readonly FundingEventTypeRetrofit Instance = new FundingEventTypeRetrofit(5, @"Retrofit", @"Retrofit");
    }
}