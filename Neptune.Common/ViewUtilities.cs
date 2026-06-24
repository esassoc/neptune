using System.Web;

namespace Neptune.Common;

public static class ViewUtilities
{
    public const string NoneString = "None";
    public const string NoAnswerProvided = "<No answer provided>";
    public const string NoCommentString = "<no comment>";
    public const string NaString = "n/a";
    public const string NotFoundString = "(not found)";
    public const string NotAvailableString = "Not available";
    public const string NotProvidedString = "not provided";
    public const string NoChangesRecommended = "No changes recommended";

    public static string HtmlEncode(this string value)
    {
        return string.IsNullOrEmpty(value) ? value : HttpUtility.HtmlEncode(value);
    }

    public static string HtmlEncodeWithBreaks(this string value)
    {
        var ret = value.HtmlEncode();
        return string.IsNullOrEmpty(ret) ? ret : ret.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "<br/>\r\n");
    }
}