//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[CustomAttributeDataType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class CustomAttributeDataType : IHavePrimaryKey
    {
        public static readonly CustomAttributeDataTypeString String = CustomAttributeDataTypeString.Instance;
        public static readonly CustomAttributeDataTypeInteger Integer = CustomAttributeDataTypeInteger.Instance;
        public static readonly CustomAttributeDataTypeDecimal Decimal = CustomAttributeDataTypeDecimal.Instance;
        public static readonly CustomAttributeDataTypeDateTime DateTime = CustomAttributeDataTypeDateTime.Instance;
        public static readonly CustomAttributeDataTypePickFromList PickFromList = CustomAttributeDataTypePickFromList.Instance;
        public static readonly CustomAttributeDataTypeMultiSelect MultiSelect = CustomAttributeDataTypeMultiSelect.Instance;

        public static readonly List<CustomAttributeDataType> All;
        public static readonly ReadOnlyDictionary<int, CustomAttributeDataType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static CustomAttributeDataType()
        {
            All = new List<CustomAttributeDataType> { String, Integer, Decimal, DateTime, PickFromList, MultiSelect };
            AllLookupDictionary = new ReadOnlyDictionary<int, CustomAttributeDataType>(All.ToDictionary(x => x.CustomAttributeDataTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected CustomAttributeDataType(int customAttributeDataTypeID, string customAttributeDataTypeName, string customAttributeDataTypeDisplayName, bool hasOptions, bool hasMeasurementUnit)
        {
            CustomAttributeDataTypeID = customAttributeDataTypeID;
            CustomAttributeDataTypeName = customAttributeDataTypeName;
            CustomAttributeDataTypeDisplayName = customAttributeDataTypeDisplayName;
            HasOptions = hasOptions;
            HasMeasurementUnit = hasMeasurementUnit;
        }

        [Key]
        public int CustomAttributeDataTypeID { get; private set; }
        public string CustomAttributeDataTypeName { get; private set; }
        public string CustomAttributeDataTypeDisplayName { get; private set; }
        public bool HasOptions { get; private set; }
        public bool HasMeasurementUnit { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return CustomAttributeDataTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(CustomAttributeDataType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.CustomAttributeDataTypeID == CustomAttributeDataTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as CustomAttributeDataType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return CustomAttributeDataTypeID;
        }

        public static bool operator ==(CustomAttributeDataType left, CustomAttributeDataType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CustomAttributeDataType left, CustomAttributeDataType right)
        {
            return !Equals(left, right);
        }

        public CustomAttributeDataTypeEnum ToEnum => (CustomAttributeDataTypeEnum)GetHashCode();

        public static CustomAttributeDataType ToType(int enumValue)
        {
            return ToType((CustomAttributeDataTypeEnum)enumValue);
        }

        public static CustomAttributeDataType ToType(CustomAttributeDataTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case CustomAttributeDataTypeEnum.DateTime:
                    return DateTime;
                case CustomAttributeDataTypeEnum.Decimal:
                    return Decimal;
                case CustomAttributeDataTypeEnum.Integer:
                    return Integer;
                case CustomAttributeDataTypeEnum.MultiSelect:
                    return MultiSelect;
                case CustomAttributeDataTypeEnum.PickFromList:
                    return PickFromList;
                case CustomAttributeDataTypeEnum.String:
                    return String;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum CustomAttributeDataTypeEnum
    {
        String = 1,
        Integer = 2,
        Decimal = 3,
        DateTime = 4,
        PickFromList = 5,
        MultiSelect = 6
    }

    public partial class CustomAttributeDataTypeString : CustomAttributeDataType
    {
        private CustomAttributeDataTypeString(int customAttributeDataTypeID, string customAttributeDataTypeName, string customAttributeDataTypeDisplayName, bool hasOptions, bool hasMeasurementUnit) : base(customAttributeDataTypeID, customAttributeDataTypeName, customAttributeDataTypeDisplayName, hasOptions, hasMeasurementUnit) {}
        public static readonly CustomAttributeDataTypeString Instance = new CustomAttributeDataTypeString(1, @"String", @"String", false, false);
    }

    public partial class CustomAttributeDataTypeInteger : CustomAttributeDataType
    {
        private CustomAttributeDataTypeInteger(int customAttributeDataTypeID, string customAttributeDataTypeName, string customAttributeDataTypeDisplayName, bool hasOptions, bool hasMeasurementUnit) : base(customAttributeDataTypeID, customAttributeDataTypeName, customAttributeDataTypeDisplayName, hasOptions, hasMeasurementUnit) {}
        public static readonly CustomAttributeDataTypeInteger Instance = new CustomAttributeDataTypeInteger(2, @"Integer", @"Integer", false, true);
    }

    public partial class CustomAttributeDataTypeDecimal : CustomAttributeDataType
    {
        private CustomAttributeDataTypeDecimal(int customAttributeDataTypeID, string customAttributeDataTypeName, string customAttributeDataTypeDisplayName, bool hasOptions, bool hasMeasurementUnit) : base(customAttributeDataTypeID, customAttributeDataTypeName, customAttributeDataTypeDisplayName, hasOptions, hasMeasurementUnit) {}
        public static readonly CustomAttributeDataTypeDecimal Instance = new CustomAttributeDataTypeDecimal(3, @"Decimal", @"Decimal", false, true);
    }

    public partial class CustomAttributeDataTypeDateTime : CustomAttributeDataType
    {
        private CustomAttributeDataTypeDateTime(int customAttributeDataTypeID, string customAttributeDataTypeName, string customAttributeDataTypeDisplayName, bool hasOptions, bool hasMeasurementUnit) : base(customAttributeDataTypeID, customAttributeDataTypeName, customAttributeDataTypeDisplayName, hasOptions, hasMeasurementUnit) {}
        public static readonly CustomAttributeDataTypeDateTime Instance = new CustomAttributeDataTypeDateTime(4, @"DateTime", @"Date/Time", false, false);
    }

    public partial class CustomAttributeDataTypePickFromList : CustomAttributeDataType
    {
        private CustomAttributeDataTypePickFromList(int customAttributeDataTypeID, string customAttributeDataTypeName, string customAttributeDataTypeDisplayName, bool hasOptions, bool hasMeasurementUnit) : base(customAttributeDataTypeID, customAttributeDataTypeName, customAttributeDataTypeDisplayName, hasOptions, hasMeasurementUnit) {}
        public static readonly CustomAttributeDataTypePickFromList Instance = new CustomAttributeDataTypePickFromList(5, @"PickFromList", @"Pick One from List", true, false);
    }

    public partial class CustomAttributeDataTypeMultiSelect : CustomAttributeDataType
    {
        private CustomAttributeDataTypeMultiSelect(int customAttributeDataTypeID, string customAttributeDataTypeName, string customAttributeDataTypeDisplayName, bool hasOptions, bool hasMeasurementUnit) : base(customAttributeDataTypeID, customAttributeDataTypeName, customAttributeDataTypeDisplayName, hasOptions, hasMeasurementUnit) {}
        public static readonly CustomAttributeDataTypeMultiSelect Instance = new CustomAttributeDataTypeMultiSelect(6, @"MultiSelect", @"Select Many from List", true, false);
    }
}