//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[NeptuneArea]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class NeptuneArea : IHavePrimaryKey
    {
        public static readonly NeptuneAreaTrash Trash = NeptuneAreaTrash.Instance;
        public static readonly NeptuneAreaOCStormwaterTools OCStormwaterTools = NeptuneAreaOCStormwaterTools.Instance;
        public static readonly NeptuneAreaModeling Modeling = NeptuneAreaModeling.Instance;
        public static readonly NeptuneAreaPlanning Planning = NeptuneAreaPlanning.Instance;
        public static readonly NeptuneAreaAI AI = NeptuneAreaAI.Instance;

        public static readonly List<NeptuneArea> All;
        public static readonly ReadOnlyDictionary<int, NeptuneArea> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static NeptuneArea()
        {
            All = new List<NeptuneArea> { Trash, OCStormwaterTools, Modeling, Planning, AI };
            AllLookupDictionary = new ReadOnlyDictionary<int, NeptuneArea>(All.ToDictionary(x => x.NeptuneAreaID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected NeptuneArea(int neptuneAreaID, string neptuneAreaName, string neptuneAreaDisplayName, int sortOrder, bool showOnPrimaryNavigation)
        {
            NeptuneAreaID = neptuneAreaID;
            NeptuneAreaName = neptuneAreaName;
            NeptuneAreaDisplayName = neptuneAreaDisplayName;
            SortOrder = sortOrder;
            ShowOnPrimaryNavigation = showOnPrimaryNavigation;
        }

        [Key]
        public int NeptuneAreaID { get; private set; }
        public string NeptuneAreaName { get; private set; }
        public string NeptuneAreaDisplayName { get; private set; }
        public int SortOrder { get; private set; }
        public bool ShowOnPrimaryNavigation { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return NeptuneAreaID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(NeptuneArea other)
        {
            if (other == null)
            {
                return false;
            }
            return other.NeptuneAreaID == NeptuneAreaID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as NeptuneArea);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return NeptuneAreaID;
        }

        public static bool operator ==(NeptuneArea left, NeptuneArea right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(NeptuneArea left, NeptuneArea right)
        {
            return !Equals(left, right);
        }

        public NeptuneAreaEnum ToEnum => (NeptuneAreaEnum)GetHashCode();

        public static NeptuneArea ToType(int enumValue)
        {
            return ToType((NeptuneAreaEnum)enumValue);
        }

        public static NeptuneArea ToType(NeptuneAreaEnum enumValue)
        {
            switch (enumValue)
            {
                case NeptuneAreaEnum.AI:
                    return AI;
                case NeptuneAreaEnum.Modeling:
                    return Modeling;
                case NeptuneAreaEnum.OCStormwaterTools:
                    return OCStormwaterTools;
                case NeptuneAreaEnum.Planning:
                    return Planning;
                case NeptuneAreaEnum.Trash:
                    return Trash;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum NeptuneAreaEnum
    {
        Trash = 1,
        OCStormwaterTools = 2,
        Modeling = 3,
        Planning = 4,
        AI = 5
    }

    public partial class NeptuneAreaTrash : NeptuneArea
    {
        private NeptuneAreaTrash(int neptuneAreaID, string neptuneAreaName, string neptuneAreaDisplayName, int sortOrder, bool showOnPrimaryNavigation) : base(neptuneAreaID, neptuneAreaName, neptuneAreaDisplayName, sortOrder, showOnPrimaryNavigation) {}
        public static readonly NeptuneAreaTrash Instance = new NeptuneAreaTrash(1, @"Trash", @"Trash Module", 40, true);
    }

    public partial class NeptuneAreaOCStormwaterTools : NeptuneArea
    {
        private NeptuneAreaOCStormwaterTools(int neptuneAreaID, string neptuneAreaName, string neptuneAreaDisplayName, int sortOrder, bool showOnPrimaryNavigation) : base(neptuneAreaID, neptuneAreaName, neptuneAreaDisplayName, sortOrder, showOnPrimaryNavigation) {}
        public static readonly NeptuneAreaOCStormwaterTools Instance = new NeptuneAreaOCStormwaterTools(2, @"OCStormwaterTools", @"Inventory Module", 10, true);
    }

    public partial class NeptuneAreaModeling : NeptuneArea
    {
        private NeptuneAreaModeling(int neptuneAreaID, string neptuneAreaName, string neptuneAreaDisplayName, int sortOrder, bool showOnPrimaryNavigation) : base(neptuneAreaID, neptuneAreaName, neptuneAreaDisplayName, sortOrder, showOnPrimaryNavigation) {}
        public static readonly NeptuneAreaModeling Instance = new NeptuneAreaModeling(3, @"Modeling", @"Modeling Module", 20, true);
    }

    public partial class NeptuneAreaPlanning : NeptuneArea
    {
        private NeptuneAreaPlanning(int neptuneAreaID, string neptuneAreaName, string neptuneAreaDisplayName, int sortOrder, bool showOnPrimaryNavigation) : base(neptuneAreaID, neptuneAreaName, neptuneAreaDisplayName, sortOrder, showOnPrimaryNavigation) {}
        public static readonly NeptuneAreaPlanning Instance = new NeptuneAreaPlanning(4, @"Planning", @"Planning Module", 30, true);
    }

    public partial class NeptuneAreaAI : NeptuneArea
    {
        private NeptuneAreaAI(int neptuneAreaID, string neptuneAreaName, string neptuneAreaDisplayName, int sortOrder, bool showOnPrimaryNavigation) : base(neptuneAreaID, neptuneAreaName, neptuneAreaDisplayName, sortOrder, showOnPrimaryNavigation) {}
        public static readonly NeptuneAreaAI Instance = new NeptuneAreaAI(5, @"AI", @"AI Module", 50, true);
    }
}