//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[FieldVisitType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class FieldVisitType : IHavePrimaryKey
    {
        public static readonly FieldVisitTypeDryWeather DryWeather = FieldVisitTypeDryWeather.Instance;
        public static readonly FieldVisitTypeWetWeather WetWeather = FieldVisitTypeWetWeather.Instance;
        public static readonly FieldVisitTypePostStormAssessment PostStormAssessment = FieldVisitTypePostStormAssessment.Instance;

        public static readonly List<FieldVisitType> All;
        public static readonly ReadOnlyDictionary<int, FieldVisitType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static FieldVisitType()
        {
            All = new List<FieldVisitType> { DryWeather, WetWeather, PostStormAssessment };
            AllLookupDictionary = new ReadOnlyDictionary<int, FieldVisitType>(All.ToDictionary(x => x.FieldVisitTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected FieldVisitType(int fieldVisitTypeID, string fieldVisitTypeName, string fieldVisitTypeDisplayName)
        {
            FieldVisitTypeID = fieldVisitTypeID;
            FieldVisitTypeName = fieldVisitTypeName;
            FieldVisitTypeDisplayName = fieldVisitTypeDisplayName;
        }

        [Key]
        public int FieldVisitTypeID { get; private set; }
        public string FieldVisitTypeName { get; private set; }
        public string FieldVisitTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return FieldVisitTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(FieldVisitType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.FieldVisitTypeID == FieldVisitTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as FieldVisitType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return FieldVisitTypeID;
        }

        public static bool operator ==(FieldVisitType left, FieldVisitType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FieldVisitType left, FieldVisitType right)
        {
            return !Equals(left, right);
        }

        public FieldVisitTypeEnum ToEnum => (FieldVisitTypeEnum)GetHashCode();

        public static FieldVisitType ToType(int enumValue)
        {
            return ToType((FieldVisitTypeEnum)enumValue);
        }

        public static FieldVisitType ToType(FieldVisitTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case FieldVisitTypeEnum.DryWeather:
                    return DryWeather;
                case FieldVisitTypeEnum.PostStormAssessment:
                    return PostStormAssessment;
                case FieldVisitTypeEnum.WetWeather:
                    return WetWeather;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum FieldVisitTypeEnum
    {
        DryWeather = 1,
        WetWeather = 2,
        PostStormAssessment = 3
    }

    public partial class FieldVisitTypeDryWeather : FieldVisitType
    {
        private FieldVisitTypeDryWeather(int fieldVisitTypeID, string fieldVisitTypeName, string fieldVisitTypeDisplayName) : base(fieldVisitTypeID, fieldVisitTypeName, fieldVisitTypeDisplayName) {}
        public static readonly FieldVisitTypeDryWeather Instance = new FieldVisitTypeDryWeather(1, @"DryWeather", @"Dry Weather");
    }

    public partial class FieldVisitTypeWetWeather : FieldVisitType
    {
        private FieldVisitTypeWetWeather(int fieldVisitTypeID, string fieldVisitTypeName, string fieldVisitTypeDisplayName) : base(fieldVisitTypeID, fieldVisitTypeName, fieldVisitTypeDisplayName) {}
        public static readonly FieldVisitTypeWetWeather Instance = new FieldVisitTypeWetWeather(2, @"WetWeather", @"Wet Weather");
    }

    public partial class FieldVisitTypePostStormAssessment : FieldVisitType
    {
        private FieldVisitTypePostStormAssessment(int fieldVisitTypeID, string fieldVisitTypeName, string fieldVisitTypeDisplayName) : base(fieldVisitTypeID, fieldVisitTypeName, fieldVisitTypeDisplayName) {}
        public static readonly FieldVisitTypePostStormAssessment Instance = new FieldVisitTypePostStormAssessment(3, @"PostStormAssessment", @"Post-Storm Assessment");
    }
}