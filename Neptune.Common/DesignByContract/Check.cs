/*-----------------------------------------------------------------------
<copyright file="Check.cs" company="Sitka Technology Group">
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

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Neptune.Common.DesignByContract
{
    /// <summary>
    /// Design By Contract Checks.
    ///
    /// Each method generates an exception or
    /// a Trace assertion if the contract is broken.
    ///
    /// If you wish to use Trace statements rather than exception handling then call the methods ending in Trace
    /// e.g., Check.RequireTrace(a > 1, "a must be > 1");
    /// Then output will be directed to a Trace listener. For example, you could insert
    ///
    /// Trace.Listeners.Clear();
    /// Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
    /// 
    /// </summary>
    public class Check
    {
        // abstract class - No creation
        private Check()
        {
        }

        #region Require

        /// <summary>
        /// Pinch point for all the throwing
        /// </summary>
        private static void ThrowThisException(Exception ex)
        {
            throw ex;
        }

        public static void Require(bool assertion, string message)
        {
            if (!assertion)
                ThrowThisException(new PreconditionException(message));
        }

        public static void RequireNotNull(object? thisObject, string message)
        {
            if (thisObject == null)
                ThrowThisException(new NullReferenceException(message));
        }

        #endregion

        #region Ensure

        public static void Ensure(bool assertion)
        {
            if (!assertion)
                throw new PostconditionException();
        }

        public static void Assert(bool assertion, string message)
        {
            if (!assertion)
            {
                ThrowThisException(new AssertionException(message));
            }
        }

        #endregion

    } // End Contract

} // End Design By Contract
