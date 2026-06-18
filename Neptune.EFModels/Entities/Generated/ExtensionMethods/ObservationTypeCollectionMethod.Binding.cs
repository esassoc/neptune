//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[ObservationTypeCollectionMethod]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class ObservationTypeCollectionMethod : IHavePrimaryKey
    {
        public static readonly ObservationTypeCollectionMethodDiscreteValue DiscreteValue = ObservationTypeCollectionMethodDiscreteValue.Instance;
        public static readonly ObservationTypeCollectionMethodPassFail PassFail = ObservationTypeCollectionMethodPassFail.Instance;
        public static readonly ObservationTypeCollectionMethodPercentage Percentage = ObservationTypeCollectionMethodPercentage.Instance;

        public static readonly List<ObservationTypeCollectionMethod> All;
        public static readonly ReadOnlyDictionary<int, ObservationTypeCollectionMethod> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static ObservationTypeCollectionMethod()
        {
            All = new List<ObservationTypeCollectionMethod> { DiscreteValue, PassFail, Percentage };
            AllLookupDictionary = new ReadOnlyDictionary<int, ObservationTypeCollectionMethod>(All.ToDictionary(x => x.ObservationTypeCollectionMethodID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected ObservationTypeCollectionMethod(int observationTypeCollectionMethodID, string observationTypeCollectionMethodName, string observationTypeCollectionMethodDisplayName, string observationTypeCollectionMethodDescription)
        {
            ObservationTypeCollectionMethodID = observationTypeCollectionMethodID;
            ObservationTypeCollectionMethodName = observationTypeCollectionMethodName;
            ObservationTypeCollectionMethodDisplayName = observationTypeCollectionMethodDisplayName;
            ObservationTypeCollectionMethodDescription = observationTypeCollectionMethodDescription;
        }
        public List<ObservationTypeSpecification> ObservationTypeSpecifications => ObservationTypeSpecification.All.Where(x => x.ObservationTypeCollectionMethodID == ObservationTypeCollectionMethodID).ToList();
        [Key]
        public int ObservationTypeCollectionMethodID { get; private set; }
        public string ObservationTypeCollectionMethodName { get; private set; }
        public string ObservationTypeCollectionMethodDisplayName { get; private set; }
        public string ObservationTypeCollectionMethodDescription { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return ObservationTypeCollectionMethodID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(ObservationTypeCollectionMethod other)
        {
            if (other == null)
            {
                return false;
            }
            return other.ObservationTypeCollectionMethodID == ObservationTypeCollectionMethodID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as ObservationTypeCollectionMethod);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return ObservationTypeCollectionMethodID;
        }

        public static bool operator ==(ObservationTypeCollectionMethod left, ObservationTypeCollectionMethod right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ObservationTypeCollectionMethod left, ObservationTypeCollectionMethod right)
        {
            return !Equals(left, right);
        }

        public ObservationTypeCollectionMethodEnum ToEnum => (ObservationTypeCollectionMethodEnum)GetHashCode();

        public static ObservationTypeCollectionMethod ToType(int enumValue)
        {
            return ToType((ObservationTypeCollectionMethodEnum)enumValue);
        }

        public static ObservationTypeCollectionMethod ToType(ObservationTypeCollectionMethodEnum enumValue)
        {
            switch (enumValue)
            {
                case ObservationTypeCollectionMethodEnum.DiscreteValue:
                    return DiscreteValue;
                case ObservationTypeCollectionMethodEnum.PassFail:
                    return PassFail;
                case ObservationTypeCollectionMethodEnum.Percentage:
                    return Percentage;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum ObservationTypeCollectionMethodEnum
    {
        DiscreteValue = 1,
        PassFail = 3,
        Percentage = 4
    }

    public partial class ObservationTypeCollectionMethodDiscreteValue : ObservationTypeCollectionMethod
    {
        private ObservationTypeCollectionMethodDiscreteValue(int observationTypeCollectionMethodID, string observationTypeCollectionMethodName, string observationTypeCollectionMethodDisplayName, string observationTypeCollectionMethodDescription) : base(observationTypeCollectionMethodID, observationTypeCollectionMethodName, observationTypeCollectionMethodDisplayName, observationTypeCollectionMethodDescription) {}
        public static readonly ObservationTypeCollectionMethodDiscreteValue Instance = new ObservationTypeCollectionMethodDiscreteValue(1, @"DiscreteValue", @"Discrete Value Observation", @"Observation is measured as one or many discrete values (e.g. time, height).");
    }

    public partial class ObservationTypeCollectionMethodPassFail : ObservationTypeCollectionMethod
    {
        private ObservationTypeCollectionMethodPassFail(int observationTypeCollectionMethodID, string observationTypeCollectionMethodName, string observationTypeCollectionMethodDisplayName, string observationTypeCollectionMethodDescription) : base(observationTypeCollectionMethodID, observationTypeCollectionMethodName, observationTypeCollectionMethodDisplayName, observationTypeCollectionMethodDescription) {}
        public static readonly ObservationTypeCollectionMethodPassFail Instance = new ObservationTypeCollectionMethodPassFail(3, @"PassFail", @"Pass/Fail Observation", @"Observation is recorded as Pass/Fail (e.g. presence of standing water).");
    }

    public partial class ObservationTypeCollectionMethodPercentage : ObservationTypeCollectionMethod
    {
        private ObservationTypeCollectionMethodPercentage(int observationTypeCollectionMethodID, string observationTypeCollectionMethodName, string observationTypeCollectionMethodDisplayName, string observationTypeCollectionMethodDescription) : base(observationTypeCollectionMethodID, observationTypeCollectionMethodName, observationTypeCollectionMethodDisplayName, observationTypeCollectionMethodDescription) {}
        public static readonly ObservationTypeCollectionMethodPercentage Instance = new ObservationTypeCollectionMethodPercentage(4, @"Percentage", @"Percent-based Observation", @"Observation is measured as one or more percent values that total to less than 100% (e.g. percent coverage of key species).");
    }
}