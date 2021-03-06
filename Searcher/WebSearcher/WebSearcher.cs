﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MagnetX.Searcher.WebSearcher
{
    abstract class WebSearcher : Searcher
    {
        protected static Regex regCfEmail1 = new Regex("<span class=\"__cf_email__\".+?>", RegexOptions.Compiled);
        protected string HandleCfEmail(string name)
        {
            return regCfEmail1.Replace(name, "").Replace("</span>", "");
        }

        /// <summary>
        /// 创建HttpClient。
        /// 子类可以重载此方法，来实现自定义请求头。
        /// </summary>
        /// <returns></returns>
        protected virtual HttpClient CreateHttpClient()
        {
            HttpClient httpClient = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            }, true);

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36");
            return httpClient;
        }

        /// <summary>
        /// 搜索指定关键词的实现。
        /// 对于不同的源，不需要修改这个方法，只需要继承本类，然后实现本类的抽象方法即可。
        /// </summary>
        /// <param name="word">关键词</param>
        /// <returns></returns>
        public override async void SearchAsync(string word)
        {
            for (int page = 1; page < 20; page++)
            {
                try
                {
                    string url = await GetURL(word, page).ConfigureAwait(false);
                    List<Result> list = new List<Result>();
                    for (int ntry = 0; ntry < 4; ntry++)
                    {
                        using (HttpClient hc = CreateHttpClient())
                        {
                            hc.Timeout = TimeSpan.FromMilliseconds(5000 + ntry * 2000);

                            try
                            {
                                var resp = await hc.GetAsync(url).ConfigureAwait(false);
                                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    string data = await hc.GetStringAsync(url).ConfigureAwait(false);
                                    foreach (string part in PrepareParts(data))
                                    {
                                        var result = await ReadPart(part).ConfigureAwait(false);
                                        if (result != null)
                                        {
                                            result.Name = HandleCfEmail(result.Name);
                                            list.Add(result);
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    await Task.Delay(250).ConfigureAwait(false);
                                }
                            }
                            catch (TaskCanceledException)
                            {
                                await Task.Delay(250).ConfigureAwait(false);
                            }
                        }
                    }

                    if (list.Count == 0) break;
                    if (OnResults == null) break;
                    if (!OnResults.Invoke(this, list)) break;
                    list.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// 测试该源是否可用的方法。
        /// </summary>
        /// <returns></returns>
        public override async Task<TestResults> TestAsync()
        {
            string url = await GetURL("电影", 1).ConfigureAwait(false);
            using (HttpClient hc = CreateHttpClient())
            {
                hc.Timeout = TimeSpan.FromMilliseconds(10000);
                try
                {
                    var resp = await hc.GetAsync(url).ConfigureAwait(false);
                    if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string data = await hc.GetStringAsync(url).ConfigureAwait(false);
                        foreach (string part in PrepareParts(data))
                        {
                            var result = await ReadPart(part).ConfigureAwait(false);
                            if (result != null) return TestResults.OK;
                        }

                        return TestResults.FormatError;
                    }
                    else
                    {
                        return TestResults.ServerError;
                    }
                }
                catch (TaskCanceledException)
                {
                    return TestResults.Timeout;
                }
                catch (Exception)
                {
                    return TestResults.UnknownError;
                }
            }
        }

        /// <summary>
        /// 抽象方法，用于获取指定关键字制定页号的搜索URL。
        /// </summary>
        /// <param name="word">关键字</param>
        /// <param name="page">页号</param>
        /// <returns></returns>
        protected abstract Task<string> GetURL(string word, int page);

        /// <summary>
        /// 抽象方法，对网页的原始内容进行预处理，返回若干个不同的分段，各个分段包含一个完整的结果。
        /// </summary>
        /// <param name="content">原始内容</param>
        /// <returns></returns>
        protected abstract IEnumerable<string> PrepareParts(string content);

        /// <summary>
        /// 抽象方法，用于解析从GetURL方法获得的分段，返回Result类型的结果。
        /// </summary>
        /// <param name="part">分段</param>
        /// <returns></returns>
        protected abstract Task<Result> ReadPart(string part);
    }
}
