//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[WaterQualityManagementPlanPermitTerm]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class WaterQualityManagementPlanPermitTerm : IHavePrimaryKey
    {
        public static readonly WaterQualityManagementPlanPermitTermNorthOCFirstTerm1990 NorthOCFirstTerm1990 = WaterQualityManagementPlanPermitTermNorthOCFirstTerm1990.Instance;
        public static readonly WaterQualityManagementPlanPermitTermNorthOCSecondTerm1996 NorthOCSecondTerm1996 = WaterQualityManagementPlanPermitTermNorthOCSecondTerm1996.Instance;
        public static readonly WaterQualityManagementPlanPermitTermNorthOCThirdTerm2002 NorthOCThirdTerm2002 = WaterQualityManagementPlanPermitTermNorthOCThirdTerm2002.Instance;
        public static readonly WaterQualityManagementPlanPermitTermNorthOCFourthTerm2009 NorthOCFourthTerm2009 = WaterQualityManagementPlanPermitTermNorthOCFourthTerm2009.Instance;
        public static readonly WaterQualityManagementPlanPermitTermSouthOCFirstTerm1990 SouthOCFirstTerm1990 = WaterQualityManagementPlanPermitTermSouthOCFirstTerm1990.Instance;
        public static readonly WaterQualityManagementPlanPermitTermSouthOCSecondTerm1996 SouthOCSecondTerm1996 = WaterQualityManagementPlanPermitTermSouthOCSecondTerm1996.Instance;
        public static readonly WaterQualityManagementPlanPermitTermSouthOCThirdTerm2002 SouthOCThirdTerm2002 = WaterQualityManagementPlanPermitTermSouthOCThirdTerm2002.Instance;
        public static readonly WaterQualityManagementPlanPermitTermSouthOCFourthTerm2009 SouthOCFourthTerm2009 = WaterQualityManagementPlanPermitTermSouthOCFourthTerm2009.Instance;
        public static readonly WaterQualityManagementPlanPermitTermSouthOCFithTerm2015 SouthOCFithTerm2015 = WaterQualityManagementPlanPermitTermSouthOCFithTerm2015.Instance;

        public static readonly List<WaterQualityManagementPlanPermitTerm> All;
        public static readonly ReadOnlyDictionary<int, WaterQualityManagementPlanPermitTerm> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static WaterQualityManagementPlanPermitTerm()
        {
            All = new List<WaterQualityManagementPlanPermitTerm> { NorthOCFirstTerm1990, NorthOCSecondTerm1996, NorthOCThirdTerm2002, NorthOCFourthTerm2009, SouthOCFirstTerm1990, SouthOCSecondTerm1996, SouthOCThirdTerm2002, SouthOCFourthTerm2009, SouthOCFithTerm2015 };
            AllLookupDictionary = new ReadOnlyDictionary<int, WaterQualityManagementPlanPermitTerm>(All.ToDictionary(x => x.WaterQualityManagementPlanPermitTermID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected WaterQualityManagementPlanPermitTerm(int waterQualityManagementPlanPermitTermID, string waterQualityManagementPlanPermitTermName, string waterQualityManagementPlanPermitTermDisplayName)
        {
            WaterQualityManagementPlanPermitTermID = waterQualityManagementPlanPermitTermID;
            WaterQualityManagementPlanPermitTermName = waterQualityManagementPlanPermitTermName;
            WaterQualityManagementPlanPermitTermDisplayName = waterQualityManagementPlanPermitTermDisplayName;
        }

        [Key]
        public int WaterQualityManagementPlanPermitTermID { get; private set; }
        public string WaterQualityManagementPlanPermitTermName { get; private set; }
        public string WaterQualityManagementPlanPermitTermDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return WaterQualityManagementPlanPermitTermID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(WaterQualityManagementPlanPermitTerm other)
        {
            if (other == null)
            {
                return false;
            }
            return other.WaterQualityManagementPlanPermitTermID == WaterQualityManagementPlanPermitTermID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as WaterQualityManagementPlanPermitTerm);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return WaterQualityManagementPlanPermitTermID;
        }

        public static bool operator ==(WaterQualityManagementPlanPermitTerm left, WaterQualityManagementPlanPermitTerm right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WaterQualityManagementPlanPermitTerm left, WaterQualityManagementPlanPermitTerm right)
        {
            return !Equals(left, right);
        }

        public WaterQualityManagementPlanPermitTermEnum ToEnum => (WaterQualityManagementPlanPermitTermEnum)GetHashCode();

        public static WaterQualityManagementPlanPermitTerm ToType(int enumValue)
        {
            return ToType((WaterQualityManagementPlanPermitTermEnum)enumValue);
        }

        public static WaterQualityManagementPlanPermitTerm ToType(WaterQualityManagementPlanPermitTermEnum enumValue)
        {
            switch (enumValue)
            {
                case WaterQualityManagementPlanPermitTermEnum.NorthOCFirstTerm1990:
                    return NorthOCFirstTerm1990;
                case WaterQualityManagementPlanPermitTermEnum.NorthOCFourthTerm2009:
                    return NorthOCFourthTerm2009;
                case WaterQualityManagementPlanPermitTermEnum.NorthOCSecondTerm1996:
                    return NorthOCSecondTerm1996;
                case WaterQualityManagementPlanPermitTermEnum.NorthOCThirdTerm2002:
                    return NorthOCThirdTerm2002;
                case WaterQualityManagementPlanPermitTermEnum.SouthOCFirstTerm1990:
                    return SouthOCFirstTerm1990;
                case WaterQualityManagementPlanPermitTermEnum.SouthOCFithTerm2015:
                    return SouthOCFithTerm2015;
                case WaterQualityManagementPlanPermitTermEnum.SouthOCFourthTerm2009:
                    return SouthOCFourthTerm2009;
                case WaterQualityManagementPlanPermitTermEnum.SouthOCSecondTerm1996:
                    return SouthOCSecondTerm1996;
                case WaterQualityManagementPlanPermitTermEnum.SouthOCThirdTerm2002:
                    return SouthOCThirdTerm2002;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum WaterQualityManagementPlanPermitTermEnum
    {
        NorthOCFirstTerm1990 = 1,
        NorthOCSecondTerm1996 = 2,
        NorthOCThirdTerm2002 = 3,
        NorthOCFourthTerm2009 = 4,
        SouthOCFirstTerm1990 = 5,
        SouthOCSecondTerm1996 = 6,
        SouthOCThirdTerm2002 = 7,
        SouthOCFourthTerm2009 = 8,
        SouthOCFithTerm2015 = 9
    }

    public partial class WaterQualityManagementPlanPermitTermNorthOCFirstTerm1990 : WaterQualityManagementPlanPermitTerm
    {
        private WaterQualityManagementPlanPermitTermNorthOCFirstTerm1990(int waterQualityManagementPlanPermitTermID, string waterQualityManagementPlanPermitTermName, string waterQualityManagementPlanPermitTermDisplayName) : base(waterQualityManagementPlanPermitTermID, waterQualityManagementPlanPermitTermName, waterQualityManagementPlanPermitTermDisplayName) {}
        public static readonly WaterQualityManagementPlanPermitTermNorthOCFirstTerm1990 Instance = new WaterQualityManagementPlanPermitTermNorthOCFirstTerm1990(1, @"NorthOCFirstTerm1990", @"North OC 1st Term - 1990");
    }

    public partial class WaterQualityManagementPlanPermitTermNorthOCSecondTerm1996 : WaterQualityManagementPlanPermitTerm
    {
        private WaterQualityManagementPlanPermitTermNorthOCSecondTerm1996(int waterQualityManagementPlanPermitTermID, string waterQualityManagementPlanPermitTermName, string waterQualityManagementPlanPermitTermDisplayName) : base(waterQualityManagementPlanPermitTermID, waterQualityManagementPlanPermitTermName, waterQualityManagementPlanPermitTermDisplayName) {}
        public static readonly WaterQualityManagementPlanPermitTermNorthOCSecondTerm1996 Instance = new WaterQualityManagementPlanPermitTermNorthOCSecondTerm1996(2, @"NorthOCSecondTerm1996", @"North OC 2nd Term - 1996");
    }

    public partial class WaterQualityManagementPlanPermitTermNorthOCThirdTerm2002 : WaterQualityManagementPlanPermitTerm
    {
        private WaterQualityManagementPlanPermitTermNorthOCThirdTerm2002(int waterQualityManagementPlanPermitTermID, string waterQualityManagementPlanPermitTermName, string waterQualityManagementPlanPermitTermDisplayName) : base(waterQualityManagementPlanPermitTermID, waterQualityManagementPlanPermitTermName, waterQualityManagementPlanPermitTermDisplayName) {}
        public static readonly WaterQualityManagementPlanPermitTermNorthOCThirdTerm2002 Instance = new WaterQualityManagementPlanPermitTermNorthOCThirdTerm2002(3, @"NorthOCThirdTerm2002", @"North OC 3rd Term - 2002 (2003 Model WQMP)");
    }

    public partial class WaterQualityManagementPlanPermitTermNorthOCFourthTerm2009 : WaterQualityManagementPlanPermitTerm
    {
        private WaterQualityManagementPlanPermitTermNorthOCFourthTerm2009(int waterQualityManagementPlanPermitTermID, string waterQualityManagementPlanPermitTermName, string waterQualityManagementPlanPermitTermDisplayName) : base(waterQualityManagementPlanPermitTermID, waterQualityManagementPlanPermitTermName, waterQualityManagementPlanPermitTermDisplayName) {}
        public static readonly WaterQualityManagementPlanPermitTermNorthOCFourthTerm2009 Instance = new WaterQualityManagementPlanPermitTermNorthOCFourthTerm2009(4, @"NorthOCFourthTerm2009", @"North OC 4th Term - 2009 (2011 Model WQMP and TGD)");
    }

    public partial class WaterQualityManagementPlanPermitTermSouthOCFirstTerm1990 : WaterQualityManagementPlanPermitTerm
    {
        private WaterQualityManagementPlanPermitTermSouthOCFirstTerm1990(int waterQualityManagementPlanPermitTermID, string waterQualityManagementPlanPermitTermName, string waterQualityManagementPlanPermitTermDisplayName) : base(waterQualityManagementPlanPermitTermID, waterQualityManagementPlanPermitTermName, waterQualityManagementPlanPermitTermDisplayName) {}
        public static readonly WaterQualityManagementPlanPermitTermSouthOCFirstTerm1990 Instance = new WaterQualityManagementPlanPermitTermSouthOCFirstTerm1990(5, @"SouthOCFirstTerm1990", @"South OC 1st Term - 1990");
    }

    public partial class WaterQualityManagementPlanPermitTermSouthOCSecondTerm1996 : WaterQualityManagementPlanPermitTerm
    {
        private WaterQualityManagementPlanPermitTermSouthOCSecondTerm1996(int waterQualityManagementPlanPermitTermID, string waterQualityManagementPlanPermitTermName, string waterQualityManagementPlanPermitTermDisplayName) : base(waterQualityManagementPlanPermitTermID, waterQualityManagementPlanPermitTermName, waterQualityManagementPlanPermitTermDisplayName) {}
        public static readonly WaterQualityManagementPlanPermitTermSouthOCSecondTerm1996 Instance = new WaterQualityManagementPlanPermitTermSouthOCSecondTerm1996(6, @"SouthOCSecondTerm1996", @"South OC 2nd Term - 1996");
    }

    public partial class WaterQualityManagementPlanPermitTermSouthOCThirdTerm2002 : WaterQualityManagementPlanPermitTerm
    {
        private WaterQualityManagementPlanPermitTermSouthOCThirdTerm2002(int waterQualityManagementPlanPermitTermID, string waterQualityManagementPlanPermitTermName, string waterQualityManagementPlanPermitTermDisplayName) : base(waterQualityManagementPlanPermitTermID, waterQualityManagementPlanPermitTermName, waterQualityManagementPlanPermitTermDisplayName) {}
        public static readonly WaterQualityManagementPlanPermitTermSouthOCThirdTerm2002 Instance = new WaterQualityManagementPlanPermitTermSouthOCThirdTerm2002(7, @"SouthOCThirdTerm2002", @"South OC 3rd Term - 2002 (2003 Model WQMP)");
    }

    public partial class WaterQualityManagementPlanPermitTermSouthOCFourthTerm2009 : WaterQualityManagementPlanPermitTerm
    {
        private WaterQualityManagementPlanPermitTermSouthOCFourthTerm2009(int waterQualityManagementPlanPermitTermID, string waterQualityManagementPlanPermitTermName, string waterQualityManagementPlanPermitTermDisplayName) : base(waterQualityManagementPlanPermitTermID, waterQualityManagementPlanPermitTermName, waterQualityManagementPlanPermitTermDisplayName) {}
        public static readonly WaterQualityManagementPlanPermitTermSouthOCFourthTerm2009 Instance = new WaterQualityManagementPlanPermitTermSouthOCFourthTerm2009(8, @"SouthOCFourthTerm2009", @"South OC 4th Term - 2009 (2013 Model WQMP, TGD, and 2012 HMP)");
    }

    public partial class WaterQualityManagementPlanPermitTermSouthOCFithTerm2015 : WaterQualityManagementPlanPermitTerm
    {
        private WaterQualityManagementPlanPermitTermSouthOCFithTerm2015(int waterQualityManagementPlanPermitTermID, string waterQualityManagementPlanPermitTermName, string waterQualityManagementPlanPermitTermDisplayName) : base(waterQualityManagementPlanPermitTermID, waterQualityManagementPlanPermitTermName, waterQualityManagementPlanPermitTermDisplayName) {}
        public static readonly WaterQualityManagementPlanPermitTermSouthOCFithTerm2015 Instance = new WaterQualityManagementPlanPermitTermSouthOCFithTerm2015(9, @"SouthOCFithTerm2015", @"South OC 5th Term - 2015 (2017 Model WQMP, TGD, and HMP)");
    }
}