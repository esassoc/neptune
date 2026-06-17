/*-----------------------------------------------------------------------
<copyright file="StringFormats.cs" company="Sitka Technology Group">
Copyright (c) Sitka Technology Group. All rights reserved.
<author>Sitka Technology Group</author>
</copyright>

<license>
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License <http://www.gnu.org/licenses/> for more details.

Source code is available upon request via <support@sitkatech.com>.
</license>
-----------------------------------------------------------------------*/

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Neptune.Common
{
    public static class StringFormats
    {
        public delegate bool TryParseDelegate<T>(string str, out T value);

        public static T? TryParseOrNull<T>(TryParseDelegate<T> parse, string str) where T : struct
        {
            T value;
            return parse(str, out value) ? value : (T?)null;
        }

        public static string Left(this string thisString, int length)
        {
            if (string.IsNullOrWhiteSpace(thisString))
                return string.Empty;

            if (thisString.Length < length)
                return thisString;
            
            return thisString.Substring(0, length);
        }

        public static string EncloseInParentheses(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            return $"({value})";
        }

        public static string ToStringPercent(this decimal? value)
        {
            return value.HasValue ? value.Value.ToStringPercent() : "";
        }
        public static string ToStringPercent(this decimal value)
        {
            return value.ToString("0.00%");
        }
        public static string ToPercentWithNoSymbolOrBlank(this decimal? d)
        {
            return d.HasValue ? (d.Value * 100).ToString("0.00") : "";
        }

        public static string ToDecimalString(this decimal? d)
        {
            return d.HasValue ? d.Value.ToString("0.00") : "";
        }

        public static string ToIntString(this decimal d)
        {
            return d.ToString("0");
        }

        public static string ToDecimalString(this decimal? d, bool isRounded)
        {
            return d.HasValue ?  (isRounded) ? d.Value.ToString("0.00") : d.Value.ToString(CultureInfo.InvariantCulture) : "";
        }
        
        public static string ToIntString(this decimal? d)
        {
            return d.HasValue ? d.Value.ToString("0") : "";
        }

        public static string ToGroupedNumeric(this byte value)
        {
            return value.ToString("#,###,###,##0");
        }

        public static string ToGroupedNumeric(this byte? value)
        {
            if (value.HasValue)
            {
                return ToGroupedNumeric(value.Value);
            }

            return string.Empty;
        }

        public static string ToGroupedNumeric(this int value)
        {
            return value.ToString("#,###,###,##0");
        }

        public static string ToGroupedNumeric(this int? value)
        {
            if (value.HasValue)
            {
                return ToGroupedNumeric(value.Value);
            }

            return string.Empty;
        }

        public static string ToGroupedNumeric(this long value)
        {
            return value.ToString("#,###,###,##0");
        }

        public static string ToGroupedNumeric(this long? value)
        {
            if (value.HasValue)
            {
                return ToGroupedNumeric(value.Value);
            }

            return string.Empty;
        }

        public static string ToGroupedNumeric(this decimal? value)
        {
            if (value.HasValue)
            {
                return ToGroupedNumeric(value.Value);
            }

            return string.Empty;
        }

        public static string ToGroupedNumeric(this decimal value)
        {
            return value.ToString("#,###,###,###,###,###,##0.###");
        }

        public static string ToGroupedNumeric(this double value)
        {
            return value.ToString("#,###,###,##0.###");
        }

        public static string ToGroupedNumeric(this double? value)
        {
            if (value.HasValue)
            {
                return ToGroupedNumeric(value.Value);
            }

            return string.Empty;
        }

        public static string ToStringShortPercent(this int value)
        {
            return value.ToString("0%");
        }

        public static string ToStringShortPercent(this decimal value)
        {
            return value.ToString("0%");
        }

        public static string ToStringShortPercent(this decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0%") : "null";
        }

        public static string ToNotProvidedIfNull(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return $"<span class='alert'>{value.ToProperCase()} {ViewUtilities.NotProvidedString}</span>";
            }
            return value;
        }

        public static string ToStringCurrencyFull(this decimal value)
        {
            return value.ToString("$#,##0.00");
        }
        public static string ToStringCurrencyFull(this decimal? value)
        {
            return value.HasValue ? value.Value.ToStringCurrencyFull() : string.Empty;
        }
        public static string ToStringCurrency(this decimal? value)
        {
            return value.HasValue ? value.Value.ToStringCurrency() : string.Empty;
        }
        public static string ToStringCurrency(this int? value)
        {
            return value.HasValue ? value.Value.ToStringCurrency() : string.Empty;
        }
        public static string ToStringCurrency(this double? value)
        {
            return value.HasValue ? value.Value.ToStringCurrency() : string.Empty;
        }
        public static string ToStringCurrency(this double value)
        {
            return value.ToString("$#,##0");
        }
        public static string ToStringCurrency(this decimal value)
        {
            return value.ToString("$#,##0");
        }
        public static string ToStringCurrency(this int value)
        {
            return value.ToString("$#,##0");
        }
        public static decimal? ParseNullableDecimalFromCurrencyString(string currencyString)
        {
            if (string.IsNullOrEmpty(currencyString))
            {
                return null;
            }
            decimal currencyValue;
            if (!decimal.TryParse(currencyString, NumberStyles.Currency, CultureInfo.CurrentCulture, out currencyValue))
            {
                throw new ApplicationException(string.Format("Could not parse currency value \"{0}\"", currencyString));
            }
            return currencyValue;
        }

        public static string ToStringDate(this DateTime dateTime)
        {
            return ConvertTimeFromUtcToPST(dateTime).ToString("MM/dd/yyyy");
        }
        public static string ToStringDate(this DateTime? dateTime)
        {
            return ToStringDate(dateTime, false);
        }

        public static string ToStringDate(this DateTime? dateTime, bool twoDigitYear)
        {
            var formatString = twoDigitYear ? "MM/dd/yy" : "MM/dd/yyyy";
            return (dateTime.HasValue) ? ConvertTimeFromUtcToPST(dateTime.Value).ToString(formatString) : string.Empty;
        }

        public static string ToStringTime(this DateTime? dateTime)
        {
            return (dateTime.HasValue) ? ConvertTimeFromUtcToPST(dateTime.Value).ToString("hh:mm tt") : string.Empty;
        }
        public static string ToStringTime(this DateTime dateTime)
        {
            return dateTime.ToString("hh:mm tt");
        }
        public static string ToStringDateTime(this DateTime dateTime)
        {
            return ((DateTime?)dateTime).ToStringDateTime();
        }
        public static string ToStringDateTime(this DateTime? dateTime)
        {
            return (dateTime.HasValue) ?
                ConvertTimeFromUtcToPST(dateTime).ToString("MM/dd/yyyy h:mm tt") : string.Empty;
        }

        private static DateTime ConvertTimeFromUtcToPST([DisallowNull] DateTime? dateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime.Value, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
        }

        public static DateTime ConvertTimeFromPSTToUTC(this DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(dateTime, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
        }

        public static string ToStringDateTimeNoLeadingZeros(this DateTime dateTime)
        {
            return ((DateTime?)dateTime).ToStringDateTimeNoLeadingZeros();
        }
        public static string ToStringDateTimeNoLeadingZeros(this DateTime? dateTime)
        {
            return (dateTime.HasValue) ? ConvertTimeFromUtcToPST(dateTime.Value).ToString("M/d/yyyy h:mm tt") : string.Empty;
        }
        public static string ToStringDateTimeFull(this DateTime dateTime)
        {
            return ((DateTime?)dateTime).ToStringDateTimeFull();
        }
        public static string ToStringDateTimeFull(this DateTime? dateTime)
        {
            // note - this is set to allow maximum precision, there are no more fractions available
            return (dateTime.HasValue) ? dateTime.Value.ToString("MM/dd/yyyy HH:mm:ss.fffffff") : string.Empty;
        }

        public static bool TryParsePhoneNumber(string input, out string phoneNumber)
        {
            const int strippedPhoneLength = 10;
            phoneNumber = null;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var strippedPhone = CleanPhoneNumber(input);
            if (strippedPhone.Length != strippedPhoneLength)
                return false;

            phoneNumber = strippedPhone.ToPhoneNumberString();

            return true;
        }

        public static string ToPhoneNumberString(this string phoneNumber)
        {
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                var strippedPhone = CleanPhoneNumber(phoneNumber);
                if (strippedPhone.Length == 10)
                    return string.Format("({0}) {1}-{2}", strippedPhone.Substring(0, 3), strippedPhone.Substring(3, 3),
                                     strippedPhone.Substring(6, 4));

                if (strippedPhone.Length == 7)
                    return string.Format("{0}-{1}", strippedPhone.Substring(0, 3), strippedPhone.Substring(3, 4));
            }
            return null;
        }

        public static string ToEllipsifiedString(this string fullString, int maxLength)
        {
            if (string.IsNullOrEmpty(fullString) || fullString.Length <= maxLength)
                return fullString;

            return fullString.Substring(0, maxLength) + "...";
        }

        public static string ToEllipsifiedStringClean(this string fullString, int maxLength)
        {
            return ToEllipsifiedStringClean(fullString, maxLength, "...");
        } 
        
        public static string ToEllipsifiedStringClean(this string fullString, int maxLength, string postfix)
        {
            if (string.IsNullOrWhiteSpace(fullString) || (fullString.Length <= maxLength))
                return fullString;

            var maxString = fullString.Trim().Substring(0, maxLength);
            var lastIndexOfSpace = maxString.LastIndexOf(' ');

            if (lastIndexOfSpace == -1)
                return maxString + postfix;

            return maxString.Substring(0, lastIndexOfSpace ) + postfix;
        }

        private static string CleanPhoneNumber(string input)
        {
            return !string.IsNullOrEmpty(input) ? new Regex("[^0-9]").Replace(input, string.Empty) : string.Empty;
        }


        /// <summary>
        /// This will only do the first letter of the whole string to upper case and lower case everything else
        /// "HEllO WORLD" -> "Hello world"
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ToUpperFirstLetter(this string text)
        {
            return $"{char.ToUpper(text[0])}{((text.Length > 1) ? text.Substring(1).ToLower() : string.Empty)}";
        }

        /// <summary>
        /// This will capitalize the first letter of every word in string
        /// "HEllO WORLD" -> "Hello World"
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ToTitleCase(this string text)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
        }

        /// <summary>
        /// Formats a number representing storage space in bytes using the SI prefixes: B, KB, MB, GB, TB, PB, EB and the base 2 1024 counting style
        /// </summary>
        public static string ToHumanReadableByteSize(long sizeInBytes)
        {
            var byteSizePrefixes = new []{ "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            var order = 0;
            double size = sizeInBytes;
            while (size >= 1024 && order + 1 < byteSizePrefixes.Length)
            {
                order++;
                size = size / 1024;
            }
            var result = $"{size:0.##} {byteSizePrefixes[order]}";
            return result;
        }

        // Capitalize the first character and add a space before
        // each capitalized letter (except the first character).
        public static string ToProperCase(this string text)
        {
            // If there are 0 or 1 characters, just return the string.
            if (text == null) return null;
            if (text.Length < 2) return text.ToUpper();

            // Start with the first character.
            var result = text.Substring(0, 1).ToUpper();

            // Add the remaining characters.
            for (var i = 1; i < text.Length; ++i)
            {
                if (char.IsUpper(text[i]))
                {
                    result += " ";
                }
                result += text[i];
            }

            return result;
        }

        // Convert the string to camel case. e.g. thisIsATestString
        public static string ToCamelCase(this string text)
        {
            // If there are 0 or 1 characters, just return the string.
            if (text == null || text.Length < 2)
                return text;

            // Split the string into words.
            var words = text.Split(
                new char[] { },
                StringSplitOptions.RemoveEmptyEntries);

            // Combine the words.
            var result = words[0].ToLower();
            for (var i = 1; i < words.Length; ++i)
            {
                result += $"{words[i].Substring(0, 1).ToUpper()}{words[i].Substring(1)}";
            }

            return result;
        }

        // Convert the string to Pascal case. e.g. ThisIsATestString
        public static string ToPascalCase(this string text)
        {
            // If there are 0 or 1 characters, just return the string.
            if (text == null) return null;
            if (text.Length < 2) return text.ToUpper();

            // Split the string into words.
            var words = text.Split(
                new char[] { },
                StringSplitOptions.RemoveEmptyEntries);

            // Combine the words.
            return words.Aggregate("", (current, word) => current + string.Format("{0}{1}", word.Substring(0, 1).ToUpper(), word.Substring(1)));
        }

        //public static readonly Regex ContainAbsoluteUrlWithApplicationDomainReferenceRegEx = new Regex(ConstructContainAbsoluteUrlWithApplicationDomainReferenceRegExForApplicationDomain(SitkaWebConfiguration.ApplicationDomain), RegexOptions.IgnoreCase | RegexOptions.Multiline);

        ///// <summary>
        ///// Does a given HTML string contain a non-server root relative URL pointing to the application domain? 
        ///// (This is bad & undesirable, by the way.)
        ///// </summary>
        ///// <param name="htmlString"></param>
        ///// <returns></returns>
        //public static bool DoesHtmlStringContainAbsoluteUrlWithApplicationDomainReference(this string htmlString)
        //{
        //    return htmlString.DoesHtmlStringContainAbsoluteUrlWithApplicationDomainReference(ContainAbsoluteUrlWithApplicationDomainReferenceRegEx);
        //}

        //public static HtmlString MakeAbsoluteLinksToApplicationDomainRelative(this HtmlString htmlString)
        //{
        //    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        //    if (htmlString == null || htmlString.ToString() == null)
        //    {
        //        // ReSharper disable once ExpressionIsAlwaysNull
        //        return htmlString;
        //    }
        //    return new HtmlString(ContainAbsoluteUrlWithApplicationDomainReferenceRegEx.Replace(htmlString.ToString(), string.Empty));
        //}

        public static string ConstructContainAbsoluteUrlWithApplicationDomainReferenceRegExForApplicationDomain(string applicationDomain)
        {
            return $@"http(s?)\:\/\/[0-9a-zA-Z]*((\.?){applicationDomain.Replace(@".", @"\.")})";
        }

        /// <summary>
        /// Only public for unit testing
        /// </summary>
        public static bool DoesHtmlStringContainAbsoluteUrlWithApplicationDomainReference(this string htmlString, Regex containAbsoluteUrlWithApplicationDomainReferenceRegEx)
        {
            return htmlString != null && containAbsoluteUrlWithApplicationDomainReferenceRegEx.IsMatch(htmlString);
        }

        public static string IndentLinesInStringByAmount(string linesToIndent, uint indentLevel, string indentToken)
        {
            if (linesToIndent == null)
            {
                return null;
            }
            if (linesToIndent == string.Empty)
            {
                return string.Empty;
            }
            if (indentLevel == 0 || string.IsNullOrEmpty(indentToken))
            {
                return linesToIndent;
            }

            var indentString = string.Empty;
            for (var i = 0; i < indentLevel; i++)
            {
                indentString += indentToken;
            }

            var lineMatches = Regex.Matches(linesToIndent, "[^\r\n]+([\r\n]+)?");
            var newLines = lineMatches.Cast<Match>().Select(x => indentString + x.Value);
            var newText = string.Join(string.Empty, newLines);
            return newText;
        }

        public static string SanitizeStringForGdb(this string str)
        {
            var arr = str.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray();
            return new string(arr).Replace(" ", "_");
        }
    }
}
