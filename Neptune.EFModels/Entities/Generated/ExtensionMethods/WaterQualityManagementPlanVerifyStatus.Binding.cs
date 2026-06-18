//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[WaterQualityManagementPlanVerifyStatus]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class WaterQualityManagementPlanVerifyStatus : IHavePrimaryKey
    {
        public static readonly WaterQualityManagementPlanVerifyStatusAdequateOAndMofWQMPisVerified AdequateOAndMofWQMPisVerified = WaterQualityManagementPlanVerifyStatusAdequateOAndMofWQMPisVerified.Instance;
        public static readonly WaterQualityManagementPlanVerifyStatusDeficienciesarePresentandFollowupisRequired DeficienciesarePresentandFollowupisRequired = WaterQualityManagementPlanVerifyStatusDeficienciesarePresentandFollowupisRequired.Instance;

        public static readonly List<WaterQualityManagementPlanVerifyStatus> All;
        public static readonly ReadOnlyDictionary<int, WaterQualityManagementPlanVerifyStatus> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static WaterQualityManagementPlanVerifyStatus()
        {
            All = new List<WaterQualityManagementPlanVerifyStatus> { AdequateOAndMofWQMPisVerified, DeficienciesarePresentandFollowupisRequired };
            AllLookupDictionary = new ReadOnlyDictionary<int, WaterQualityManagementPlanVerifyStatus>(All.ToDictionary(x => x.WaterQualityManagementPlanVerifyStatusID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected WaterQualityManagementPlanVerifyStatus(int waterQualityManagementPlanVerifyStatusID, string waterQualityManagementPlanVerifyStatusName, string waterQualityManagementPlanVerifyStatusDisplayName)
        {
            WaterQualityManagementPlanVerifyStatusID = waterQualityManagementPlanVerifyStatusID;
            WaterQualityManagementPlanVerifyStatusName = waterQualityManagementPlanVerifyStatusName;
            WaterQualityManagementPlanVerifyStatusDisplayName = waterQualityManagementPlanVerifyStatusDisplayName;
        }

        [Key]
        public int WaterQualityManagementPlanVerifyStatusID { get; private set; }
        public string WaterQualityManagementPlanVerifyStatusName { get; private set; }
        public string WaterQualityManagementPlanVerifyStatusDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return WaterQualityManagementPlanVerifyStatusID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(WaterQualityManagementPlanVerifyStatus other)
        {
            if (other == null)
            {
                return false;
            }
            return other.WaterQualityManagementPlanVerifyStatusID == WaterQualityManagementPlanVerifyStatusID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as WaterQualityManagementPlanVerifyStatus);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return WaterQualityManagementPlanVerifyStatusID;
        }

        public static bool operator ==(WaterQualityManagementPlanVerifyStatus left, WaterQualityManagementPlanVerifyStatus right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WaterQualityManagementPlanVerifyStatus left, WaterQualityManagementPlanVerifyStatus right)
        {
            return !Equals(left, right);
        }

        public WaterQualityManagementPlanVerifyStatusEnum ToEnum => (WaterQualityManagementPlanVerifyStatusEnum)GetHashCode();

        public static WaterQualityManagementPlanVerifyStatus ToType(int enumValue)
        {
            return ToType((WaterQualityManagementPlanVerifyStatusEnum)enumValue);
        }

        public static WaterQualityManagementPlanVerifyStatus ToType(WaterQualityManagementPlanVerifyStatusEnum enumValue)
        {
            switch (enumValue)
            {
                case WaterQualityManagementPlanVerifyStatusEnum.AdequateOAndMofWQMPisVerified:
                    return AdequateOAndMofWQMPisVerified;
                case WaterQualityManagementPlanVerifyStatusEnum.DeficienciesarePresentandFollowupisRequired:
                    return DeficienciesarePresentandFollowupisRequired;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum WaterQualityManagementPlanVerifyStatusEnum
    {
        AdequateOAndMofWQMPisVerified = 1,
        DeficienciesarePresentandFollowupisRequired = 2
    }

    public partial class WaterQualityManagementPlanVerifyStatusAdequateOAndMofWQMPisVerified : WaterQualityManagementPlanVerifyStatus
    {
        private WaterQualityManagementPlanVerifyStatusAdequateOAndMofWQMPisVerified(int waterQualityManagementPlanVerifyStatusID, string waterQualityManagementPlanVerifyStatusName, string waterQualityManagementPlanVerifyStatusDisplayName) : base(waterQualityManagementPlanVerifyStatusID, waterQualityManagementPlanVerifyStatusName, waterQualityManagementPlanVerifyStatusDisplayName) {}
        public static readonly WaterQualityManagementPlanVerifyStatusAdequateOAndMofWQMPisVerified Instance = new WaterQualityManagementPlanVerifyStatusAdequateOAndMofWQMPisVerified(1, @"Adequate O&M of WQMP is Verified", @"Adequate O&M of WQMP is Verified");
    }

    public partial class WaterQualityManagementPlanVerifyStatusDeficienciesarePresentandFollowupisRequired : WaterQualityManagementPlanVerifyStatus
    {
        private WaterQualityManagementPlanVerifyStatusDeficienciesarePresentandFollowupisRequired(int waterQualityManagementPlanVerifyStatusID, string waterQualityManagementPlanVerifyStatusName, string waterQualityManagementPlanVerifyStatusDisplayName) : base(waterQualityManagementPlanVerifyStatusID, waterQualityManagementPlanVerifyStatusName, waterQualityManagementPlanVerifyStatusDisplayName) {}
        public static readonly WaterQualityManagementPlanVerifyStatusDeficienciesarePresentandFollowupisRequired Instance = new WaterQualityManagementPlanVerifyStatusDeficienciesarePresentandFollowupisRequired(2, @"Deficiencies are Present and Follow-up is Required", @"Deficiencies are Present and Follow-up is Required");
    }
}