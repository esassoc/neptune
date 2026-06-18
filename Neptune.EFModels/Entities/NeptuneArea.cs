namespace Neptune.EFModels.Entities
{
    public abstract partial class NeptuneArea
    {
        public abstract IRole GetRole(int roleID);
        public abstract bool IsAreaVisibleToPerson(Person person);
        public abstract string GetIconUrl();

        protected abstract Func<Person, IRole> GetPersonRoleToUseFunc();

    }

    public partial class NeptuneAreaAI
    {
        public override IRole GetRole(int roleID)
        {
            return Role.AllLookupDictionary[roleID];
        }

        public override bool IsAreaVisibleToPerson(Person person)
        {
            return person.RoleID == Role.Admin.RoleID || person.RoleID == Role.SitkaAdmin.RoleID;
        }

        public override string GetIconUrl()
        {
            return "/Content/img/icons/ai-icon.png";
        }

        protected override Func<Person, IRole> GetPersonRoleToUseFunc()
        {
            return x => x.Role;
        }
    }

    public partial class NeptuneAreaTrash
    {
        public override IRole GetRole(int roleID)
        {
            return Role.AllLookupDictionary[roleID];
        }

        public override bool IsAreaVisibleToPerson(Person person)
        {
            return true;
        }

        public override string GetIconUrl()
        {
            return "/Content/img/icons/trashIcon.png";
        }

        protected override Func<Person, IRole> GetPersonRoleToUseFunc()
        {
            return x => x.Role;
        }
    }

    public partial class NeptuneAreaOCStormwaterTools
    {
        public override IRole GetRole(int roleID)
        {
            return Role.AllLookupDictionary[roleID];
        }

        public override bool IsAreaVisibleToPerson(Person person)
        {
            return true;
        }

        public override string GetIconUrl()
        {
            return "/Content/img/icons/inventoryIcon.png";
        }

        protected override Func<Person, IRole> GetPersonRoleToUseFunc()
        {
            return x => x.Role;
        }
    }

    public partial class NeptuneAreaModeling
    {
        public override IRole GetRole(int roleID)
        {
            return Role.AllLookupDictionary[roleID];
        }

        public override bool IsAreaVisibleToPerson(Person person)
        {
            return true;
        }

        public override string GetIconUrl()
        {
            return "/Content/img/icons/modelingIcon.png";
        }

        protected override Func<Person, IRole> GetPersonRoleToUseFunc()
        {
            return x => x.Role;
        }
    }

    public partial class NeptuneAreaPlanning
    {
        public override IRole GetRole(int roleID)
        {
            return Role.AllLookupDictionary[roleID];
        }

        public override bool IsAreaVisibleToPerson(Person person)
        {
            return true;
        }

        public override string GetIconUrl()
        {
            return "/Content/img/icons/planningIcon.png";
        }

        protected override Func<Person, IRole> GetPersonRoleToUseFunc()
        {
            return x => x.Role;
        }
    }
}