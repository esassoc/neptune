//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[PreliminarySourceIdentificationCategory]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class PreliminarySourceIdentificationCategory : IHavePrimaryKey
    {
        public static readonly PreliminarySourceIdentificationCategoryVehicles Vehicles = PreliminarySourceIdentificationCategoryVehicles.Instance;
        public static readonly PreliminarySourceIdentificationCategoryInadequateWasteContainerManagement InadequateWasteContainerManagement = PreliminarySourceIdentificationCategoryInadequateWasteContainerManagement.Instance;
        public static readonly PreliminarySourceIdentificationCategoryPedestrianLitter PedestrianLitter = PreliminarySourceIdentificationCategoryPedestrianLitter.Instance;
        public static readonly PreliminarySourceIdentificationCategoryIllegalDumping IllegalDumping = PreliminarySourceIdentificationCategoryIllegalDumping.Instance;

        public static readonly List<PreliminarySourceIdentificationCategory> All;
        public static readonly ReadOnlyDictionary<int, PreliminarySourceIdentificationCategory> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static PreliminarySourceIdentificationCategory()
        {
            All = new List<PreliminarySourceIdentificationCategory> { Vehicles, InadequateWasteContainerManagement, PedestrianLitter, IllegalDumping };
            AllLookupDictionary = new ReadOnlyDictionary<int, PreliminarySourceIdentificationCategory>(All.ToDictionary(x => x.PreliminarySourceIdentificationCategoryID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected PreliminarySourceIdentificationCategory(int preliminarySourceIdentificationCategoryID, string preliminarySourceIdentificationCategoryName, string preliminarySourceIdentificationCategoryDisplayName)
        {
            PreliminarySourceIdentificationCategoryID = preliminarySourceIdentificationCategoryID;
            PreliminarySourceIdentificationCategoryName = preliminarySourceIdentificationCategoryName;
            PreliminarySourceIdentificationCategoryDisplayName = preliminarySourceIdentificationCategoryDisplayName;
        }
        public List<PreliminarySourceIdentificationType> PreliminarySourceIdentificationTypes => PreliminarySourceIdentificationType.All.Where(x => x.PreliminarySourceIdentificationCategoryID == PreliminarySourceIdentificationCategoryID).ToList();
        [Key]
        public int PreliminarySourceIdentificationCategoryID { get; private set; }
        public string PreliminarySourceIdentificationCategoryName { get; private set; }
        public string PreliminarySourceIdentificationCategoryDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return PreliminarySourceIdentificationCategoryID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(PreliminarySourceIdentificationCategory other)
        {
            if (other == null)
            {
                return false;
            }
            return other.PreliminarySourceIdentificationCategoryID == PreliminarySourceIdentificationCategoryID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as PreliminarySourceIdentificationCategory);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return PreliminarySourceIdentificationCategoryID;
        }

        public static bool operator ==(PreliminarySourceIdentificationCategory left, PreliminarySourceIdentificationCategory right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PreliminarySourceIdentificationCategory left, PreliminarySourceIdentificationCategory right)
        {
            return !Equals(left, right);
        }

        public PreliminarySourceIdentificationCategoryEnum ToEnum => (PreliminarySourceIdentificationCategoryEnum)GetHashCode();

        public static PreliminarySourceIdentificationCategory ToType(int enumValue)
        {
            return ToType((PreliminarySourceIdentificationCategoryEnum)enumValue);
        }

        public static PreliminarySourceIdentificationCategory ToType(PreliminarySourceIdentificationCategoryEnum enumValue)
        {
            switch (enumValue)
            {
                case PreliminarySourceIdentificationCategoryEnum.IllegalDumping:
                    return IllegalDumping;
                case PreliminarySourceIdentificationCategoryEnum.InadequateWasteContainerManagement:
                    return InadequateWasteContainerManagement;
                case PreliminarySourceIdentificationCategoryEnum.PedestrianLitter:
                    return PedestrianLitter;
                case PreliminarySourceIdentificationCategoryEnum.Vehicles:
                    return Vehicles;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum PreliminarySourceIdentificationCategoryEnum
    {
        Vehicles = 1,
        InadequateWasteContainerManagement = 2,
        PedestrianLitter = 3,
        IllegalDumping = 4
    }

    public partial class PreliminarySourceIdentificationCategoryVehicles : PreliminarySourceIdentificationCategory
    {
        private PreliminarySourceIdentificationCategoryVehicles(int preliminarySourceIdentificationCategoryID, string preliminarySourceIdentificationCategoryName, string preliminarySourceIdentificationCategoryDisplayName) : base(preliminarySourceIdentificationCategoryID, preliminarySourceIdentificationCategoryName, preliminarySourceIdentificationCategoryDisplayName) {}
        public static readonly PreliminarySourceIdentificationCategoryVehicles Instance = new PreliminarySourceIdentificationCategoryVehicles(1, @"Vehicles", @"Vehicles");
    }

    public partial class PreliminarySourceIdentificationCategoryInadequateWasteContainerManagement : PreliminarySourceIdentificationCategory
    {
        private PreliminarySourceIdentificationCategoryInadequateWasteContainerManagement(int preliminarySourceIdentificationCategoryID, string preliminarySourceIdentificationCategoryName, string preliminarySourceIdentificationCategoryDisplayName) : base(preliminarySourceIdentificationCategoryID, preliminarySourceIdentificationCategoryName, preliminarySourceIdentificationCategoryDisplayName) {}
        public static readonly PreliminarySourceIdentificationCategoryInadequateWasteContainerManagement Instance = new PreliminarySourceIdentificationCategoryInadequateWasteContainerManagement(2, @"InadequateWasteContainerManagement", @"Inadequate Waste Container Management");
    }

    public partial class PreliminarySourceIdentificationCategoryPedestrianLitter : PreliminarySourceIdentificationCategory
    {
        private PreliminarySourceIdentificationCategoryPedestrianLitter(int preliminarySourceIdentificationCategoryID, string preliminarySourceIdentificationCategoryName, string preliminarySourceIdentificationCategoryDisplayName) : base(preliminarySourceIdentificationCategoryID, preliminarySourceIdentificationCategoryName, preliminarySourceIdentificationCategoryDisplayName) {}
        public static readonly PreliminarySourceIdentificationCategoryPedestrianLitter Instance = new PreliminarySourceIdentificationCategoryPedestrianLitter(3, @"PedestrianLitter", @"Pedestrian Litter");
    }

    public partial class PreliminarySourceIdentificationCategoryIllegalDumping : PreliminarySourceIdentificationCategory
    {
        private PreliminarySourceIdentificationCategoryIllegalDumping(int preliminarySourceIdentificationCategoryID, string preliminarySourceIdentificationCategoryName, string preliminarySourceIdentificationCategoryDisplayName) : base(preliminarySourceIdentificationCategoryID, preliminarySourceIdentificationCategoryName, preliminarySourceIdentificationCategoryDisplayName) {}
        public static readonly PreliminarySourceIdentificationCategoryIllegalDumping Instance = new PreliminarySourceIdentificationCategoryIllegalDumping(4, @"IllegalDumping", @"Illegal Dumping");
    }
}