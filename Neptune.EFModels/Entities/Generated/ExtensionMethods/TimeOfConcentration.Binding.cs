//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[TimeOfConcentration]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class TimeOfConcentration : IHavePrimaryKey
    {
        public static readonly TimeOfConcentrationFiveMinutes FiveMinutes = TimeOfConcentrationFiveMinutes.Instance;
        public static readonly TimeOfConcentrationTenMinutes TenMinutes = TimeOfConcentrationTenMinutes.Instance;
        public static readonly TimeOfConcentrationFifteenMinutes FifteenMinutes = TimeOfConcentrationFifteenMinutes.Instance;
        public static readonly TimeOfConcentrationTwentyMinutes TwentyMinutes = TimeOfConcentrationTwentyMinutes.Instance;
        public static readonly TimeOfConcentrationThirtyMinutes ThirtyMinutes = TimeOfConcentrationThirtyMinutes.Instance;
        public static readonly TimeOfConcentrationFortyFiveMinutes FortyFiveMinutes = TimeOfConcentrationFortyFiveMinutes.Instance;
        public static readonly TimeOfConcentrationSixtyMinutes SixtyMinutes = TimeOfConcentrationSixtyMinutes.Instance;

        public static readonly List<TimeOfConcentration> All;
        public static readonly ReadOnlyDictionary<int, TimeOfConcentration> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static TimeOfConcentration()
        {
            All = new List<TimeOfConcentration> { FiveMinutes, TenMinutes, FifteenMinutes, TwentyMinutes, ThirtyMinutes, FortyFiveMinutes, SixtyMinutes };
            AllLookupDictionary = new ReadOnlyDictionary<int, TimeOfConcentration>(All.ToDictionary(x => x.TimeOfConcentrationID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected TimeOfConcentration(int timeOfConcentrationID, string timeOfConcentrationName, string timeOfConcentrationDisplayName)
        {
            TimeOfConcentrationID = timeOfConcentrationID;
            TimeOfConcentrationName = timeOfConcentrationName;
            TimeOfConcentrationDisplayName = timeOfConcentrationDisplayName;
        }

        [Key]
        public int TimeOfConcentrationID { get; private set; }
        public string TimeOfConcentrationName { get; private set; }
        public string TimeOfConcentrationDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return TimeOfConcentrationID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(TimeOfConcentration other)
        {
            if (other == null)
            {
                return false;
            }
            return other.TimeOfConcentrationID == TimeOfConcentrationID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as TimeOfConcentration);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return TimeOfConcentrationID;
        }

        public static bool operator ==(TimeOfConcentration left, TimeOfConcentration right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TimeOfConcentration left, TimeOfConcentration right)
        {
            return !Equals(left, right);
        }

        public TimeOfConcentrationEnum ToEnum => (TimeOfConcentrationEnum)GetHashCode();

        public static TimeOfConcentration ToType(int enumValue)
        {
            return ToType((TimeOfConcentrationEnum)enumValue);
        }

        public static TimeOfConcentration ToType(TimeOfConcentrationEnum enumValue)
        {
            switch (enumValue)
            {
                case TimeOfConcentrationEnum.FifteenMinutes:
                    return FifteenMinutes;
                case TimeOfConcentrationEnum.FiveMinutes:
                    return FiveMinutes;
                case TimeOfConcentrationEnum.FortyFiveMinutes:
                    return FortyFiveMinutes;
                case TimeOfConcentrationEnum.SixtyMinutes:
                    return SixtyMinutes;
                case TimeOfConcentrationEnum.TenMinutes:
                    return TenMinutes;
                case TimeOfConcentrationEnum.ThirtyMinutes:
                    return ThirtyMinutes;
                case TimeOfConcentrationEnum.TwentyMinutes:
                    return TwentyMinutes;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum TimeOfConcentrationEnum
    {
        FiveMinutes = 1,
        TenMinutes = 2,
        FifteenMinutes = 3,
        TwentyMinutes = 4,
        ThirtyMinutes = 5,
        FortyFiveMinutes = 6,
        SixtyMinutes = 7
    }

    public partial class TimeOfConcentrationFiveMinutes : TimeOfConcentration
    {
        private TimeOfConcentrationFiveMinutes(int timeOfConcentrationID, string timeOfConcentrationName, string timeOfConcentrationDisplayName) : base(timeOfConcentrationID, timeOfConcentrationName, timeOfConcentrationDisplayName) {}
        public static readonly TimeOfConcentrationFiveMinutes Instance = new TimeOfConcentrationFiveMinutes(1, @"FiveMinutes", @"5");
    }

    public partial class TimeOfConcentrationTenMinutes : TimeOfConcentration
    {
        private TimeOfConcentrationTenMinutes(int timeOfConcentrationID, string timeOfConcentrationName, string timeOfConcentrationDisplayName) : base(timeOfConcentrationID, timeOfConcentrationName, timeOfConcentrationDisplayName) {}
        public static readonly TimeOfConcentrationTenMinutes Instance = new TimeOfConcentrationTenMinutes(2, @"TenMinutes", @"10");
    }

    public partial class TimeOfConcentrationFifteenMinutes : TimeOfConcentration
    {
        private TimeOfConcentrationFifteenMinutes(int timeOfConcentrationID, string timeOfConcentrationName, string timeOfConcentrationDisplayName) : base(timeOfConcentrationID, timeOfConcentrationName, timeOfConcentrationDisplayName) {}
        public static readonly TimeOfConcentrationFifteenMinutes Instance = new TimeOfConcentrationFifteenMinutes(3, @"FifteenMinutes", @"15");
    }

    public partial class TimeOfConcentrationTwentyMinutes : TimeOfConcentration
    {
        private TimeOfConcentrationTwentyMinutes(int timeOfConcentrationID, string timeOfConcentrationName, string timeOfConcentrationDisplayName) : base(timeOfConcentrationID, timeOfConcentrationName, timeOfConcentrationDisplayName) {}
        public static readonly TimeOfConcentrationTwentyMinutes Instance = new TimeOfConcentrationTwentyMinutes(4, @"TwentyMinutes", @"20");
    }

    public partial class TimeOfConcentrationThirtyMinutes : TimeOfConcentration
    {
        private TimeOfConcentrationThirtyMinutes(int timeOfConcentrationID, string timeOfConcentrationName, string timeOfConcentrationDisplayName) : base(timeOfConcentrationID, timeOfConcentrationName, timeOfConcentrationDisplayName) {}
        public static readonly TimeOfConcentrationThirtyMinutes Instance = new TimeOfConcentrationThirtyMinutes(5, @"ThirtyMinutes", @"30");
    }

    public partial class TimeOfConcentrationFortyFiveMinutes : TimeOfConcentration
    {
        private TimeOfConcentrationFortyFiveMinutes(int timeOfConcentrationID, string timeOfConcentrationName, string timeOfConcentrationDisplayName) : base(timeOfConcentrationID, timeOfConcentrationName, timeOfConcentrationDisplayName) {}
        public static readonly TimeOfConcentrationFortyFiveMinutes Instance = new TimeOfConcentrationFortyFiveMinutes(6, @"FortyFiveMinutes", @"45");
    }

    public partial class TimeOfConcentrationSixtyMinutes : TimeOfConcentration
    {
        private TimeOfConcentrationSixtyMinutes(int timeOfConcentrationID, string timeOfConcentrationName, string timeOfConcentrationDisplayName) : base(timeOfConcentrationID, timeOfConcentrationName, timeOfConcentrationDisplayName) {}
        public static readonly TimeOfConcentrationSixtyMinutes Instance = new TimeOfConcentrationSixtyMinutes(7, @"SixtyMinutes", @"60");
    }
}