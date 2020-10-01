using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using WebScraping.Model;


namespace WebScraping
{
    class Program
    {
        static int MAX_WAIT_SECONDS = 1000;
        
        static void Main(string[] args)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Console.WriteLine("Begin Scraping!");
                ScrapeProcess();
                Console.WriteLine("Success End of Scraping");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error for Scraping");
                Console.WriteLine(e);
            }
            Console.ReadLine();
            
            
        }

        public static  void ScrapeProcess()
        {
            /*
             * Files folder of application
             */
            var path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            var realPath = Path.Combine(path, @"..\..\..\Files"); 

            /*
             * Google Chrome Selenium driver used for Web Scrapping
             */
            var driver = new ChromeDriver();
            
            /*
             * URL of site
             */
            var url = "https://srh.bankofchina.com/search/whpj/searchen.jsp";
            /*
             *going to that site GET methotod 
             */
            driver.Navigate().GoToUrl(url);
            /*
             * getting currency list options and their values and storing them into list 
             */
            var currency=driver.FindElement(By.Id("pjname")).FindElements(By.TagName("option"));
            var currencyList = new List<string>();
            foreach (var node in currency)
            {
                var currencyValue = node.GetAttribute("value");
                if(currencyValue.Equals("0"))
                    continue;
                currencyList.Add(currencyValue);
            }
            /*
             * start date is 2 days before today,end day is today
             */
            DateTime start = DateTime.Now.AddDays(-2);
            DateTime end = DateTime.Now;

            var dateFormat = "yyyy-MM-dd";
            /*
             * inserting start and end date into form
             */
            driver.FindElement(By.Name("erectDate")).SendKeys(start.ToString(dateFormat));
            driver.FindElement(By.Name("nothing")).SendKeys(end.ToString(dateFormat));

            /*
             * foreach currency srape its each pagination table
             */
            foreach (var nextCurrency in currencyList)
            {
                /*
                 * Find currency and select it
                 */
                driver.FindElement(By.XPath("//select[@id = 'pjname']/option[@value = " + "'" + nextCurrency + "'" +
                                            "]")).Click();
                /*
                 * Find search button and click it
                 */
                driver.FindElement(By.XPath("//input[@type='button' and @value='search']")).Click();
                /*
                 * Find out if table has message that currency table is empty and go to next currency
                 */
                if(driver.FindElement(By.XPath("//table[2]/tbody/tr/td")).GetAttribute("innerHTML").Equals("sorry, no records！"))
                    continue;
                /*
                 * Exlpicit wait for first pagination table of currency to show up
                 * because pagination table number is rendered by
                  * window.onload() js methotod
                 */
                WebDriverWait waitForFirstLoad = new WebDriverWait(driver, TimeSpan.FromSeconds(MAX_WAIT_SECONDS)); 
                waitForFirstLoad.Until(e => e.FindElement(By.XPath("//table[3]/tbody/tr/td/div[@id='list_navigator']/span[@class = 'nav_page nav_currpage' and @title = 'First Page']")));
                /*
                 * getting number of pagination tables for selected currency
                 */
                IWebElement nodeLastPage = driver.FindElement(By.XPath("//table[3]/tbody/tr/td/div[@id='list_navigator']/span[@class='nav_page' and @title='Last Page']/a"));;
                
                int pageCount = int.Parse(nodeLastPage.GetAttribute("innerHTML"));
                /*
                 * deleting currency file if that file already exists 
                 */
                if (File.Exists(Path.Combine(realPath, nextCurrency + ".csv")))
                    File.Delete(Path.Combine(realPath, nextCurrency + ".csv"));

                FileInfo fi = new FileInfo(Path.Combine(realPath, nextCurrency + ".csv"));

                    
                using (FileStream fs = fi.Create())
                {
                    /*
                     * writting header of selected currency into its file
                     */
                    WriteToFileStream(fs,
                        "Currency Name,Buying Rate,Cash Buying Rate,Selling Rate,Cash Selling Rate,Middle Rate,Pub Time,Page Number");
                    /*
                     * get each table of selected currency and write to its file
                     */
                    for (int i = 1; i <= pageCount; i++)
                    {
                        /*
                         * for current paginated table select all rows
                         */
                        var currencyRowsList = driver.FindElements(By.XPath("//table[2]/tbody/tr")).Skip(1);
                        /*
                         * scrape data from each row and write into file
                         */
                        foreach (var currencyRow in currencyRowsList)
                        {
                            var currencyCeilList = currencyRow.FindElements(By.TagName("td")).ToArray();
                            CurrencyData cd = new CurrencyData();
                            cd.CurrencyName = currencyCeilList[0].Text;
                            cd.BuyingRate = currencyCeilList[1].Text;
                            cd.CashBuyingRate = currencyCeilList[2].Text;
                            cd.SellingRate = currencyCeilList[3].Text;
                            cd.CashSellingRate = currencyCeilList[4].Text;
                            cd.MiddleRate = currencyCeilList[5].Text;
                            cd.PubTime = currencyCeilList[6].Text;
                            cd.PageNumber = i;
                            WriteToFileStream(fs,cd.ToString());
                            
                        }
                        /*
                         * last pagination page of each currency does not have next buttton,
                         * so we need a break here
                         */
                        if (i == pageCount) 
                            break;
                        /*
                         * Explicit wait for next pagination table to show up,until it's
                         * number is current + 1 because pagination table number is rendered by
                         * window.onload() js methotod
                         */
                        driver.FindElement(By.XPath("//table[3]/tbody/tr/td/div[@id='list_navigator']/span[@class = 'wcm_pointer nav_go_next']/a")).SendKeys(Keys.Enter);
                        WebDriverWait waitForNextPage = new WebDriverWait(driver, TimeSpan.FromSeconds(MAX_WAIT_SECONDS)); 
                        waitForNextPage.Until(e => e.FindElement(By.XPath("//table[3]/tbody/tr/td/div[@id='list_navigator']/span[@class = 'nav_page nav_currpage']")).Text.Equals(""+(i+1)));
                        
                    }

                }
                
            }
        }

        public static void WriteToFileStream(FileStream fs,string text)
        {
            Byte[] bytes = new UTF8Encoding(true).GetBytes(text+"\n");
            fs.Write(bytes, 0, bytes.Length);
        }
        
        
    }
}
