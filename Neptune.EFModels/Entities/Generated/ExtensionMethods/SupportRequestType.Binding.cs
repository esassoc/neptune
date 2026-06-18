//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[SupportRequestType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class SupportRequestType : IHavePrimaryKey
    {
        public static readonly SupportRequestTypeReportBug ReportBug = SupportRequestTypeReportBug.Instance;
        public static readonly SupportRequestTypeForgotLoginInfo ForgotLoginInfo = SupportRequestTypeForgotLoginInfo.Instance;
        public static readonly SupportRequestTypeNewOrganization NewOrganization = SupportRequestTypeNewOrganization.Instance;
        public static readonly SupportRequestTypeProvideFeedback ProvideFeedback = SupportRequestTypeProvideFeedback.Instance;
        public static readonly SupportRequestTypeRequestOrganizationNameChange RequestOrganizationNameChange = SupportRequestTypeRequestOrganizationNameChange.Instance;
        public static readonly SupportRequestTypeOther Other = SupportRequestTypeOther.Instance;
        public static readonly SupportRequestTypeRequestToChangeUserAccountPrivileges RequestToChangeUserAccountPrivileges = SupportRequestTypeRequestToChangeUserAccountPrivileges.Instance;

        public static readonly List<SupportRequestType> All;
        public static readonly ReadOnlyDictionary<int, SupportRequestType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static SupportRequestType()
        {
            All = new List<SupportRequestType> { ReportBug, ForgotLoginInfo, NewOrganization, ProvideFeedback, RequestOrganizationNameChange, Other, RequestToChangeUserAccountPrivileges };
            AllLookupDictionary = new ReadOnlyDictionary<int, SupportRequestType>(All.ToDictionary(x => x.SupportRequestTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected SupportRequestType(int supportRequestTypeID, string supportRequestTypeName, string supportRequestTypeDisplayName, int supportRequestTypeSortOrder)
        {
            SupportRequestTypeID = supportRequestTypeID;
            SupportRequestTypeName = supportRequestTypeName;
            SupportRequestTypeDisplayName = supportRequestTypeDisplayName;
            SupportRequestTypeSortOrder = supportRequestTypeSortOrder;
        }

        [Key]
        public int SupportRequestTypeID { get; private set; }
        public string SupportRequestTypeName { get; private set; }
        public string SupportRequestTypeDisplayName { get; private set; }
        public int SupportRequestTypeSortOrder { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return SupportRequestTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(SupportRequestType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.SupportRequestTypeID == SupportRequestTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as SupportRequestType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return SupportRequestTypeID;
        }

        public static bool operator ==(SupportRequestType left, SupportRequestType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SupportRequestType left, SupportRequestType right)
        {
            return !Equals(left, right);
        }

        public SupportRequestTypeEnum ToEnum => (SupportRequestTypeEnum)GetHashCode();

        public static SupportRequestType ToType(int enumValue)
        {
            return ToType((SupportRequestTypeEnum)enumValue);
        }

        public static SupportRequestType ToType(SupportRequestTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case SupportRequestTypeEnum.ForgotLoginInfo:
                    return ForgotLoginInfo;
                case SupportRequestTypeEnum.NewOrganization:
                    return NewOrganization;
                case SupportRequestTypeEnum.Other:
                    return Other;
                case SupportRequestTypeEnum.ProvideFeedback:
                    return ProvideFeedback;
                case SupportRequestTypeEnum.ReportBug:
                    return ReportBug;
                case SupportRequestTypeEnum.RequestOrganizationNameChange:
                    return RequestOrganizationNameChange;
                case SupportRequestTypeEnum.RequestToChangeUserAccountPrivileges:
                    return RequestToChangeUserAccountPrivileges;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum SupportRequestTypeEnum
    {
        ReportBug = 1,
        ForgotLoginInfo = 2,
        NewOrganization = 3,
        ProvideFeedback = 4,
        RequestOrganizationNameChange = 5,
        Other = 6,
        RequestToChangeUserAccountPrivileges = 7
    }

    public partial class SupportRequestTypeReportBug : SupportRequestType
    {
        private SupportRequestTypeReportBug(int supportRequestTypeID, string supportRequestTypeName, string supportRequestTypeDisplayName, int supportRequestTypeSortOrder) : base(supportRequestTypeID, supportRequestTypeName, supportRequestTypeDisplayName, supportRequestTypeSortOrder) {}
        public static readonly SupportRequestTypeReportBug Instance = new SupportRequestTypeReportBug(1, @"ReportBug", @"Ran into a bug or problem with this system", 7);
    }

    public partial class SupportRequestTypeForgotLoginInfo : SupportRequestType
    {
        private SupportRequestTypeForgotLoginInfo(int supportRequestTypeID, string supportRequestTypeName, string supportRequestTypeDisplayName, int supportRequestTypeSortOrder) : base(supportRequestTypeID, supportRequestTypeName, supportRequestTypeDisplayName, supportRequestTypeSortOrder) {}
        public static readonly SupportRequestTypeForgotLoginInfo Instance = new SupportRequestTypeForgotLoginInfo(2, @"ForgotLoginInfo", @"Can't log in (forgot my username or password, account is locked, etc.)", 2);
    }

    public partial class SupportRequestTypeNewOrganization : SupportRequestType
    {
        private SupportRequestTypeNewOrganization(int supportRequestTypeID, string supportRequestTypeName, string supportRequestTypeDisplayName, int supportRequestTypeSortOrder) : base(supportRequestTypeID, supportRequestTypeName, supportRequestTypeDisplayName, supportRequestTypeSortOrder) {}
        public static readonly SupportRequestTypeNewOrganization Instance = new SupportRequestTypeNewOrganization(3, @"NewOrganization", @"Need an Organization added to the list", 4);
    }

    public partial class SupportRequestTypeProvideFeedback : SupportRequestType
    {
        private SupportRequestTypeProvideFeedback(int supportRequestTypeID, string supportRequestTypeName, string supportRequestTypeDisplayName, int supportRequestTypeSortOrder) : base(supportRequestTypeID, supportRequestTypeName, supportRequestTypeDisplayName, supportRequestTypeSortOrder) {}
        public static readonly SupportRequestTypeProvideFeedback Instance = new SupportRequestTypeProvideFeedback(4, @"ProvideFeedback", @"Provide Feedback on the site", 6);
    }

    public partial class SupportRequestTypeRequestOrganizationNameChange : SupportRequestType
    {
        private SupportRequestTypeRequestOrganizationNameChange(int supportRequestTypeID, string supportRequestTypeName, string supportRequestTypeDisplayName, int supportRequestTypeSortOrder) : base(supportRequestTypeID, supportRequestTypeName, supportRequestTypeDisplayName, supportRequestTypeSortOrder) {}
        public static readonly SupportRequestTypeRequestOrganizationNameChange Instance = new SupportRequestTypeRequestOrganizationNameChange(5, @"RequestOrganizationNameChange", @"Request a change to an Organization's name", 9);
    }

    public partial class SupportRequestTypeOther : SupportRequestType
    {
        private SupportRequestTypeOther(int supportRequestTypeID, string supportRequestTypeName, string supportRequestTypeDisplayName, int supportRequestTypeSortOrder) : base(supportRequestTypeID, supportRequestTypeName, supportRequestTypeDisplayName, supportRequestTypeSortOrder) {}
        public static readonly SupportRequestTypeOther Instance = new SupportRequestTypeOther(6, @"Other", @"Other", 100);
    }

    public partial class SupportRequestTypeRequestToChangeUserAccountPrivileges : SupportRequestType
    {
        private SupportRequestTypeRequestToChangeUserAccountPrivileges(int supportRequestTypeID, string supportRequestTypeName, string supportRequestTypeDisplayName, int supportRequestTypeSortOrder) : base(supportRequestTypeID, supportRequestTypeName, supportRequestTypeDisplayName, supportRequestTypeSortOrder) {}
        public static readonly SupportRequestTypeRequestToChangeUserAccountPrivileges Instance = new SupportRequestTypeRequestToChangeUserAccountPrivileges(7, @"RequestToChangeUserAccountPrivileges", @"Request to change user account privileges", 10);
    }
}