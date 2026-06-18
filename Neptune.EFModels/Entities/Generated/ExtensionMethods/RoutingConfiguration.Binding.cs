//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[RoutingConfiguration]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class RoutingConfiguration : IHavePrimaryKey
    {
        public static readonly RoutingConfigurationOnline Online = RoutingConfigurationOnline.Instance;
        public static readonly RoutingConfigurationOffline Offline = RoutingConfigurationOffline.Instance;

        public static readonly List<RoutingConfiguration> All;
        public static readonly ReadOnlyDictionary<int, RoutingConfiguration> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static RoutingConfiguration()
        {
            All = new List<RoutingConfiguration> { Online, Offline };
            AllLookupDictionary = new ReadOnlyDictionary<int, RoutingConfiguration>(All.ToDictionary(x => x.RoutingConfigurationID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected RoutingConfiguration(int routingConfigurationID, string routingConfigurationName, string routingConfigurationDisplayName)
        {
            RoutingConfigurationID = routingConfigurationID;
            RoutingConfigurationName = routingConfigurationName;
            RoutingConfigurationDisplayName = routingConfigurationDisplayName;
        }

        [Key]
        public int RoutingConfigurationID { get; private set; }
        public string RoutingConfigurationName { get; private set; }
        public string RoutingConfigurationDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return RoutingConfigurationID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(RoutingConfiguration other)
        {
            if (other == null)
            {
                return false;
            }
            return other.RoutingConfigurationID == RoutingConfigurationID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as RoutingConfiguration);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return RoutingConfigurationID;
        }

        public static bool operator ==(RoutingConfiguration left, RoutingConfiguration right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RoutingConfiguration left, RoutingConfiguration right)
        {
            return !Equals(left, right);
        }

        public RoutingConfigurationEnum ToEnum => (RoutingConfigurationEnum)GetHashCode();

        public static RoutingConfiguration ToType(int enumValue)
        {
            return ToType((RoutingConfigurationEnum)enumValue);
        }

        public static RoutingConfiguration ToType(RoutingConfigurationEnum enumValue)
        {
            switch (enumValue)
            {
                case RoutingConfigurationEnum.Offline:
                    return Offline;
                case RoutingConfigurationEnum.Online:
                    return Online;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum RoutingConfigurationEnum
    {
        Online = 1,
        Offline = 2
    }

    public partial class RoutingConfigurationOnline : RoutingConfiguration
    {
        private RoutingConfigurationOnline(int routingConfigurationID, string routingConfigurationName, string routingConfigurationDisplayName) : base(routingConfigurationID, routingConfigurationName, routingConfigurationDisplayName) {}
        public static readonly RoutingConfigurationOnline Instance = new RoutingConfigurationOnline(1, @"Online", @"Online");
    }

    public partial class RoutingConfigurationOffline : RoutingConfiguration
    {
        private RoutingConfigurationOffline(int routingConfigurationID, string routingConfigurationName, string routingConfigurationDisplayName) : base(routingConfigurationID, routingConfigurationName, routingConfigurationDisplayName) {}
        public static readonly RoutingConfigurationOffline Instance = new RoutingConfigurationOffline(2, @"Offline", @"Offline");
    }
}