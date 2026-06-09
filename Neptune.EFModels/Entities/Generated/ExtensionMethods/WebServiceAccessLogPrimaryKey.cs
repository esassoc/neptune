//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: dbo.WebServiceAccessLog


namespace Neptune.EFModels.Entities
{
    public class WebServiceAccessLogPrimaryKey : EntityPrimaryKey<WebServiceAccessLog>
    {
        public WebServiceAccessLogPrimaryKey() : base(){}
        public WebServiceAccessLogPrimaryKey(int primaryKeyValue) : base(primaryKeyValue){}
        public WebServiceAccessLogPrimaryKey(WebServiceAccessLog webServiceAccessLog) : base(webServiceAccessLog){}

        public static implicit operator WebServiceAccessLogPrimaryKey(int primaryKeyValue)
        {
            return new WebServiceAccessLogPrimaryKey(primaryKeyValue);
        }

        public static implicit operator WebServiceAccessLogPrimaryKey(WebServiceAccessLog webServiceAccessLog)
        {
            return new WebServiceAccessLogPrimaryKey(webServiceAccessLog);
        }
    }
}