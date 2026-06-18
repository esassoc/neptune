//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[ObservationThresholdType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class ObservationThresholdType : IHavePrimaryKey
    {
        public static readonly ObservationThresholdTypeSpecificValue SpecificValue = ObservationThresholdTypeSpecificValue.Instance;
        public static readonly ObservationThresholdTypeRelativeToBenchmark RelativeToBenchmark = ObservationThresholdTypeRelativeToBenchmark.Instance;
        public static readonly ObservationThresholdTypeNone None = ObservationThresholdTypeNone.Instance;

        public static readonly List<ObservationThresholdType> All;
        public static readonly ReadOnlyDictionary<int, ObservationThresholdType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static ObservationThresholdType()
        {
            All = new List<ObservationThresholdType> { SpecificValue, RelativeToBenchmark, None };
            AllLookupDictionary = new ReadOnlyDictionary<int, ObservationThresholdType>(All.ToDictionary(x => x.ObservationThresholdTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected ObservationThresholdType(int observationThresholdTypeID, string observationThresholdTypeName, string observationThresholdTypeDisplayName)
        {
            ObservationThresholdTypeID = observationThresholdTypeID;
            ObservationThresholdTypeName = observationThresholdTypeName;
            ObservationThresholdTypeDisplayName = observationThresholdTypeDisplayName;
        }
        public List<ObservationTypeSpecification> ObservationTypeSpecifications => ObservationTypeSpecification.All.Where(x => x.ObservationThresholdTypeID == ObservationThresholdTypeID).ToList();
        [Key]
        public int ObservationThresholdTypeID { get; private set; }
        public string ObservationThresholdTypeName { get; private set; }
        public string ObservationThresholdTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return ObservationThresholdTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(ObservationThresholdType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.ObservationThresholdTypeID == ObservationThresholdTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as ObservationThresholdType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return ObservationThresholdTypeID;
        }

        public static bool operator ==(ObservationThresholdType left, ObservationThresholdType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ObservationThresholdType left, ObservationThresholdType right)
        {
            return !Equals(left, right);
        }

        public ObservationThresholdTypeEnum ToEnum => (ObservationThresholdTypeEnum)GetHashCode();

        public static ObservationThresholdType ToType(int enumValue)
        {
            return ToType((ObservationThresholdTypeEnum)enumValue);
        }

        public static ObservationThresholdType ToType(ObservationThresholdTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case ObservationThresholdTypeEnum.None:
                    return None;
                case ObservationThresholdTypeEnum.RelativeToBenchmark:
                    return RelativeToBenchmark;
                case ObservationThresholdTypeEnum.SpecificValue:
                    return SpecificValue;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum ObservationThresholdTypeEnum
    {
        SpecificValue = 1,
        RelativeToBenchmark = 2,
        None = 3
    }

    public partial class ObservationThresholdTypeSpecificValue : ObservationThresholdType
    {
        private ObservationThresholdTypeSpecificValue(int observationThresholdTypeID, string observationThresholdTypeName, string observationThresholdTypeDisplayName) : base(observationThresholdTypeID, observationThresholdTypeName, observationThresholdTypeDisplayName) {}
        public static readonly ObservationThresholdTypeSpecificValue Instance = new ObservationThresholdTypeSpecificValue(1, @"SpecificValue", @"Threshold is a specific value");
    }

    public partial class ObservationThresholdTypeRelativeToBenchmark : ObservationThresholdType
    {
        private ObservationThresholdTypeRelativeToBenchmark(int observationThresholdTypeID, string observationThresholdTypeName, string observationThresholdTypeDisplayName) : base(observationThresholdTypeID, observationThresholdTypeName, observationThresholdTypeDisplayName) {}
        public static readonly ObservationThresholdTypeRelativeToBenchmark Instance = new ObservationThresholdTypeRelativeToBenchmark(2, @"RelativeToBenchmark", @"Threshold is a relative percent of the benchmark value");
    }

    public partial class ObservationThresholdTypeNone : ObservationThresholdType
    {
        private ObservationThresholdTypeNone(int observationThresholdTypeID, string observationThresholdTypeName, string observationThresholdTypeDisplayName) : base(observationThresholdTypeID, observationThresholdTypeName, observationThresholdTypeDisplayName) {}
        public static readonly ObservationThresholdTypeNone Instance = new ObservationThresholdTypeNone(3, @"None", @"None");
    }
}