//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[WebServiceAccessLog]
namespace Neptune.EFModels.Entities
{
    public partial class WebServiceAccessLog : IHavePrimaryKey
    {
        public int PrimaryKey => WebServiceAccessLogID;


        public static class FieldLengths
        {
            public const int Endpoint = 200;
            public const int HttpMethod = 10;
        }
    }
}