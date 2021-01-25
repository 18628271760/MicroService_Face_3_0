using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MicroService_Face_3_0.Utility
{
    public static class HttpHelper
    {
        public static async Task<string> PostFileAsByte(byte[] data, Uri url, string fileName)
        {
            using (var client = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    using (var stream = new MemoryStream(data))
                    {
                        StreamContent streamContent = new StreamContent(stream);
                        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        content.Add(streamContent, "file", fileName);
                        using (var message = client.PostAsync(url, content).Result)
                        {
                            return await message.Content.ReadAsStringAsync();
                        }
                    }
                }
            }
        }

        public static string PostAndGetJson(string url, Dictionary<string, string> dic)
        {
            string result = null;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            #region 添加Post 参数
            StringBuilder builder = new StringBuilder();
            int i = 0;
            foreach (var item in dic)
            {
                if (i > 0)
                {
                    builder.Append("&");
                }
                builder.AppendFormat("{0}={1}", item.Key, item.Value);
                i++;
            }
            byte[] data = Encoding.UTF8.GetBytes(builder.ToString());
            req.ContentLength = data.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();
            }
            #endregion
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            //获取响应内容
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }

        public static string PostFeatureData(string url, byte[] data)
        {
            string result = null;
            try
            {
                HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(url);
                webrequest.Method = "post";
                webrequest.ContentLength = data.Length;
                Stream stream = webrequest.GetRequestStream();
                stream.Write(data, 0, data.Length);
                stream.Close();
                using (var httpWebResponse = webrequest.GetResponse())
                {
                    using (StreamReader responseStream = new StreamReader(httpWebResponse.GetResponseStream()))
                    {
                        result = responseStream.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogInfo(" HttpHelper -> PostFeatureData exception as : " + ex.ToString());
            }
            return result;
        }

        public static void OpenDoorWithRrtry3Times(string url)
        {
            using (WebClient client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.GetEncoding("GB2312");
                Uri uri = new Uri(url);
                try
                {
                    LogHelper.LogInfo("Begin open door");
                    var openDoorString = Encoding.UTF8.GetString(client.DownloadData(uri));
                    LogHelper.LogInfo("End open door");
                    LogHelper.LogInfo("Open door result : " + openDoorString);
                }
                catch (Exception ex)
                {
                    LogHelper.LogInfo("HttpHelper->OpenDoorWithRrtry3Times exception : 调用URL：" + url + ex.ToString());
                    for (int index = 0; index < 3; index++)
                    {
                        try
                        {
                            LogHelper.LogInfo("Try open door :" + (index + 1) + " times begin:");
                            var openDoorString = Encoding.UTF8.GetString(client.DownloadData(uri));
                            LogHelper.LogInfo("Try open door :" + (index + 1) + " times success!");
                            break;
                        }
                        catch (Exception e)
                        {
                            LogHelper.LogInfo("Try open door :" + (index + 1) + " times throw exception as: 调用URL：" + url + e.ToString());
                        }
                    }
                }
            }
        }

        public static string GetJsonString(string url)
        {
            string result = null;
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.GetEncoding("GB2312");
                    Uri uri = new Uri(url);
                    result = client.DownloadString(uri);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogInfo("HttpHelper->GetJsonString generate exception as: 调用URL：" + url + ex.ToString());
            }
            return result;
        }
    }
}
