//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[MonthsOfOperation]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class MonthsOfOperation : IHavePrimaryKey
    {
        public static readonly MonthsOfOperationSummer Summer = MonthsOfOperationSummer.Instance;
        public static readonly MonthsOfOperationWinter Winter = MonthsOfOperationWinter.Instance;
        public static readonly MonthsOfOperationBoth Both = MonthsOfOperationBoth.Instance;

        public static readonly List<MonthsOfOperation> All;
        public static readonly ReadOnlyDictionary<int, MonthsOfOperation> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static MonthsOfOperation()
        {
            All = new List<MonthsOfOperation> { Summer, Winter, Both };
            AllLookupDictionary = new ReadOnlyDictionary<int, MonthsOfOperation>(All.ToDictionary(x => x.MonthsOfOperationID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected MonthsOfOperation(int monthsOfOperationID, string monthsOfOperationName, string monthsOfOperationDisplayName, string monthsOfOperationNereidAlias)
        {
            MonthsOfOperationID = monthsOfOperationID;
            MonthsOfOperationName = monthsOfOperationName;
            MonthsOfOperationDisplayName = monthsOfOperationDisplayName;
            MonthsOfOperationNereidAlias = monthsOfOperationNereidAlias;
        }

        [Key]
        public int MonthsOfOperationID { get; private set; }
        public string MonthsOfOperationName { get; private set; }
        public string MonthsOfOperationDisplayName { get; private set; }
        public string MonthsOfOperationNereidAlias { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return MonthsOfOperationID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(MonthsOfOperation other)
        {
            if (other == null)
            {
                return false;
            }
            return other.MonthsOfOperationID == MonthsOfOperationID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as MonthsOfOperation);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return MonthsOfOperationID;
        }

        public static bool operator ==(MonthsOfOperation left, MonthsOfOperation right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MonthsOfOperation left, MonthsOfOperation right)
        {
            return !Equals(left, right);
        }

        public MonthsOfOperationEnum ToEnum => (MonthsOfOperationEnum)GetHashCode();

        public static MonthsOfOperation ToType(int enumValue)
        {
            return ToType((MonthsOfOperationEnum)enumValue);
        }

        public static MonthsOfOperation ToType(MonthsOfOperationEnum enumValue)
        {
            switch (enumValue)
            {
                case MonthsOfOperationEnum.Both:
                    return Both;
                case MonthsOfOperationEnum.Summer:
                    return Summer;
                case MonthsOfOperationEnum.Winter:
                    return Winter;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum MonthsOfOperationEnum
    {
        Summer = 1,
        Winter = 2,
        Both = 3
    }

    public partial class MonthsOfOperationSummer : MonthsOfOperation
    {
        private MonthsOfOperationSummer(int monthsOfOperationID, string monthsOfOperationName, string monthsOfOperationDisplayName, string monthsOfOperationNereidAlias) : base(monthsOfOperationID, monthsOfOperationName, monthsOfOperationDisplayName, monthsOfOperationNereidAlias) {}
        public static readonly MonthsOfOperationSummer Instance = new MonthsOfOperationSummer(1, @"Summer", @"Summer", @"summer");
    }

    public partial class MonthsOfOperationWinter : MonthsOfOperation
    {
        private MonthsOfOperationWinter(int monthsOfOperationID, string monthsOfOperationName, string monthsOfOperationDisplayName, string monthsOfOperationNereidAlias) : base(monthsOfOperationID, monthsOfOperationName, monthsOfOperationDisplayName, monthsOfOperationNereidAlias) {}
        public static readonly MonthsOfOperationWinter Instance = new MonthsOfOperationWinter(2, @"Winter", @"Winter", @"winter");
    }

    public partial class MonthsOfOperationBoth : MonthsOfOperation
    {
        private MonthsOfOperationBoth(int monthsOfOperationID, string monthsOfOperationName, string monthsOfOperationDisplayName, string monthsOfOperationNereidAlias) : base(monthsOfOperationID, monthsOfOperationName, monthsOfOperationDisplayName, monthsOfOperationNereidAlias) {}
        public static readonly MonthsOfOperationBoth Instance = new MonthsOfOperationBoth(3, @"Both", @"Both", @"both");
    }
}