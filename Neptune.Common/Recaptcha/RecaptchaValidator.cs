/*-----------------------------------------------------------------------
<copyright file="RecaptchaValidator.cs" company="Sitka Technology Group">
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

using System.Net.Http.Json;

namespace Neptune.Common.Recaptcha
{
    public class RecaptchaValidator
    {
        /// <summary>
        /// This is using the new Recaptcha v3 API
        /// </summary>
        public static async Task<bool> IsValidResponse(string verifyURL, string secretKey, string captchaResponseToken)
        {
            var reCaptchaVerifyUri = $"{verifyURL}?secret={secretKey}&response={captchaResponseToken}";

            var httpClient = new HttpClient();
            var googleRecaptchaV3Response = await httpClient.GetFromJsonAsync<GoogleRecaptchaV3Response>(reCaptchaVerifyUri);
            return googleRecaptchaV3Response!.Success;
        }
    }
}
