//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[DelineationType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class DelineationType : IHavePrimaryKey
    {
        public static readonly DelineationTypeCentralized Centralized = DelineationTypeCentralized.Instance;
        public static readonly DelineationTypeDistributed Distributed = DelineationTypeDistributed.Instance;

        public static readonly List<DelineationType> All;
        public static readonly ReadOnlyDictionary<int, DelineationType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static DelineationType()
        {
            All = new List<DelineationType> { Centralized, Distributed };
            AllLookupDictionary = new ReadOnlyDictionary<int, DelineationType>(All.ToDictionary(x => x.DelineationTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected DelineationType(int delineationTypeID, string delineationTypeName, string delineationTypeDisplayName)
        {
            DelineationTypeID = delineationTypeID;
            DelineationTypeName = delineationTypeName;
            DelineationTypeDisplayName = delineationTypeDisplayName;
        }

        [Key]
        public int DelineationTypeID { get; private set; }
        public string DelineationTypeName { get; private set; }
        public string DelineationTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return DelineationTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(DelineationType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.DelineationTypeID == DelineationTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as DelineationType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return DelineationTypeID;
        }

        public static bool operator ==(DelineationType left, DelineationType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DelineationType left, DelineationType right)
        {
            return !Equals(left, right);
        }

        public DelineationTypeEnum ToEnum => (DelineationTypeEnum)GetHashCode();

        public static DelineationType ToType(int enumValue)
        {
            return ToType((DelineationTypeEnum)enumValue);
        }

        public static DelineationType ToType(DelineationTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case DelineationTypeEnum.Centralized:
                    return Centralized;
                case DelineationTypeEnum.Distributed:
                    return Distributed;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum DelineationTypeEnum
    {
        Centralized = 1,
        Distributed = 2
    }

    public partial class DelineationTypeCentralized : DelineationType
    {
        private DelineationTypeCentralized(int delineationTypeID, string delineationTypeName, string delineationTypeDisplayName) : base(delineationTypeID, delineationTypeName, delineationTypeDisplayName) {}
        public static readonly DelineationTypeCentralized Instance = new DelineationTypeCentralized(1, @"Centralized", @"Centralized");
    }

    public partial class DelineationTypeDistributed : DelineationType
    {
        private DelineationTypeDistributed(int delineationTypeID, string delineationTypeName, string delineationTypeDisplayName) : base(delineationTypeID, delineationTypeName, delineationTypeDisplayName) {}
        public static readonly DelineationTypeDistributed Instance = new DelineationTypeDistributed(2, @"Distributed", @"Distributed");
    }
}