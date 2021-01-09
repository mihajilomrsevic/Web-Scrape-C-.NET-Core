using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Web_Scrape_C_Sharp_NET_Core
{
    class Program
    {
        static string url = "https://srh.bankofchina.com/search/whpj/searchen.jsp";
        static int page = 1;
        static int pageCount = 1;
        static int itemCount = 0;
        static StringBuilder listData = new StringBuilder();

        // Create a proxy object
        public static WebProxy proxy = new WebProxy
        {
            Address = new Uri($"http://178.115.244.26:8080")
        };

        // Now create a client handler which uses that proxy
        public static HttpClientHandler httpClientHandler = new HttpClientHandler
        {
            UseProxy = true,
            AllowAutoRedirect = true,
            Proxy = proxy,
        };
        public static async Task<string> GetResponseResult(string dateStart, string dateEnd, string currency, int page)
        {
            var dict = new Dictionary<string, string>();

            dict.Add("erectDate", dateStart);
            dict.Add("nothing", dateEnd);
            dict.Add("pjname", currency);
            dict.Add("page", page.ToString());

            var req = new HttpRequestMessage(HttpMethod.Post, url);

            var client = new HttpClient();

            var response = await client.PostAsync(url, new FormUrlEncodedContent(dict));
            var responseFromServer = "";
            using (Stream dataStream = await response.Content.ReadAsStreamAsync())
            {
                StreamReader reader = new StreamReader(dataStream);
                responseFromServer = reader.ReadToEnd();
            }
            req.Dispose();
            response.Dispose();
            if (responseFromServer.Contains("sorry, no records"))
            {
                return "sorry, no records";
            }

            while (responseFromServer.Contains("sorry,"))
            {
                req = new HttpRequestMessage(HttpMethod.Post, url);

                client = new HttpClient();

                response = await client.PostAsync(url, new FormUrlEncodedContent(dict));
                responseFromServer = "";
                using (Stream dataStream = await response.Content.ReadAsStreamAsync())
                {
                    StreamReader reader = new StreamReader(dataStream);
                    responseFromServer = reader.ReadToEnd();
                }
                req.Dispose();
                response.Dispose();
            }
            return responseFromServer;
        }
        private static string GetMultipleInstances(string value, int counter)
        {
            string output = "";
            for (int index = 0; index < counter; index++)
            {
                output = output + value;
            }
            return output;
        }
        private static async Task<string> Currencies()
        {
            DateTime dateToday = DateTime.Now;
            var dict = new Dictionary<string, string>();

            dict.Add("erectDate", dateToday.AddDays(-2).ToString("yyyy-MM-dd"));
            dict.Add("nothing", dateToday.ToString("yyyy-MM-dd"));
            dict.Add("pjname", "USD");
            dict.Add("page", page.ToString());

            var req = new HttpRequestMessage(HttpMethod.Post, url);

            var client = new HttpClient();

            var response = await client.PostAsync(url, new FormUrlEncodedContent(dict));
            var responseFromServer = "";
            using (Stream dataStream = await response.Content.ReadAsStreamAsync())
            {
                StreamReader reader = new StreamReader(dataStream);
                responseFromServer = reader.ReadToEnd();
            }
            req.Dispose();
            response.Dispose();
            return responseFromServer;

        }
        static void Main(string[] args)
        {
            listData.AppendLine($"Currency Name        Buying Rate     Cash Buying     Selling Rate        Cash Selling Rate       Middle Rate     Pub Time");
            DateTime dateToday = DateTime.Now;
            var resultFinal = "";

            var nesto = Currencies();
            while (nesto.Status.ToString() == "Faulted")
                nesto = Currencies();
            HtmlDocument htmlDocument1 = new HtmlDocument();
            htmlDocument1.LoadHtml(nesto.Result);

            var currenciesNode = htmlDocument1.DocumentNode.SelectNodes("//option").Skip(1).ToList();
            List<string> currencies = new List<string>();
            foreach (var c in currenciesNode)
            {
                currencies.Add(c.InnerText);
            }

            var currencyCounter = 0;
            var result = GetResponseResult(dateToday.AddDays(-2).ToString("yyyy-MM-dd"), dateToday.ToString("yyyy-MM-dd"), currencies[currencyCounter], pageCount);
            bool validateResult = false;
            while (currencyCounter < currencies.Count)
            {
                while (!resultFinal.Contains("var m_nRecordCount = "))
                {
                    result = null;
                    resultFinal = "";
                    result = GetResponseResult(dateToday.AddDays(-2).ToString("yyyy-MM-dd"), dateToday.ToString("yyyy-MM-dd"), currencies[currencyCounter], pageCount);
                    resultFinal = result.Result.ToString();
                    if (resultFinal.Contains("sorry, no records"))
                    {
                        validateResult = false;
                        break;
                    }
                    validateResult = true;
                }
                if (validateResult == true)
                {
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(resultFinal);

                    File.WriteAllText("data.txt", resultFinal);
                    string[] strSource = File.ReadAllLines("data.txt");
                    var dataCatched = "";
                    foreach (var l in strSource)
                    {
                        if (l.Contains("var m_nRecordCount = "))
                        {
                            dataCatched = l;
                            break;
                        }
                    }
                    var numberOfRecords = dataCatched.Split(" = ".Trim())[1].Replace(';', ' ').Trim();
                    var allPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(numberOfRecords) / 20));
                    Console.WriteLine("Number of pages: " + allPages);


                    var rows = htmlDocument.DocumentNode.SelectNodes("//tr").Skip(3).ToList();
                    var rowCount = 0;
                    foreach (var row in rows)
                    {
                        if (rowCount >= rows.Count - 4)
                        {
                            break;
                        }
                        rowCount++;
                        itemCount++;
                        List<HtmlNode> cells = row.SelectNodes("th|td").ToList();

                        string s1 = cells[0].InnerText.ToString().Replace("\r\n", "").Trim();
                        string s2 = cells[1].InnerText.ToString().Replace("\r\n", "").Trim();
                        string s3 = cells[2].InnerText.ToString();
                        string s4 = cells[3].InnerText.ToString();
                        string s5 = cells[4].InnerText.ToString();
                        string s6 = cells[5].InnerText.ToString();
                        string s7 = cells[6].InnerText.ToString();
                        listData.AppendLine(
                            $"{s1} " + GetMultipleInstances(" \t ", 15) +
                            $"{s2}" + GetMultipleInstances(" \t ", 8) +
                            $"{s3} " + GetMultipleInstances(" \t ", 8) +
                            $"{s4}" + GetMultipleInstances(" \t ", 10) +
                            $"{s5} " + GetMultipleInstances(" \t ", 14) +
                            $"{s6}" + GetMultipleInstances(" \t ", 9) +
                            $"{s7}"
                            );
                        Console.WriteLine($"{itemCount} {s1} {s2} {s3} {s4} {s5} {s6} {s7}");
                        Console.WriteLine($"{itemCount} {rowCount} {pageCount}");
                    }
                    rowCount = 0;
                    pageCount++;
                    // posle prvog
                    while ((pageCount < allPages + 1 || pageCount != allPages + 1) || !resultFinal.Contains("var m_nRecordCount = "))
                    {
                        result = null;
                        resultFinal = "";
                        result = GetResponseResult(dateToday.AddDays(-2).ToString("yyyy-MM-dd"), dateToday.ToString("yyyy-MM-dd"), currencies[currencyCounter], pageCount);
                        resultFinal = result.Result.ToString();
                        if (resultFinal.Contains("sorry, no records"))
                        {
                            break;
                        }
                        while (resultFinal.Contains("soryy"))
                        {
                            result = GetResponseResult(dateToday.AddDays(-2).ToString("yyyy-MM-dd"), dateToday.ToString("yyyy-MM-dd"), currencies[currencyCounter], pageCount);
                            resultFinal = result.Result.ToString();
                        }
                        File.WriteAllText("data.txt", resultFinal);
                        strSource = File.ReadAllLines("data.txt");
                        dataCatched = "";
                        foreach (var l in strSource)
                        {
                            if (l.Contains("var m_nRecordCount = "))
                            {
                                dataCatched = l;
                                break;
                            }
                        }

                        numberOfRecords = dataCatched.Split(" = ".Trim())[1].Replace(';', ' ').Trim();
                        allPages = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(numberOfRecords) / 20));
                        Console.WriteLine("Number of pages: " + allPages);

                        htmlDocument.LoadHtml(resultFinal);
                        rows = htmlDocument.DocumentNode.SelectNodes("//tr").Skip(3).ToList();
                        rowCount = 0;
                        foreach (var row in rows)
                        {
                            if (rowCount >= rows.Count - 4)
                            {
                                break;
                            }
                            rowCount++;
                            itemCount++;
                            List<HtmlNode> cells = row.SelectNodes("th|td").ToList();

                            string s1 = cells[0].InnerText.ToString().Replace("\r\n", "").Trim();
                            string s2 = cells[1].InnerText.ToString().Replace("\r\n", "").Trim();
                            string s3 = cells[2].InnerText.ToString();
                            string s4 = cells[3].InnerText.ToString();
                            string s5 = cells[4].InnerText.ToString();
                            string s6 = cells[5].InnerText.ToString();
                            string s7 = cells[6].InnerText.ToString();
                            listData.AppendLine(
                                $"{s1} " + GetMultipleInstances(" \t ", 15) +
                                $"{s2}" + GetMultipleInstances(" \t ", 8) +
                                $"{s3} " + GetMultipleInstances(" \t ", 8) +
                                $"{s4}" + GetMultipleInstances(" \t ", 10) +
                                $"{s5} " + GetMultipleInstances(" \t ", 14) +
                                $"{s6}" + GetMultipleInstances(" \t ", 9) +
                                $"{s7}"
                                );
                            Console.WriteLine($"{itemCount} {s1} {s2} {s3} {s4} {s5} {s6} {s7}");
                            Console.WriteLine($"{itemCount} {rowCount} {pageCount}");
                        }
                        pageCount++;
                    }
                    System.IO.File.WriteAllText($"{currencies[currencyCounter]}_{dateToday.AddDays(-2).ToString("yyyy-MM-dd")}_{dateToday.ToString("yyyy-MM-dd")}.csv", listData.ToString());
                    listData.Clear();
                    listData = new StringBuilder();
                    listData.AppendLine($"Currency Name        Buying Rate     Cash Buying     Selling Rate        Cash Selling Rate       Middle Rate     Pub Time");
                    itemCount = 0;
                    rowCount = 0;
                    currencyCounter++;
                    resultFinal = "";
                    pageCount = 1;
                }
                else
                {
                    currencyCounter++;
                }
            }
        }
    }
}
