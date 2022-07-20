using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Notus.Communication
{
    public static class Request
    {
        public static async Task<string> Post(string UrlAddress, Dictionary<string, string> PostData, int TimeOut = 0, bool UseTimeoutAsSecond = true)
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
        public static string PostSync(string UrlAddress, Dictionary<string, string> PostData, int TimeOut = 0, bool UseTimeoutAsSecond = true)
        {
            //Console.WriteLine(UrlAddress);
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

                    if (response.IsSuccessStatusCode)
                    {
                        HttpContent responseContent = response.Content;
                        return responseContent.ReadAsStringAsync().GetAwaiter().GetResult();
                    }
                }
            }catch(Exception err)
            {

                Notus.Print.Danger(true,"Notus.Core.Function.PostRequestSync -> Line 606 -> " + err.Message);
                //Console.WriteLine("Notus.Core.Function.PostRequestSync -> Line 606");
                //Console.WriteLine(err.Message);
            }
            return string.Empty;
        }
        public static async Task<string> Get(string UrlAddress, int TimeOut = 0, bool UseTimeoutAsSecond = true)
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
                Console.WriteLine(err.Message);
            }
            return string.Empty;
        }
        public static string GetSync(string UrlAddress, int TimeOut = 0, bool UseTimeoutAsSecond = true)
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
                Console.WriteLine(err.Message);
            }
            return string.Empty;
        }
    }
}
