using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Web_Scrape_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                scrapeData();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error scraping data: " + e.Message.ToString());
            }
        }

        static void scrapeData()
        {
            var pageString = "";
            using (var client = new WebClient())
            {
                pageString = client.DownloadString("https://srh.bankofchina.com/search/whpj/searchen.jsp");
            }

            Console.WriteLine(" \n Scraping in process.. \n");

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageString);

            var selectTag = htmlDoc.DocumentNode.Descendants("select").Where(node => node.GetAttributeValue("id", "").Equals("pjname")).FirstOrDefault();
            var options = selectTag.Descendants("option").ToList();
            List<string> currencyOptions = new List<string>();
            foreach (var item in options)
            {
                currencyOptions.Add(item.InnerText);
            }

            string startDate = DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd");
            string endDate = DateTime.Today.ToString("yyyy-MM-dd");
            int currentPageNum = 1;
            int previousPageNum = 1;
            int counter = 0;

            for (int i = 1; i < currencyOptions.Count; i++)
            {
                var csv = new StringBuilder();
                string currentCurrency = currencyOptions[i];

                currentPageNum = 1;
                previousPageNum = 1;
                counter = 0;

                pageString = getHtmlString(startDate, endDate, currentCurrency, currentPageNum.ToString());
                htmlDoc.LoadHtml(pageString);

                var tables = htmlDoc.DocumentNode.Descendants("table").ToList();
                var rows = tables[2].Descendants("tr").ToList();
                int currentRows = rows.Count;

                if (currentRows < 2)
                {
                    Console.WriteLine(i + ". " + currentCurrency);
                    Console.WriteLine("\t No records.");
                    csv.AppendLine("No records.");
                    string fileName = @"\" + currentCurrency + "_" + startDate + "_" + endDate + ".txt";
                    string filePath = Directory.GetCurrentDirectory() + fileName;
                    File.WriteAllText(filePath, csv.ToString());
                    //Console.WriteLine("\t Done.");
                }
                else
                {
                    Console.WriteLine(i + ". " + currentCurrency);

                    foreach (var row in rows)
                    {
                        var cells = row.Descendants("td").ToList();

                        string rowForFile = "";
                        for (int d = 0; d < cells.Count; d++)
                        {
                            if (d == cells.Count - 1)
                            {
                                rowForFile = rowForFile + cells[d].InnerText;
                            }
                            else
                            {
                                rowForFile = rowForFile + cells[d].InnerText + ",";
                            }
                        }
                        csv.AppendLine(rowForFile);
                        counter++;
                    }

                    currentPageNum++;

                    pageString = getHtmlString(startDate, endDate, currentCurrency, currentPageNum.ToString());
                    htmlDoc.LoadHtml(pageString);

                    var pageForm = htmlDoc.DocumentNode.Descendants("form").Where(node => node.GetAttributeValue("name", "").Equals("pageform")).ToList();
                    var pageNum = pageForm[0].Descendants("input").Where(node => node.GetAttributeValue("name", "").Equals("page")).ToList();
                    int num = Int32.Parse(pageNum[0].GetAttributeValue("value", ""));

                    while (num > previousPageNum)
                    {
                        tables = htmlDoc.DocumentNode.Descendants("table").ToList();
                        rows = tables[2].Descendants("tr").ToList();
                        currentRows = rows.Count;

                        for (int r = 1; r < currentRows; r++)
                        {
                            var cells = rows[r].Descendants("td").ToList();

                            string rowForFile = "";
                            for (int d = 0; d < cells.Count; d++)
                            {
                                if (d == cells.Count - 1)
                                {
                                    rowForFile = rowForFile + cells[d].InnerText;
                                }
                                else
                                {
                                    rowForFile = rowForFile + cells[d].InnerText + ",";
                                }
                            }
                            csv.AppendLine(rowForFile);
                            counter++;
                        }

                        currentPageNum++;

                        pageString = getHtmlString(startDate, endDate, currentCurrency, currentPageNum.ToString());
                        htmlDoc.LoadHtml(pageString);

                        pageForm = htmlDoc.DocumentNode.Descendants("form").Where(node => node.GetAttributeValue("name", "").Equals("pageform")).ToList();
                        pageNum = pageForm[0].Descendants("input").Where(node => node.GetAttributeValue("name", "").Equals("page")).ToList();
                        num = Int32.Parse(pageNum[0].GetAttributeValue("value", ""));

                        previousPageNum++;
                    }

                    string fileName = @"\" + currentCurrency + "_" + startDate + "_" + endDate + ".txt";
                    string filePath = Directory.GetCurrentDirectory() + fileName;
                    File.WriteAllText(filePath, csv.ToString());
                    Console.WriteLine("\t Pages: {0}, total records: {1}", num, --counter);
                    Console.WriteLine("\t\t\t\t Done. ");
                }
            }
            Console.WriteLine();
            Console.WriteLine("Scraping completed.");
        }

        static string getHtmlString(string startDate, string endDate, string currency, string pageNumber)
        {
            string pageString = "";
            using (var client = new WebClient())
            {
                var values = new NameValueCollection();
                values["erectDate"] = startDate;
                values["nothing"] = endDate;
                values["pjname"] = currency;
                values["page"] = pageNumber;

                var response = client.UploadValues("https://srh.bankofchina.com/search/whpj/searchen.jsp", values);

                pageString = Encoding.Default.GetString(response);
            }
            return pageString;
        }


    }
}
