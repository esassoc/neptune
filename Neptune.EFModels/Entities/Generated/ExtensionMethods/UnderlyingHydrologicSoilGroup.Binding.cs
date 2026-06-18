//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[UnderlyingHydrologicSoilGroup]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class UnderlyingHydrologicSoilGroup : IHavePrimaryKey
    {
        public static readonly UnderlyingHydrologicSoilGroupA A = UnderlyingHydrologicSoilGroupA.Instance;
        public static readonly UnderlyingHydrologicSoilGroupB B = UnderlyingHydrologicSoilGroupB.Instance;
        public static readonly UnderlyingHydrologicSoilGroupC C = UnderlyingHydrologicSoilGroupC.Instance;
        public static readonly UnderlyingHydrologicSoilGroupD D = UnderlyingHydrologicSoilGroupD.Instance;
        public static readonly UnderlyingHydrologicSoilGroupLiner Liner = UnderlyingHydrologicSoilGroupLiner.Instance;

        public static readonly List<UnderlyingHydrologicSoilGroup> All;
        public static readonly ReadOnlyDictionary<int, UnderlyingHydrologicSoilGroup> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static UnderlyingHydrologicSoilGroup()
        {
            All = new List<UnderlyingHydrologicSoilGroup> { A, B, C, D, Liner };
            AllLookupDictionary = new ReadOnlyDictionary<int, UnderlyingHydrologicSoilGroup>(All.ToDictionary(x => x.UnderlyingHydrologicSoilGroupID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected UnderlyingHydrologicSoilGroup(int underlyingHydrologicSoilGroupID, string underlyingHydrologicSoilGroupName, string underlyingHydrologicSoilGroupDisplayName)
        {
            UnderlyingHydrologicSoilGroupID = underlyingHydrologicSoilGroupID;
            UnderlyingHydrologicSoilGroupName = underlyingHydrologicSoilGroupName;
            UnderlyingHydrologicSoilGroupDisplayName = underlyingHydrologicSoilGroupDisplayName;
        }

        [Key]
        public int UnderlyingHydrologicSoilGroupID { get; private set; }
        public string UnderlyingHydrologicSoilGroupName { get; private set; }
        public string UnderlyingHydrologicSoilGroupDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return UnderlyingHydrologicSoilGroupID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(UnderlyingHydrologicSoilGroup other)
        {
            if (other == null)
            {
                return false;
            }
            return other.UnderlyingHydrologicSoilGroupID == UnderlyingHydrologicSoilGroupID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as UnderlyingHydrologicSoilGroup);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return UnderlyingHydrologicSoilGroupID;
        }

        public static bool operator ==(UnderlyingHydrologicSoilGroup left, UnderlyingHydrologicSoilGroup right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(UnderlyingHydrologicSoilGroup left, UnderlyingHydrologicSoilGroup right)
        {
            return !Equals(left, right);
        }

        public UnderlyingHydrologicSoilGroupEnum ToEnum => (UnderlyingHydrologicSoilGroupEnum)GetHashCode();

        public static UnderlyingHydrologicSoilGroup ToType(int enumValue)
        {
            return ToType((UnderlyingHydrologicSoilGroupEnum)enumValue);
        }

        public static UnderlyingHydrologicSoilGroup ToType(UnderlyingHydrologicSoilGroupEnum enumValue)
        {
            switch (enumValue)
            {
                case UnderlyingHydrologicSoilGroupEnum.A:
                    return A;
                case UnderlyingHydrologicSoilGroupEnum.B:
                    return B;
                case UnderlyingHydrologicSoilGroupEnum.C:
                    return C;
                case UnderlyingHydrologicSoilGroupEnum.D:
                    return D;
                case UnderlyingHydrologicSoilGroupEnum.Liner:
                    return Liner;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum UnderlyingHydrologicSoilGroupEnum
    {
        A = 1,
        B = 2,
        C = 3,
        D = 4,
        Liner = 5
    }

    public partial class UnderlyingHydrologicSoilGroupA : UnderlyingHydrologicSoilGroup
    {
        private UnderlyingHydrologicSoilGroupA(int underlyingHydrologicSoilGroupID, string underlyingHydrologicSoilGroupName, string underlyingHydrologicSoilGroupDisplayName) : base(underlyingHydrologicSoilGroupID, underlyingHydrologicSoilGroupName, underlyingHydrologicSoilGroupDisplayName) {}
        public static readonly UnderlyingHydrologicSoilGroupA Instance = new UnderlyingHydrologicSoilGroupA(1, @"A", @"A");
    }

    public partial class UnderlyingHydrologicSoilGroupB : UnderlyingHydrologicSoilGroup
    {
        private UnderlyingHydrologicSoilGroupB(int underlyingHydrologicSoilGroupID, string underlyingHydrologicSoilGroupName, string underlyingHydrologicSoilGroupDisplayName) : base(underlyingHydrologicSoilGroupID, underlyingHydrologicSoilGroupName, underlyingHydrologicSoilGroupDisplayName) {}
        public static readonly UnderlyingHydrologicSoilGroupB Instance = new UnderlyingHydrologicSoilGroupB(2, @"B", @"B");
    }

    public partial class UnderlyingHydrologicSoilGroupC : UnderlyingHydrologicSoilGroup
    {
        private UnderlyingHydrologicSoilGroupC(int underlyingHydrologicSoilGroupID, string underlyingHydrologicSoilGroupName, string underlyingHydrologicSoilGroupDisplayName) : base(underlyingHydrologicSoilGroupID, underlyingHydrologicSoilGroupName, underlyingHydrologicSoilGroupDisplayName) {}
        public static readonly UnderlyingHydrologicSoilGroupC Instance = new UnderlyingHydrologicSoilGroupC(3, @"C", @"C");
    }

    public partial class UnderlyingHydrologicSoilGroupD : UnderlyingHydrologicSoilGroup
    {
        private UnderlyingHydrologicSoilGroupD(int underlyingHydrologicSoilGroupID, string underlyingHydrologicSoilGroupName, string underlyingHydrologicSoilGroupDisplayName) : base(underlyingHydrologicSoilGroupID, underlyingHydrologicSoilGroupName, underlyingHydrologicSoilGroupDisplayName) {}
        public static readonly UnderlyingHydrologicSoilGroupD Instance = new UnderlyingHydrologicSoilGroupD(4, @"D", @"D");
    }

    public partial class UnderlyingHydrologicSoilGroupLiner : UnderlyingHydrologicSoilGroup
    {
        private UnderlyingHydrologicSoilGroupLiner(int underlyingHydrologicSoilGroupID, string underlyingHydrologicSoilGroupName, string underlyingHydrologicSoilGroupDisplayName) : base(underlyingHydrologicSoilGroupID, underlyingHydrologicSoilGroupName, underlyingHydrologicSoilGroupDisplayName) {}
        public static readonly UnderlyingHydrologicSoilGroupLiner Instance = new UnderlyingHydrologicSoilGroupLiner(5, @"Liner", @"Liner");
    }
}