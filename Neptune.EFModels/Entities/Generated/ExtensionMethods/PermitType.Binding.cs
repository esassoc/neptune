//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[PermitType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class PermitType : IHavePrimaryKey
    {
        public static readonly PermitTypePhaseIMS4 PhaseIMS4 = PermitTypePhaseIMS4.Instance;
        public static readonly PermitTypePhaseIIMS4 PhaseIIMS4 = PermitTypePhaseIIMS4.Instance;
        public static readonly PermitTypeIGP IGP = PermitTypeIGP.Instance;
        public static readonly PermitTypeIndividualPermit IndividualPermit = PermitTypeIndividualPermit.Instance;
        public static readonly PermitTypeCalTransMS4 CalTransMS4 = PermitTypeCalTransMS4.Instance;
        public static readonly PermitTypeOther Other = PermitTypeOther.Instance;

        public static readonly List<PermitType> All;
        public static readonly ReadOnlyDictionary<int, PermitType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static PermitType()
        {
            All = new List<PermitType> { PhaseIMS4, PhaseIIMS4, IGP, IndividualPermit, CalTransMS4, Other };
            AllLookupDictionary = new ReadOnlyDictionary<int, PermitType>(All.ToDictionary(x => x.PermitTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected PermitType(int permitTypeID, string permitTypeName, string permitTypeDisplayName)
        {
            PermitTypeID = permitTypeID;
            PermitTypeName = permitTypeName;
            PermitTypeDisplayName = permitTypeDisplayName;
        }

        [Key]
        public int PermitTypeID { get; private set; }
        public string PermitTypeName { get; private set; }
        public string PermitTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return PermitTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(PermitType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.PermitTypeID == PermitTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as PermitType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return PermitTypeID;
        }

        public static bool operator ==(PermitType left, PermitType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PermitType left, PermitType right)
        {
            return !Equals(left, right);
        }

        public PermitTypeEnum ToEnum => (PermitTypeEnum)GetHashCode();

        public static PermitType ToType(int enumValue)
        {
            return ToType((PermitTypeEnum)enumValue);
        }

        public static PermitType ToType(PermitTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case PermitTypeEnum.CalTransMS4:
                    return CalTransMS4;
                case PermitTypeEnum.IGP:
                    return IGP;
                case PermitTypeEnum.IndividualPermit:
                    return IndividualPermit;
                case PermitTypeEnum.Other:
                    return Other;
                case PermitTypeEnum.PhaseIIMS4:
                    return PhaseIIMS4;
                case PermitTypeEnum.PhaseIMS4:
                    return PhaseIMS4;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum PermitTypeEnum
    {
        PhaseIMS4 = 1,
        PhaseIIMS4 = 2,
        IGP = 3,
        IndividualPermit = 4,
        CalTransMS4 = 5,
        Other = 6
    }

    public partial class PermitTypePhaseIMS4 : PermitType
    {
        private PermitTypePhaseIMS4(int permitTypeID, string permitTypeName, string permitTypeDisplayName) : base(permitTypeID, permitTypeName, permitTypeDisplayName) {}
        public static readonly PermitTypePhaseIMS4 Instance = new PermitTypePhaseIMS4(1, @"PhaseIMS4", @"Phase I MS4");
    }

    public partial class PermitTypePhaseIIMS4 : PermitType
    {
        private PermitTypePhaseIIMS4(int permitTypeID, string permitTypeName, string permitTypeDisplayName) : base(permitTypeID, permitTypeName, permitTypeDisplayName) {}
        public static readonly PermitTypePhaseIIMS4 Instance = new PermitTypePhaseIIMS4(2, @"PhaseIIMS4", @"Phase II MS4");
    }

    public partial class PermitTypeIGP : PermitType
    {
        private PermitTypeIGP(int permitTypeID, string permitTypeName, string permitTypeDisplayName) : base(permitTypeID, permitTypeName, permitTypeDisplayName) {}
        public static readonly PermitTypeIGP Instance = new PermitTypeIGP(3, @"IGP", @"IGP");
    }

    public partial class PermitTypeIndividualPermit : PermitType
    {
        private PermitTypeIndividualPermit(int permitTypeID, string permitTypeName, string permitTypeDisplayName) : base(permitTypeID, permitTypeName, permitTypeDisplayName) {}
        public static readonly PermitTypeIndividualPermit Instance = new PermitTypeIndividualPermit(4, @"IndividualPermit", @"Individual Permit");
    }

    public partial class PermitTypeCalTransMS4 : PermitType
    {
        private PermitTypeCalTransMS4(int permitTypeID, string permitTypeName, string permitTypeDisplayName) : base(permitTypeID, permitTypeName, permitTypeDisplayName) {}
        public static readonly PermitTypeCalTransMS4 Instance = new PermitTypeCalTransMS4(5, @"CalTransMS4", @"CalTrans MS4");
    }

    public partial class PermitTypeOther : PermitType
    {
        private PermitTypeOther(int permitTypeID, string permitTypeName, string permitTypeDisplayName) : base(permitTypeID, permitTypeName, permitTypeDisplayName) {}
        public static readonly PermitTypeOther Instance = new PermitTypeOther(6, @"Other", @"Other");
    }
}