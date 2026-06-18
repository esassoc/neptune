//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[StormwaterBreadCrumbEntity]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class StormwaterBreadCrumbEntity : IHavePrimaryKey
    {
        public static readonly StormwaterBreadCrumbEntityTreatmentBMP TreatmentBMP = StormwaterBreadCrumbEntityTreatmentBMP.Instance;
        public static readonly StormwaterBreadCrumbEntityJurisdiction Jurisdiction = StormwaterBreadCrumbEntityJurisdiction.Instance;
        public static readonly StormwaterBreadCrumbEntityUsers Users = StormwaterBreadCrumbEntityUsers.Instance;
        public static readonly StormwaterBreadCrumbEntityAssessments Assessments = StormwaterBreadCrumbEntityAssessments.Instance;
        public static readonly StormwaterBreadCrumbEntityFieldVisits FieldVisits = StormwaterBreadCrumbEntityFieldVisits.Instance;
        public static readonly StormwaterBreadCrumbEntityFieldRecords FieldRecords = StormwaterBreadCrumbEntityFieldRecords.Instance;
        public static readonly StormwaterBreadCrumbEntityWaterQualityManagementPlan WaterQualityManagementPlan = StormwaterBreadCrumbEntityWaterQualityManagementPlan.Instance;
        public static readonly StormwaterBreadCrumbEntityParcel Parcel = StormwaterBreadCrumbEntityParcel.Instance;
        public static readonly StormwaterBreadCrumbEntityOnlandVisualTrashAssessment OnlandVisualTrashAssessment = StormwaterBreadCrumbEntityOnlandVisualTrashAssessment.Instance;

        public static readonly List<StormwaterBreadCrumbEntity> All;
        public static readonly ReadOnlyDictionary<int, StormwaterBreadCrumbEntity> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static StormwaterBreadCrumbEntity()
        {
            All = new List<StormwaterBreadCrumbEntity> { TreatmentBMP, Jurisdiction, Users, Assessments, FieldVisits, FieldRecords, WaterQualityManagementPlan, Parcel, OnlandVisualTrashAssessment };
            AllLookupDictionary = new ReadOnlyDictionary<int, StormwaterBreadCrumbEntity>(All.ToDictionary(x => x.StormwaterBreadCrumbEntityID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected StormwaterBreadCrumbEntity(int stormwaterBreadCrumbEntityID, string stormwaterBreadCrumbEntityName, string stormwaterBreadCrumbEntityDisplayName, string glyphIconClass, string colorClass)
        {
            StormwaterBreadCrumbEntityID = stormwaterBreadCrumbEntityID;
            StormwaterBreadCrumbEntityName = stormwaterBreadCrumbEntityName;
            StormwaterBreadCrumbEntityDisplayName = stormwaterBreadCrumbEntityDisplayName;
            GlyphIconClass = glyphIconClass;
            ColorClass = colorClass;
        }

        [Key]
        public int StormwaterBreadCrumbEntityID { get; private set; }
        public string StormwaterBreadCrumbEntityName { get; private set; }
        public string StormwaterBreadCrumbEntityDisplayName { get; private set; }
        public string GlyphIconClass { get; private set; }
        public string ColorClass { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return StormwaterBreadCrumbEntityID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(StormwaterBreadCrumbEntity other)
        {
            if (other == null)
            {
                return false;
            }
            return other.StormwaterBreadCrumbEntityID == StormwaterBreadCrumbEntityID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as StormwaterBreadCrumbEntity);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return StormwaterBreadCrumbEntityID;
        }

        public static bool operator ==(StormwaterBreadCrumbEntity left, StormwaterBreadCrumbEntity right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StormwaterBreadCrumbEntity left, StormwaterBreadCrumbEntity right)
        {
            return !Equals(left, right);
        }

        public StormwaterBreadCrumbEntityEnum ToEnum => (StormwaterBreadCrumbEntityEnum)GetHashCode();

        public static StormwaterBreadCrumbEntity ToType(int enumValue)
        {
            return ToType((StormwaterBreadCrumbEntityEnum)enumValue);
        }

        public static StormwaterBreadCrumbEntity ToType(StormwaterBreadCrumbEntityEnum enumValue)
        {
            switch (enumValue)
            {
                case StormwaterBreadCrumbEntityEnum.Assessments:
                    return Assessments;
                case StormwaterBreadCrumbEntityEnum.FieldRecords:
                    return FieldRecords;
                case StormwaterBreadCrumbEntityEnum.FieldVisits:
                    return FieldVisits;
                case StormwaterBreadCrumbEntityEnum.Jurisdiction:
                    return Jurisdiction;
                case StormwaterBreadCrumbEntityEnum.OnlandVisualTrashAssessment:
                    return OnlandVisualTrashAssessment;
                case StormwaterBreadCrumbEntityEnum.Parcel:
                    return Parcel;
                case StormwaterBreadCrumbEntityEnum.TreatmentBMP:
                    return TreatmentBMP;
                case StormwaterBreadCrumbEntityEnum.Users:
                    return Users;
                case StormwaterBreadCrumbEntityEnum.WaterQualityManagementPlan:
                    return WaterQualityManagementPlan;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum StormwaterBreadCrumbEntityEnum
    {
        TreatmentBMP = 1,
        Jurisdiction = 3,
        Users = 4,
        Assessments = 5,
        FieldVisits = 6,
        FieldRecords = 7,
        WaterQualityManagementPlan = 8,
        Parcel = 9,
        OnlandVisualTrashAssessment = 10
    }

    public partial class StormwaterBreadCrumbEntityTreatmentBMP : StormwaterBreadCrumbEntity
    {
        private StormwaterBreadCrumbEntityTreatmentBMP(int stormwaterBreadCrumbEntityID, string stormwaterBreadCrumbEntityName, string stormwaterBreadCrumbEntityDisplayName, string glyphIconClass, string colorClass) : base(stormwaterBreadCrumbEntityID, stormwaterBreadCrumbEntityName, stormwaterBreadCrumbEntityDisplayName, glyphIconClass, colorClass) {}
        public static readonly StormwaterBreadCrumbEntityTreatmentBMP Instance = new StormwaterBreadCrumbEntityTreatmentBMP(1, @"TreatmentBMP", @"Treatment BMP", @"glyphicon-leaf", @"treatmentBMPColor");
    }

    public partial class StormwaterBreadCrumbEntityJurisdiction : StormwaterBreadCrumbEntity
    {
        private StormwaterBreadCrumbEntityJurisdiction(int stormwaterBreadCrumbEntityID, string stormwaterBreadCrumbEntityName, string stormwaterBreadCrumbEntityDisplayName, string glyphIconClass, string colorClass) : base(stormwaterBreadCrumbEntityID, stormwaterBreadCrumbEntityName, stormwaterBreadCrumbEntityDisplayName, glyphIconClass, colorClass) {}
        public static readonly StormwaterBreadCrumbEntityJurisdiction Instance = new StormwaterBreadCrumbEntityJurisdiction(3, @"Jurisdiction", @"Jurisdiction", @"glyphicon-home", @"jurisdictionColor");
    }

    public partial class StormwaterBreadCrumbEntityUsers : StormwaterBreadCrumbEntity
    {
        private StormwaterBreadCrumbEntityUsers(int stormwaterBreadCrumbEntityID, string stormwaterBreadCrumbEntityName, string stormwaterBreadCrumbEntityDisplayName, string glyphIconClass, string colorClass) : base(stormwaterBreadCrumbEntityID, stormwaterBreadCrumbEntityName, stormwaterBreadCrumbEntityDisplayName, glyphIconClass, colorClass) {}
        public static readonly StormwaterBreadCrumbEntityUsers Instance = new StormwaterBreadCrumbEntityUsers(4, @"Users", @"Users", @"glyphicon-user", @"userColor");
    }

    public partial class StormwaterBreadCrumbEntityAssessments : StormwaterBreadCrumbEntity
    {
        private StormwaterBreadCrumbEntityAssessments(int stormwaterBreadCrumbEntityID, string stormwaterBreadCrumbEntityName, string stormwaterBreadCrumbEntityDisplayName, string glyphIconClass, string colorClass) : base(stormwaterBreadCrumbEntityID, stormwaterBreadCrumbEntityName, stormwaterBreadCrumbEntityDisplayName, glyphIconClass, colorClass) {}
        public static readonly StormwaterBreadCrumbEntityAssessments Instance = new StormwaterBreadCrumbEntityAssessments(5, @"Assessments", @"Assessments", @"glyphicon-pencil", @"registrationColor");
    }

    public partial class StormwaterBreadCrumbEntityFieldVisits : StormwaterBreadCrumbEntity
    {
        private StormwaterBreadCrumbEntityFieldVisits(int stormwaterBreadCrumbEntityID, string stormwaterBreadCrumbEntityName, string stormwaterBreadCrumbEntityDisplayName, string glyphIconClass, string colorClass) : base(stormwaterBreadCrumbEntityID, stormwaterBreadCrumbEntityName, stormwaterBreadCrumbEntityDisplayName, glyphIconClass, colorClass) {}
        public static readonly StormwaterBreadCrumbEntityFieldVisits Instance = new StormwaterBreadCrumbEntityFieldVisits(6, @"FieldVisits", @"Field Visits", @"glyphicon-globe", @"registrationColor");
    }

    public partial class StormwaterBreadCrumbEntityFieldRecords : StormwaterBreadCrumbEntity
    {
        private StormwaterBreadCrumbEntityFieldRecords(int stormwaterBreadCrumbEntityID, string stormwaterBreadCrumbEntityName, string stormwaterBreadCrumbEntityDisplayName, string glyphIconClass, string colorClass) : base(stormwaterBreadCrumbEntityID, stormwaterBreadCrumbEntityName, stormwaterBreadCrumbEntityDisplayName, glyphIconClass, colorClass) {}
        public static readonly StormwaterBreadCrumbEntityFieldRecords Instance = new StormwaterBreadCrumbEntityFieldRecords(7, @"FieldRecords", @"Field Records", @"glyphicon-globe", @"registrationColor");
    }

    public partial class StormwaterBreadCrumbEntityWaterQualityManagementPlan : StormwaterBreadCrumbEntity
    {
        private StormwaterBreadCrumbEntityWaterQualityManagementPlan(int stormwaterBreadCrumbEntityID, string stormwaterBreadCrumbEntityName, string stormwaterBreadCrumbEntityDisplayName, string glyphIconClass, string colorClass) : base(stormwaterBreadCrumbEntityID, stormwaterBreadCrumbEntityName, stormwaterBreadCrumbEntityDisplayName, glyphIconClass, colorClass) {}
        public static readonly StormwaterBreadCrumbEntityWaterQualityManagementPlan Instance = new StormwaterBreadCrumbEntityWaterQualityManagementPlan(8, @"WaterQualityManagementPlan", @"Water Quality Management Plan", @"glyphicon-list-alt", @"waterQualityManagementPlanColor");
    }

    public partial class StormwaterBreadCrumbEntityParcel : StormwaterBreadCrumbEntity
    {
        private StormwaterBreadCrumbEntityParcel(int stormwaterBreadCrumbEntityID, string stormwaterBreadCrumbEntityName, string stormwaterBreadCrumbEntityDisplayName, string glyphIconClass, string colorClass) : base(stormwaterBreadCrumbEntityID, stormwaterBreadCrumbEntityName, stormwaterBreadCrumbEntityDisplayName, glyphIconClass, colorClass) {}
        public static readonly StormwaterBreadCrumbEntityParcel Instance = new StormwaterBreadCrumbEntityParcel(9, @"Parcel", @"Parcel", @"glyphicon-home", @"parcelColor");
    }

    public partial class StormwaterBreadCrumbEntityOnlandVisualTrashAssessment : StormwaterBreadCrumbEntity
    {
        private StormwaterBreadCrumbEntityOnlandVisualTrashAssessment(int stormwaterBreadCrumbEntityID, string stormwaterBreadCrumbEntityName, string stormwaterBreadCrumbEntityDisplayName, string glyphIconClass, string colorClass) : base(stormwaterBreadCrumbEntityID, stormwaterBreadCrumbEntityName, stormwaterBreadCrumbEntityDisplayName, glyphIconClass, colorClass) {}
        public static readonly StormwaterBreadCrumbEntityOnlandVisualTrashAssessment Instance = new StormwaterBreadCrumbEntityOnlandVisualTrashAssessment(10, @"OnlandVisualTrashAssessment", @"Onland Visual Trash Assessment", @"glyphicon-trash", @"onlandVisualTrashAssessmentColor");
    }
}