//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[WaterQualityManagementPlanLandUse]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class WaterQualityManagementPlanLandUse : IHavePrimaryKey
    {
        public static readonly WaterQualityManagementPlanLandUseResidential Residential = WaterQualityManagementPlanLandUseResidential.Instance;
        public static readonly WaterQualityManagementPlanLandUseCommercial Commercial = WaterQualityManagementPlanLandUseCommercial.Instance;
        public static readonly WaterQualityManagementPlanLandUseIndustrial Industrial = WaterQualityManagementPlanLandUseIndustrial.Instance;
        public static readonly WaterQualityManagementPlanLandUseOther Other = WaterQualityManagementPlanLandUseOther.Instance;
        public static readonly WaterQualityManagementPlanLandUseRoad Road = WaterQualityManagementPlanLandUseRoad.Instance;
        public static readonly WaterQualityManagementPlanLandUseFlood Flood = WaterQualityManagementPlanLandUseFlood.Instance;
        public static readonly WaterQualityManagementPlanLandUseMunicipal Municipal = WaterQualityManagementPlanLandUseMunicipal.Instance;
        public static readonly WaterQualityManagementPlanLandUsePark Park = WaterQualityManagementPlanLandUsePark.Instance;
        public static readonly WaterQualityManagementPlanLandUseMixed Mixed = WaterQualityManagementPlanLandUseMixed.Instance;

        public static readonly List<WaterQualityManagementPlanLandUse> All;
        public static readonly ReadOnlyDictionary<int, WaterQualityManagementPlanLandUse> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static WaterQualityManagementPlanLandUse()
        {
            All = new List<WaterQualityManagementPlanLandUse> { Residential, Commercial, Industrial, Other, Road, Flood, Municipal, Park, Mixed };
            AllLookupDictionary = new ReadOnlyDictionary<int, WaterQualityManagementPlanLandUse>(All.ToDictionary(x => x.WaterQualityManagementPlanLandUseID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected WaterQualityManagementPlanLandUse(int waterQualityManagementPlanLandUseID, string waterQualityManagementPlanLandUseName, string waterQualityManagementPlanLandUseDisplayName, int sortOrder)
        {
            WaterQualityManagementPlanLandUseID = waterQualityManagementPlanLandUseID;
            WaterQualityManagementPlanLandUseName = waterQualityManagementPlanLandUseName;
            WaterQualityManagementPlanLandUseDisplayName = waterQualityManagementPlanLandUseDisplayName;
            SortOrder = sortOrder;
        }

        [Key]
        public int WaterQualityManagementPlanLandUseID { get; private set; }
        public string WaterQualityManagementPlanLandUseName { get; private set; }
        public string WaterQualityManagementPlanLandUseDisplayName { get; private set; }
        public int SortOrder { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return WaterQualityManagementPlanLandUseID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(WaterQualityManagementPlanLandUse other)
        {
            if (other == null)
            {
                return false;
            }
            return other.WaterQualityManagementPlanLandUseID == WaterQualityManagementPlanLandUseID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as WaterQualityManagementPlanLandUse);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return WaterQualityManagementPlanLandUseID;
        }

        public static bool operator ==(WaterQualityManagementPlanLandUse left, WaterQualityManagementPlanLandUse right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WaterQualityManagementPlanLandUse left, WaterQualityManagementPlanLandUse right)
        {
            return !Equals(left, right);
        }

        public WaterQualityManagementPlanLandUseEnum ToEnum => (WaterQualityManagementPlanLandUseEnum)GetHashCode();

        public static WaterQualityManagementPlanLandUse ToType(int enumValue)
        {
            return ToType((WaterQualityManagementPlanLandUseEnum)enumValue);
        }

        public static WaterQualityManagementPlanLandUse ToType(WaterQualityManagementPlanLandUseEnum enumValue)
        {
            switch (enumValue)
            {
                case WaterQualityManagementPlanLandUseEnum.Commercial:
                    return Commercial;
                case WaterQualityManagementPlanLandUseEnum.Flood:
                    return Flood;
                case WaterQualityManagementPlanLandUseEnum.Industrial:
                    return Industrial;
                case WaterQualityManagementPlanLandUseEnum.Mixed:
                    return Mixed;
                case WaterQualityManagementPlanLandUseEnum.Municipal:
                    return Municipal;
                case WaterQualityManagementPlanLandUseEnum.Other:
                    return Other;
                case WaterQualityManagementPlanLandUseEnum.Park:
                    return Park;
                case WaterQualityManagementPlanLandUseEnum.Residential:
                    return Residential;
                case WaterQualityManagementPlanLandUseEnum.Road:
                    return Road;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum WaterQualityManagementPlanLandUseEnum
    {
        Residential = 1,
        Commercial = 2,
        Industrial = 3,
        Other = 4,
        Road = 5,
        Flood = 6,
        Municipal = 7,
        Park = 8,
        Mixed = 9
    }

    public partial class WaterQualityManagementPlanLandUseResidential : WaterQualityManagementPlanLandUse
    {
        private WaterQualityManagementPlanLandUseResidential(int waterQualityManagementPlanLandUseID, string waterQualityManagementPlanLandUseName, string waterQualityManagementPlanLandUseDisplayName, int sortOrder) : base(waterQualityManagementPlanLandUseID, waterQualityManagementPlanLandUseName, waterQualityManagementPlanLandUseDisplayName, sortOrder) {}
        public static readonly WaterQualityManagementPlanLandUseResidential Instance = new WaterQualityManagementPlanLandUseResidential(1, @"Residential", @"Residential", 70);
    }

    public partial class WaterQualityManagementPlanLandUseCommercial : WaterQualityManagementPlanLandUse
    {
        private WaterQualityManagementPlanLandUseCommercial(int waterQualityManagementPlanLandUseID, string waterQualityManagementPlanLandUseName, string waterQualityManagementPlanLandUseDisplayName, int sortOrder) : base(waterQualityManagementPlanLandUseID, waterQualityManagementPlanLandUseName, waterQualityManagementPlanLandUseDisplayName, sortOrder) {}
        public static readonly WaterQualityManagementPlanLandUseCommercial Instance = new WaterQualityManagementPlanLandUseCommercial(2, @"Commercial", @"Commercial", 10);
    }

    public partial class WaterQualityManagementPlanLandUseIndustrial : WaterQualityManagementPlanLandUse
    {
        private WaterQualityManagementPlanLandUseIndustrial(int waterQualityManagementPlanLandUseID, string waterQualityManagementPlanLandUseName, string waterQualityManagementPlanLandUseDisplayName, int sortOrder) : base(waterQualityManagementPlanLandUseID, waterQualityManagementPlanLandUseName, waterQualityManagementPlanLandUseDisplayName, sortOrder) {}
        public static readonly WaterQualityManagementPlanLandUseIndustrial Instance = new WaterQualityManagementPlanLandUseIndustrial(3, @"Industrial", @"Industrial", 30);
    }

    public partial class WaterQualityManagementPlanLandUseOther : WaterQualityManagementPlanLandUse
    {
        private WaterQualityManagementPlanLandUseOther(int waterQualityManagementPlanLandUseID, string waterQualityManagementPlanLandUseName, string waterQualityManagementPlanLandUseDisplayName, int sortOrder) : base(waterQualityManagementPlanLandUseID, waterQualityManagementPlanLandUseName, waterQualityManagementPlanLandUseDisplayName, sortOrder) {}
        public static readonly WaterQualityManagementPlanLandUseOther Instance = new WaterQualityManagementPlanLandUseOther(4, @"Other", @"Other", 90);
    }

    public partial class WaterQualityManagementPlanLandUseRoad : WaterQualityManagementPlanLandUse
    {
        private WaterQualityManagementPlanLandUseRoad(int waterQualityManagementPlanLandUseID, string waterQualityManagementPlanLandUseName, string waterQualityManagementPlanLandUseDisplayName, int sortOrder) : base(waterQualityManagementPlanLandUseID, waterQualityManagementPlanLandUseName, waterQualityManagementPlanLandUseDisplayName, sortOrder) {}
        public static readonly WaterQualityManagementPlanLandUseRoad Instance = new WaterQualityManagementPlanLandUseRoad(5, @"Road", @"Road", 80);
    }

    public partial class WaterQualityManagementPlanLandUseFlood : WaterQualityManagementPlanLandUse
    {
        private WaterQualityManagementPlanLandUseFlood(int waterQualityManagementPlanLandUseID, string waterQualityManagementPlanLandUseName, string waterQualityManagementPlanLandUseDisplayName, int sortOrder) : base(waterQualityManagementPlanLandUseID, waterQualityManagementPlanLandUseName, waterQualityManagementPlanLandUseDisplayName, sortOrder) {}
        public static readonly WaterQualityManagementPlanLandUseFlood Instance = new WaterQualityManagementPlanLandUseFlood(6, @"Flood", @"Flood", 20);
    }

    public partial class WaterQualityManagementPlanLandUseMunicipal : WaterQualityManagementPlanLandUse
    {
        private WaterQualityManagementPlanLandUseMunicipal(int waterQualityManagementPlanLandUseID, string waterQualityManagementPlanLandUseName, string waterQualityManagementPlanLandUseDisplayName, int sortOrder) : base(waterQualityManagementPlanLandUseID, waterQualityManagementPlanLandUseName, waterQualityManagementPlanLandUseDisplayName, sortOrder) {}
        public static readonly WaterQualityManagementPlanLandUseMunicipal Instance = new WaterQualityManagementPlanLandUseMunicipal(7, @"Municipal", @"Municipal", 50);
    }

    public partial class WaterQualityManagementPlanLandUsePark : WaterQualityManagementPlanLandUse
    {
        private WaterQualityManagementPlanLandUsePark(int waterQualityManagementPlanLandUseID, string waterQualityManagementPlanLandUseName, string waterQualityManagementPlanLandUseDisplayName, int sortOrder) : base(waterQualityManagementPlanLandUseID, waterQualityManagementPlanLandUseName, waterQualityManagementPlanLandUseDisplayName, sortOrder) {}
        public static readonly WaterQualityManagementPlanLandUsePark Instance = new WaterQualityManagementPlanLandUsePark(8, @"Park", @"Park", 60);
    }

    public partial class WaterQualityManagementPlanLandUseMixed : WaterQualityManagementPlanLandUse
    {
        private WaterQualityManagementPlanLandUseMixed(int waterQualityManagementPlanLandUseID, string waterQualityManagementPlanLandUseName, string waterQualityManagementPlanLandUseDisplayName, int sortOrder) : base(waterQualityManagementPlanLandUseID, waterQualityManagementPlanLandUseName, waterQualityManagementPlanLandUseDisplayName, sortOrder) {}
        public static readonly WaterQualityManagementPlanLandUseMixed Instance = new WaterQualityManagementPlanLandUseMixed(9, @"Mixed", @"Mixed", 40);
    }
}