using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Notus.Communication
{
    public static class Request
    {
        public static async Task<string> Post(
            string UrlAddress,
            Dictionary<string, string> PostData,
            int TimeOut = 0,
            bool UseTimeoutAsSecond = true,
            bool showOnError = true
        )
        {
            using (HttpClient client = new HttpClient())
            {
                if (TimeOut > 0)
                {
                    client.Timeout = (UseTimeoutAsSecond == true ? TimeSpan.FromSeconds(TimeOut * 1000) : TimeSpan.FromMilliseconds(TimeOut));
                }

                HttpResponseMessage response = await client.PostAsync(UrlAddress, new FormUrlEncodedContent(PostData));
                if (response.IsSuccessStatusCode)
                {
                    HttpContent responseContent = response.Content;
                    return await responseContent.ReadAsStringAsync();
                }
            }
            return string.Empty;
        }
        public static (bool, string) PostSync(
            string UrlAddress,
            Dictionary<string, string> PostData,
            int TimeOut = 0,
            bool UseTimeoutAsSecond = true,
            bool showOnError = true
        )
        {
            FormUrlEncodedContent formContent = new FormUrlEncodedContent(PostData);
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    if (TimeOut > 0)
                    {
                        client.Timeout = (UseTimeoutAsSecond == true ? TimeSpan.FromSeconds(TimeOut * 1000) : TimeSpan.FromMilliseconds(TimeOut));
                    }
                    HttpResponseMessage response = client.PostAsync(UrlAddress, formContent).GetAwaiter().GetResult();
                    HttpContent responseContent = response.Content;
                    if (response.IsSuccessStatusCode)
                    {
                        return (true, responseContent.ReadAsStringAsync().GetAwaiter().GetResult());
                    }
                }
            }
            catch (Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    4456320,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );

                Notus.Print.Danger(showOnError, "Notus.Core.Function.PostRequestSync -> Line 606 -> " + err.Message);
            }
            return (false, string.Empty);
        }
        public static async Task<string> Get(string UrlAddress, int TimeOut = 0, bool UseTimeoutAsSecond = true, bool showOnError = true)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    if (TimeOut > 0)
                    {
                        client.Timeout = (UseTimeoutAsSecond == true ? TimeSpan.FromSeconds(TimeOut * 1000) : TimeSpan.FromMilliseconds(TimeOut));
                    }
                    HttpResponseMessage response = await client.GetAsync(UrlAddress);
                    if (response.IsSuccessStatusCode)
                    {
                        HttpContent responseContent = response.Content;
                        return await responseContent.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    774586,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
                Notus.Print.Danger(showOnError, "Notus.Core.Function.Get -> Line 80 -> " + err.Message);
            }
            return string.Empty;
        }
        public static string GetSync(
            string UrlAddress,
            int TimeOut = 0,
            bool UseTimeoutAsSecond = true,
            bool showOnError = true,
            Notus.Variable.Common.ClassSetting? objSettings = null
        )
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    if (TimeOut > 0)
                    {
                        client.Timeout = (UseTimeoutAsSecond == true ? TimeSpan.FromSeconds(TimeOut * 1000) : TimeSpan.FromMilliseconds(TimeOut));
                    }
                    HttpResponseMessage response = client.GetAsync(UrlAddress).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode)
                    {
                        HttpContent responseContent = response.Content;
                        return responseContent.ReadAsStringAsync().GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    5532145,
                    err.Message,
                    "BlockRowNo",
                    objSettings,
                    err
                );

                if (objSettings == null)
                {
                    Notus.Print.Danger(showOnError, "Notus.Core.Function.Get -> Line 112 -> " + err.Message);
                }
                else
                {
                    Notus.Print.Danger(objSettings, "Notus.Core.Function.Get -> Line 116 -> " + err.Message);
                }
            }
            return string.Empty;
        }
    }
}
