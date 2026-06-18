//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[MaintenanceRecordType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class MaintenanceRecordType : IHavePrimaryKey
    {
        public static readonly MaintenanceRecordTypeRoutine Routine = MaintenanceRecordTypeRoutine.Instance;
        public static readonly MaintenanceRecordTypeCorrective Corrective = MaintenanceRecordTypeCorrective.Instance;

        public static readonly List<MaintenanceRecordType> All;
        public static readonly ReadOnlyDictionary<int, MaintenanceRecordType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static MaintenanceRecordType()
        {
            All = new List<MaintenanceRecordType> { Routine, Corrective };
            AllLookupDictionary = new ReadOnlyDictionary<int, MaintenanceRecordType>(All.ToDictionary(x => x.MaintenanceRecordTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected MaintenanceRecordType(int maintenanceRecordTypeID, string maintenanceRecordTypeName, string maintenanceRecordTypeDisplayName)
        {
            MaintenanceRecordTypeID = maintenanceRecordTypeID;
            MaintenanceRecordTypeName = maintenanceRecordTypeName;
            MaintenanceRecordTypeDisplayName = maintenanceRecordTypeDisplayName;
        }

        [Key]
        public int MaintenanceRecordTypeID { get; private set; }
        public string MaintenanceRecordTypeName { get; private set; }
        public string MaintenanceRecordTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return MaintenanceRecordTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(MaintenanceRecordType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.MaintenanceRecordTypeID == MaintenanceRecordTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as MaintenanceRecordType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return MaintenanceRecordTypeID;
        }

        public static bool operator ==(MaintenanceRecordType left, MaintenanceRecordType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MaintenanceRecordType left, MaintenanceRecordType right)
        {
            return !Equals(left, right);
        }

        public MaintenanceRecordTypeEnum ToEnum => (MaintenanceRecordTypeEnum)GetHashCode();

        public static MaintenanceRecordType ToType(int enumValue)
        {
            return ToType((MaintenanceRecordTypeEnum)enumValue);
        }

        public static MaintenanceRecordType ToType(MaintenanceRecordTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case MaintenanceRecordTypeEnum.Corrective:
                    return Corrective;
                case MaintenanceRecordTypeEnum.Routine:
                    return Routine;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum MaintenanceRecordTypeEnum
    {
        Routine = 1,
        Corrective = 2
    }

    public partial class MaintenanceRecordTypeRoutine : MaintenanceRecordType
    {
        private MaintenanceRecordTypeRoutine(int maintenanceRecordTypeID, string maintenanceRecordTypeName, string maintenanceRecordTypeDisplayName) : base(maintenanceRecordTypeID, maintenanceRecordTypeName, maintenanceRecordTypeDisplayName) {}
        public static readonly MaintenanceRecordTypeRoutine Instance = new MaintenanceRecordTypeRoutine(1, @"Routine", @"Routine");
    }

    public partial class MaintenanceRecordTypeCorrective : MaintenanceRecordType
    {
        private MaintenanceRecordTypeCorrective(int maintenanceRecordTypeID, string maintenanceRecordTypeName, string maintenanceRecordTypeDisplayName) : base(maintenanceRecordTypeID, maintenanceRecordTypeName, maintenanceRecordTypeDisplayName) {}
        public static readonly MaintenanceRecordTypeCorrective Instance = new MaintenanceRecordTypeCorrective(2, @"Corrective", @"Corrective");
    }
}