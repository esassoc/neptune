//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[SourceControlBMPAttributeCategory]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class SourceControlBMPAttributeCategory : IHavePrimaryKey
    {
        public static readonly SourceControlBMPAttributeCategoryHydrologicSourceControlandSiteDesignBMPs HydrologicSourceControlandSiteDesignBMPs = SourceControlBMPAttributeCategoryHydrologicSourceControlandSiteDesignBMPs.Instance;
        public static readonly SourceControlBMPAttributeCategoryApplicableRoutineNonStructuralSourceControlBMPs ApplicableRoutineNonStructuralSourceControlBMPs = SourceControlBMPAttributeCategoryApplicableRoutineNonStructuralSourceControlBMPs.Instance;
        public static readonly SourceControlBMPAttributeCategoryApplicableRoutineStructuralSourceControlBMPs ApplicableRoutineStructuralSourceControlBMPs = SourceControlBMPAttributeCategoryApplicableRoutineStructuralSourceControlBMPs.Instance;

        public static readonly List<SourceControlBMPAttributeCategory> All;
        public static readonly ReadOnlyDictionary<int, SourceControlBMPAttributeCategory> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static SourceControlBMPAttributeCategory()
        {
            All = new List<SourceControlBMPAttributeCategory> { HydrologicSourceControlandSiteDesignBMPs, ApplicableRoutineNonStructuralSourceControlBMPs, ApplicableRoutineStructuralSourceControlBMPs };
            AllLookupDictionary = new ReadOnlyDictionary<int, SourceControlBMPAttributeCategory>(All.ToDictionary(x => x.SourceControlBMPAttributeCategoryID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected SourceControlBMPAttributeCategory(int sourceControlBMPAttributeCategoryID, string sourceControlBMPAttributeCategoryShortName, string sourceControlBMPAttributeCategoryName)
        {
            SourceControlBMPAttributeCategoryID = sourceControlBMPAttributeCategoryID;
            SourceControlBMPAttributeCategoryShortName = sourceControlBMPAttributeCategoryShortName;
            SourceControlBMPAttributeCategoryName = sourceControlBMPAttributeCategoryName;
        }

        [Key]
        public int SourceControlBMPAttributeCategoryID { get; private set; }
        public string SourceControlBMPAttributeCategoryShortName { get; private set; }
        public string SourceControlBMPAttributeCategoryName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return SourceControlBMPAttributeCategoryID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(SourceControlBMPAttributeCategory other)
        {
            if (other == null)
            {
                return false;
            }
            return other.SourceControlBMPAttributeCategoryID == SourceControlBMPAttributeCategoryID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as SourceControlBMPAttributeCategory);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return SourceControlBMPAttributeCategoryID;
        }

        public static bool operator ==(SourceControlBMPAttributeCategory left, SourceControlBMPAttributeCategory right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SourceControlBMPAttributeCategory left, SourceControlBMPAttributeCategory right)
        {
            return !Equals(left, right);
        }

        public SourceControlBMPAttributeCategoryEnum ToEnum => (SourceControlBMPAttributeCategoryEnum)GetHashCode();

        public static SourceControlBMPAttributeCategory ToType(int enumValue)
        {
            return ToType((SourceControlBMPAttributeCategoryEnum)enumValue);
        }

        public static SourceControlBMPAttributeCategory ToType(SourceControlBMPAttributeCategoryEnum enumValue)
        {
            switch (enumValue)
            {
                case SourceControlBMPAttributeCategoryEnum.ApplicableRoutineNonStructuralSourceControlBMPs:
                    return ApplicableRoutineNonStructuralSourceControlBMPs;
                case SourceControlBMPAttributeCategoryEnum.ApplicableRoutineStructuralSourceControlBMPs:
                    return ApplicableRoutineStructuralSourceControlBMPs;
                case SourceControlBMPAttributeCategoryEnum.HydrologicSourceControlandSiteDesignBMPs:
                    return HydrologicSourceControlandSiteDesignBMPs;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum SourceControlBMPAttributeCategoryEnum
    {
        HydrologicSourceControlandSiteDesignBMPs = 1,
        ApplicableRoutineNonStructuralSourceControlBMPs = 2,
        ApplicableRoutineStructuralSourceControlBMPs = 3
    }

    public partial class SourceControlBMPAttributeCategoryHydrologicSourceControlandSiteDesignBMPs : SourceControlBMPAttributeCategory
    {
        private SourceControlBMPAttributeCategoryHydrologicSourceControlandSiteDesignBMPs(int sourceControlBMPAttributeCategoryID, string sourceControlBMPAttributeCategoryShortName, string sourceControlBMPAttributeCategoryName) : base(sourceControlBMPAttributeCategoryID, sourceControlBMPAttributeCategoryShortName, sourceControlBMPAttributeCategoryName) {}
        public static readonly SourceControlBMPAttributeCategoryHydrologicSourceControlandSiteDesignBMPs Instance = new SourceControlBMPAttributeCategoryHydrologicSourceControlandSiteDesignBMPs(1, @"Site Design", @"Hydrologic Source Control and Site Design BMPs");
    }

    public partial class SourceControlBMPAttributeCategoryApplicableRoutineNonStructuralSourceControlBMPs : SourceControlBMPAttributeCategory
    {
        private SourceControlBMPAttributeCategoryApplicableRoutineNonStructuralSourceControlBMPs(int sourceControlBMPAttributeCategoryID, string sourceControlBMPAttributeCategoryShortName, string sourceControlBMPAttributeCategoryName) : base(sourceControlBMPAttributeCategoryID, sourceControlBMPAttributeCategoryShortName, sourceControlBMPAttributeCategoryName) {}
        public static readonly SourceControlBMPAttributeCategoryApplicableRoutineNonStructuralSourceControlBMPs Instance = new SourceControlBMPAttributeCategoryApplicableRoutineNonStructuralSourceControlBMPs(2, @"Non-Structural", @"Applicable Routine Non-Structural Source Control BMPs");
    }

    public partial class SourceControlBMPAttributeCategoryApplicableRoutineStructuralSourceControlBMPs : SourceControlBMPAttributeCategory
    {
        private SourceControlBMPAttributeCategoryApplicableRoutineStructuralSourceControlBMPs(int sourceControlBMPAttributeCategoryID, string sourceControlBMPAttributeCategoryShortName, string sourceControlBMPAttributeCategoryName) : base(sourceControlBMPAttributeCategoryID, sourceControlBMPAttributeCategoryShortName, sourceControlBMPAttributeCategoryName) {}
        public static readonly SourceControlBMPAttributeCategoryApplicableRoutineStructuralSourceControlBMPs Instance = new SourceControlBMPAttributeCategoryApplicableRoutineStructuralSourceControlBMPs(3, @"Structural", @"Applicable Routine Structural Source Control BMPs");
    }
}