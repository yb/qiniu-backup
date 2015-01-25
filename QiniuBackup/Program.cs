using LitJson;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace QiniuBackup
{
    class Program
    {
        /// <summary>
        /// 配置
        /// </summary>
        static NameValueCollection Config = null;


        /// <summary>
        /// 程序入口
        /// </summary>
        static void Main(string[] args)
        {
            // 读取配置文件
            Config = ConfigurationManager.AppSettings;

            // 欢迎界面
            Console.WriteLine("========================================");
            Console.WriteLine("欢迎使用 Qiniu Backup 七牛备份工具");
            Console.WriteLine("请先确保配置文件中各项配置填写正确");
            Console.WriteLine("> 确认运行 请按 Enter 键");
            Console.WriteLine("> 取消运行 请按任意键");
            Console.WriteLine("========================================");
            if (Console.ReadKey(true).Key != ConsoleKey.Enter) return;

            // 私有空间下载签名的过期时间
            DateTime expired = DateTime.Now.AddMonths(1);

            // 每次扫描列表的数量
            int limit = 100;

            // 读取次数，成功下载次数，跳过次数
            int times = 0, success = 0, skip = 0;

            // 循环扫描文件列表
            string marker = "";
            do
            {
                Log("开始扫描第 " + (limit * times) + " 至 " + (limit * ++times) + " 个文件");

                // 扫描文件，取得扫描结果
                JsonData result = List(Config["Prefix"], limit, marker);
                JsonData items = result["items"];
                marker = result.Keys.Contains("marker") ? (string)result["marker"] : "";

                // 遍历文件列表，下载文件
                Log("扫描到 " + items.Count + " 个文件，开始下载");
                foreach (JsonData item in items)
                {
                    // 资源名，文件名
                    string filename = (string)item["key"];
                    string savepath = Config["SaveAs"] + filename.Replace('/', '\\');

                    // 如果文件存在，覆盖则删除，不覆盖则跳过
                    if (File.Exists(savepath))
                    {
                        if (Convert.ToBoolean(Config["OverWrite"])) File.Delete(savepath);
                        else
                        {
                            skip++;
                            continue;
                        }
                    }

                    // 检查并创建文件夹
                    CheckPath(savepath);

                    // 下载地址，如果为私有空间则追加签名
                    string url = Config["Domain"] + filename;
                    if (Convert.ToBoolean(Config["Private"])) url = url + "?" + DownloadToken(url, expired);

                    // 下载资源
                    Log("开始下载：" + filename);
                    WebClient web = new WebClient();
                    web.DownloadFile(url, savepath);
                    success++;
                }

            } while (marker != "");

            // 下载完成
            Console.WriteLine("========================================");
            Log("下载完成，成功下载 " + success + " 个文件，跳过 " + skip + " 个文件！");
            Console.WriteLine();
            Console.WriteLine("按任意键完成并退出");
            Console.ReadKey(true);
        }


        /// <summary>
        /// 输出时间和日志
        /// </summary>
        static void Log(string message)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
        }


        /// <summary>
        /// 检查路径，创建目录
        /// </summary>
        static void CheckPath(string path)
        {
            path = path.Substring(0, path.LastIndexOf('\\'));
            if (Directory.Exists(path) == false) Directory.CreateDirectory(path);
        }


        /// <summary>
        /// 读取资源列表
        /// </summary>
        static JsonData List(string prefix = "", int limit = 100, string marker = "")
        {
            string uri = string.Format("/list?bucket={0}&marker={1}&limit={2}&prefix={3}",
                HttpUtility.UrlEncode(Config["Bucket"]),
                HttpUtility.UrlEncode(marker),
                HttpUtility.UrlEncode(limit.ToString()),
                HttpUtility.UrlEncode(prefix)
                );

            WebClient web = new WebClient();
            web.Encoding = Encoding.UTF8;
            web.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
            web.Headers.Add(HttpRequestHeader.Authorization, "QBox " + AccessToken(uri));
            string result = web.DownloadString("http://rsf.qbox.me" + uri);

            return JsonMapper.ToObject(result);
        }


        /// <summary>
        /// URL 安全的 Base64 编码
        /// </summary>
        static string Base64Encode(string content)
        {
            return string.IsNullOrEmpty(content) 
                ? "" 
                : Base64Encode(Encoding.UTF8.GetBytes(content));
        }
        static string Base64Encode(byte[] content)
        {
            if (content == null || content.Length == 0) return "";
            string encode = Convert.ToBase64String(content);
            return encode.Replace('+', '-').Replace('/', '_');
        }


        /// <summary>
        /// 使用 HMAC-SHA1 对内容签名
        /// </summary>
        static string Sign(string content)
        {
            byte[] key = Encoding.UTF8.GetBytes(Config["SecretKey"]);
            HMACSHA1 hmac = new HMACSHA1(key);
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(content));
            string encode = Base64Encode(hash);
            return Config["AccessKey"] + ":" + encode;
        }


        /// <summary>
        /// 管理凭证
        /// </summary>
        static string AccessToken(string uri, string body = "")
        {
            return Sign(uri + "\n" + body);
        }


        /// <summary>
        /// 下载凭证
        /// </summary>
        static string DownloadToken(string url, DateTime expired)
        {
            DateTime time = new DateTime(1970, 1, 1);
            string e = ((int)(expired - time).TotalSeconds).ToString();
            return string.Format("e={0}&token={1}", e, Sign(url + "?e=" + e));
        }
    }
}
