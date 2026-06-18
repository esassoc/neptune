//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[WaterQualityManagementPlanVisitStatus]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class WaterQualityManagementPlanVisitStatus : IHavePrimaryKey
    {
        public static readonly WaterQualityManagementPlanVisitStatusInitialAnnualVerification InitialAnnualVerification = WaterQualityManagementPlanVisitStatusInitialAnnualVerification.Instance;
        public static readonly WaterQualityManagementPlanVisitStatusFollowupVerification FollowupVerification = WaterQualityManagementPlanVisitStatusFollowupVerification.Instance;

        public static readonly List<WaterQualityManagementPlanVisitStatus> All;
        public static readonly ReadOnlyDictionary<int, WaterQualityManagementPlanVisitStatus> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static WaterQualityManagementPlanVisitStatus()
        {
            All = new List<WaterQualityManagementPlanVisitStatus> { InitialAnnualVerification, FollowupVerification };
            AllLookupDictionary = new ReadOnlyDictionary<int, WaterQualityManagementPlanVisitStatus>(All.ToDictionary(x => x.WaterQualityManagementPlanVisitStatusID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected WaterQualityManagementPlanVisitStatus(int waterQualityManagementPlanVisitStatusID, string waterQualityManagementPlanVisitStatusName, string waterQualityManagementPlanVisitStatusDisplayName)
        {
            WaterQualityManagementPlanVisitStatusID = waterQualityManagementPlanVisitStatusID;
            WaterQualityManagementPlanVisitStatusName = waterQualityManagementPlanVisitStatusName;
            WaterQualityManagementPlanVisitStatusDisplayName = waterQualityManagementPlanVisitStatusDisplayName;
        }

        [Key]
        public int WaterQualityManagementPlanVisitStatusID { get; private set; }
        public string WaterQualityManagementPlanVisitStatusName { get; private set; }
        public string WaterQualityManagementPlanVisitStatusDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return WaterQualityManagementPlanVisitStatusID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(WaterQualityManagementPlanVisitStatus other)
        {
            if (other == null)
            {
                return false;
            }
            return other.WaterQualityManagementPlanVisitStatusID == WaterQualityManagementPlanVisitStatusID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as WaterQualityManagementPlanVisitStatus);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return WaterQualityManagementPlanVisitStatusID;
        }

        public static bool operator ==(WaterQualityManagementPlanVisitStatus left, WaterQualityManagementPlanVisitStatus right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WaterQualityManagementPlanVisitStatus left, WaterQualityManagementPlanVisitStatus right)
        {
            return !Equals(left, right);
        }

        public WaterQualityManagementPlanVisitStatusEnum ToEnum => (WaterQualityManagementPlanVisitStatusEnum)GetHashCode();

        public static WaterQualityManagementPlanVisitStatus ToType(int enumValue)
        {
            return ToType((WaterQualityManagementPlanVisitStatusEnum)enumValue);
        }

        public static WaterQualityManagementPlanVisitStatus ToType(WaterQualityManagementPlanVisitStatusEnum enumValue)
        {
            switch (enumValue)
            {
                case WaterQualityManagementPlanVisitStatusEnum.FollowupVerification:
                    return FollowupVerification;
                case WaterQualityManagementPlanVisitStatusEnum.InitialAnnualVerification:
                    return InitialAnnualVerification;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum WaterQualityManagementPlanVisitStatusEnum
    {
        InitialAnnualVerification = 1,
        FollowupVerification = 2
    }

    public partial class WaterQualityManagementPlanVisitStatusInitialAnnualVerification : WaterQualityManagementPlanVisitStatus
    {
        private WaterQualityManagementPlanVisitStatusInitialAnnualVerification(int waterQualityManagementPlanVisitStatusID, string waterQualityManagementPlanVisitStatusName, string waterQualityManagementPlanVisitStatusDisplayName) : base(waterQualityManagementPlanVisitStatusID, waterQualityManagementPlanVisitStatusName, waterQualityManagementPlanVisitStatusDisplayName) {}
        public static readonly WaterQualityManagementPlanVisitStatusInitialAnnualVerification Instance = new WaterQualityManagementPlanVisitStatusInitialAnnualVerification(1, @"Initial Annual Verification", @"Initial Annual Verification");
    }

    public partial class WaterQualityManagementPlanVisitStatusFollowupVerification : WaterQualityManagementPlanVisitStatus
    {
        private WaterQualityManagementPlanVisitStatusFollowupVerification(int waterQualityManagementPlanVisitStatusID, string waterQualityManagementPlanVisitStatusName, string waterQualityManagementPlanVisitStatusDisplayName) : base(waterQualityManagementPlanVisitStatusID, waterQualityManagementPlanVisitStatusName, waterQualityManagementPlanVisitStatusDisplayName) {}
        public static readonly WaterQualityManagementPlanVisitStatusFollowupVerification Instance = new WaterQualityManagementPlanVisitStatusFollowupVerification(2, @"Follow-up Verification", @"Follow-up Verification");
    }
}