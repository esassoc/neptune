//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[TrashCaptureStatusType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class TrashCaptureStatusType : IHavePrimaryKey
    {
        public static readonly TrashCaptureStatusTypeFull Full = TrashCaptureStatusTypeFull.Instance;
        public static readonly TrashCaptureStatusTypePartial Partial = TrashCaptureStatusTypePartial.Instance;
        public static readonly TrashCaptureStatusTypeNone None = TrashCaptureStatusTypeNone.Instance;
        public static readonly TrashCaptureStatusTypeNotProvided NotProvided = TrashCaptureStatusTypeNotProvided.Instance;

        public static readonly List<TrashCaptureStatusType> All;
        public static readonly ReadOnlyDictionary<int, TrashCaptureStatusType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static TrashCaptureStatusType()
        {
            All = new List<TrashCaptureStatusType> { Full, Partial, None, NotProvided };
            AllLookupDictionary = new ReadOnlyDictionary<int, TrashCaptureStatusType>(All.ToDictionary(x => x.TrashCaptureStatusTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected TrashCaptureStatusType(int trashCaptureStatusTypeID, string trashCaptureStatusTypeName, string trashCaptureStatusTypeDisplayName, int trashCaptureStatusTypeSortOrder, int trashCaptureStatusTypePriority, string trashCaptureStatusTypeColorCode)
        {
            TrashCaptureStatusTypeID = trashCaptureStatusTypeID;
            TrashCaptureStatusTypeName = trashCaptureStatusTypeName;
            TrashCaptureStatusTypeDisplayName = trashCaptureStatusTypeDisplayName;
            TrashCaptureStatusTypeSortOrder = trashCaptureStatusTypeSortOrder;
            TrashCaptureStatusTypePriority = trashCaptureStatusTypePriority;
            TrashCaptureStatusTypeColorCode = trashCaptureStatusTypeColorCode;
        }

        [Key]
        public int TrashCaptureStatusTypeID { get; private set; }
        public string TrashCaptureStatusTypeName { get; private set; }
        public string TrashCaptureStatusTypeDisplayName { get; private set; }
        public int TrashCaptureStatusTypeSortOrder { get; private set; }
        public int TrashCaptureStatusTypePriority { get; private set; }
        public string TrashCaptureStatusTypeColorCode { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return TrashCaptureStatusTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(TrashCaptureStatusType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.TrashCaptureStatusTypeID == TrashCaptureStatusTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as TrashCaptureStatusType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return TrashCaptureStatusTypeID;
        }

        public static bool operator ==(TrashCaptureStatusType left, TrashCaptureStatusType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TrashCaptureStatusType left, TrashCaptureStatusType right)
        {
            return !Equals(left, right);
        }

        public TrashCaptureStatusTypeEnum ToEnum => (TrashCaptureStatusTypeEnum)GetHashCode();

        public static TrashCaptureStatusType ToType(int enumValue)
        {
            return ToType((TrashCaptureStatusTypeEnum)enumValue);
        }

        public static TrashCaptureStatusType ToType(TrashCaptureStatusTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case TrashCaptureStatusTypeEnum.Full:
                    return Full;
                case TrashCaptureStatusTypeEnum.None:
                    return None;
                case TrashCaptureStatusTypeEnum.NotProvided:
                    return NotProvided;
                case TrashCaptureStatusTypeEnum.Partial:
                    return Partial;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum TrashCaptureStatusTypeEnum
    {
        Full = 1,
        Partial = 2,
        None = 3,
        NotProvided = 4
    }

    public partial class TrashCaptureStatusTypeFull : TrashCaptureStatusType
    {
        private TrashCaptureStatusTypeFull(int trashCaptureStatusTypeID, string trashCaptureStatusTypeName, string trashCaptureStatusTypeDisplayName, int trashCaptureStatusTypeSortOrder, int trashCaptureStatusTypePriority, string trashCaptureStatusTypeColorCode) : base(trashCaptureStatusTypeID, trashCaptureStatusTypeName, trashCaptureStatusTypeDisplayName, trashCaptureStatusTypeSortOrder, trashCaptureStatusTypePriority, trashCaptureStatusTypeColorCode) {}
        public static readonly TrashCaptureStatusTypeFull Instance = new TrashCaptureStatusTypeFull(1, @"Full", @"Full", 10, 1, @"E83521");
    }

    public partial class TrashCaptureStatusTypePartial : TrashCaptureStatusType
    {
        private TrashCaptureStatusTypePartial(int trashCaptureStatusTypeID, string trashCaptureStatusTypeName, string trashCaptureStatusTypeDisplayName, int trashCaptureStatusTypeSortOrder, int trashCaptureStatusTypePriority, string trashCaptureStatusTypeColorCode) : base(trashCaptureStatusTypeID, trashCaptureStatusTypeName, trashCaptureStatusTypeDisplayName, trashCaptureStatusTypeSortOrder, trashCaptureStatusTypePriority, trashCaptureStatusTypeColorCode) {}
        public static readonly TrashCaptureStatusTypePartial Instance = new TrashCaptureStatusTypePartial(2, @"Partial", @"Partial (>5mm but less than full sizing)", 20, 2, @"5289FF");
    }

    public partial class TrashCaptureStatusTypeNone : TrashCaptureStatusType
    {
        private TrashCaptureStatusTypeNone(int trashCaptureStatusTypeID, string trashCaptureStatusTypeName, string trashCaptureStatusTypeDisplayName, int trashCaptureStatusTypeSortOrder, int trashCaptureStatusTypePriority, string trashCaptureStatusTypeColorCode) : base(trashCaptureStatusTypeID, trashCaptureStatusTypeName, trashCaptureStatusTypeDisplayName, trashCaptureStatusTypeSortOrder, trashCaptureStatusTypePriority, trashCaptureStatusTypeColorCode) {}
        public static readonly TrashCaptureStatusTypeNone Instance = new TrashCaptureStatusTypeNone(3, @"None", @"No Trash Capture", 30, 3, @"3D3D3E");
    }

    public partial class TrashCaptureStatusTypeNotProvided : TrashCaptureStatusType
    {
        private TrashCaptureStatusTypeNotProvided(int trashCaptureStatusTypeID, string trashCaptureStatusTypeName, string trashCaptureStatusTypeDisplayName, int trashCaptureStatusTypeSortOrder, int trashCaptureStatusTypePriority, string trashCaptureStatusTypeColorCode) : base(trashCaptureStatusTypeID, trashCaptureStatusTypeName, trashCaptureStatusTypeDisplayName, trashCaptureStatusTypeSortOrder, trashCaptureStatusTypePriority, trashCaptureStatusTypeColorCode) {}
        public static readonly TrashCaptureStatusTypeNotProvided Instance = new TrashCaptureStatusTypeNotProvided(4, @"NotProvided", @"Not Provided", 40, 4, @"878688");
    }
}