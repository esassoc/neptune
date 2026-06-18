//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[TreatmentBMPAssessmentType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class TreatmentBMPAssessmentType : IHavePrimaryKey
    {
        public static readonly TreatmentBMPAssessmentTypeInitial Initial = TreatmentBMPAssessmentTypeInitial.Instance;
        public static readonly TreatmentBMPAssessmentTypePostMaintenance PostMaintenance = TreatmentBMPAssessmentTypePostMaintenance.Instance;

        public static readonly List<TreatmentBMPAssessmentType> All;
        public static readonly ReadOnlyDictionary<int, TreatmentBMPAssessmentType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static TreatmentBMPAssessmentType()
        {
            All = new List<TreatmentBMPAssessmentType> { Initial, PostMaintenance };
            AllLookupDictionary = new ReadOnlyDictionary<int, TreatmentBMPAssessmentType>(All.ToDictionary(x => x.TreatmentBMPAssessmentTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected TreatmentBMPAssessmentType(int treatmentBMPAssessmentTypeID, string treatmentBMPAssessmentTypeName, string treatmentBMPAssessmentTypeDisplayName)
        {
            TreatmentBMPAssessmentTypeID = treatmentBMPAssessmentTypeID;
            TreatmentBMPAssessmentTypeName = treatmentBMPAssessmentTypeName;
            TreatmentBMPAssessmentTypeDisplayName = treatmentBMPAssessmentTypeDisplayName;
        }

        [Key]
        public int TreatmentBMPAssessmentTypeID { get; private set; }
        public string TreatmentBMPAssessmentTypeName { get; private set; }
        public string TreatmentBMPAssessmentTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return TreatmentBMPAssessmentTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(TreatmentBMPAssessmentType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.TreatmentBMPAssessmentTypeID == TreatmentBMPAssessmentTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as TreatmentBMPAssessmentType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return TreatmentBMPAssessmentTypeID;
        }

        public static bool operator ==(TreatmentBMPAssessmentType left, TreatmentBMPAssessmentType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TreatmentBMPAssessmentType left, TreatmentBMPAssessmentType right)
        {
            return !Equals(left, right);
        }

        public TreatmentBMPAssessmentTypeEnum ToEnum => (TreatmentBMPAssessmentTypeEnum)GetHashCode();

        public static TreatmentBMPAssessmentType ToType(int enumValue)
        {
            return ToType((TreatmentBMPAssessmentTypeEnum)enumValue);
        }

        public static TreatmentBMPAssessmentType ToType(TreatmentBMPAssessmentTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case TreatmentBMPAssessmentTypeEnum.Initial:
                    return Initial;
                case TreatmentBMPAssessmentTypeEnum.PostMaintenance:
                    return PostMaintenance;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum TreatmentBMPAssessmentTypeEnum
    {
        Initial = 1,
        PostMaintenance = 2
    }

    public partial class TreatmentBMPAssessmentTypeInitial : TreatmentBMPAssessmentType
    {
        private TreatmentBMPAssessmentTypeInitial(int treatmentBMPAssessmentTypeID, string treatmentBMPAssessmentTypeName, string treatmentBMPAssessmentTypeDisplayName) : base(treatmentBMPAssessmentTypeID, treatmentBMPAssessmentTypeName, treatmentBMPAssessmentTypeDisplayName) {}
        public static readonly TreatmentBMPAssessmentTypeInitial Instance = new TreatmentBMPAssessmentTypeInitial(1, @"Initial", @"Initial");
    }

    public partial class TreatmentBMPAssessmentTypePostMaintenance : TreatmentBMPAssessmentType
    {
        private TreatmentBMPAssessmentTypePostMaintenance(int treatmentBMPAssessmentTypeID, string treatmentBMPAssessmentTypeName, string treatmentBMPAssessmentTypeDisplayName) : base(treatmentBMPAssessmentTypeID, treatmentBMPAssessmentTypeName, treatmentBMPAssessmentTypeDisplayName) {}
        public static readonly TreatmentBMPAssessmentTypePostMaintenance Instance = new TreatmentBMPAssessmentTypePostMaintenance(2, @"PostMaintenance", @"Post-Maintenance");
    }
}