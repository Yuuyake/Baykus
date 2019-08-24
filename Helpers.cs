using OfficeOpenXml;
using Outlook = Microsoft.Office.Interop.Outlook;
using ExcelApp = Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Console = Colorful.Console;
using Colorful;
using Baykus.Properties;
using Microsoft.Office.Interop.Outlook;

namespace Baykus
{
    class Helpers {
        // ========================================   Miscellaneous Functions  ==================================
        //
        static Color red = Color.Red;
        static Color green = Color.FromArgb(0,255,0);
        static Color white = Color.White;
        static string ipTXTfile = "ip.txt";
        static string outputFileName = "result.xls";
        /// <summary>
        /// when writing password to console interface, hides characters
        /// </summary>
        /// <returns></returns>
        static public SecureString darker() { 
            SecureString securePwd = new SecureString();
            ConsoleKeyInfo key;
            do {
                key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter) {
                    securePwd.AppendChar(key.KeyChar);
                    Console.Write("*");
                }
                else {
                    if (key.Key == ConsoleKey.Backspace && securePwd.Length > 0) {
                        securePwd = new NetworkCredential("", securePwd.ToString().Substring(0, (securePwd.Length - 1))).SecurePassword;
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter) {
                        break;
                    }
                }
            } while (true);
            return securePwd;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stats"></param>
        /// <param name="counter"></param>
        static public void printStat(List<string> stats,int counter) {
            for (int i = 0; i < stats.Count; i++) {
                if (i == 0) {
                    string printer = "\n │\n ├─ {0} Requesting answer for IP: [{1}]";
                    Formatter[] fruits = new Formatter[] { new Formatter(counter, Color.LightGoldenrodYellow), new Formatter(stats[0], Color.LightGoldenrodYellow) };
                    Console.WriteFormatted(printer, Color.FromArgb(0, 255, 0), fruits);
                }
                else {
                    if(stats[i].Contains('!') == true) // this means there is error
                        Console.WriteFormatted(stats[i], Color.FromArgb(0,255,0));
                    else
                        Console.WriteFormatted(stats[i], Color.Green);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="apiName"></param>
        /// <returns></returns>
        static public string printOk(string apiName) { // to edit easier because of it is printed many time 
            //Console.WriteFormatted("\n │\t├─ " + apiName + " OK", Color.Green);
            return "\n │\t├─ " + apiName + " OK";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="problem"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        static public string printBad(string api, string problem, string msg) { // to edit easier because of it is printed many time 
            //Console.WriteFormatted("\n │\t├─ !" + api + " bad " + problem + ": " + msg, green);
            return "\n │\t├─ !" + api + " bad " + problem + ": " + msg;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <param name="myP"></param>
        /// <param name="IP"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        static public List<string> ExecuteWithTimeLimit(TimeSpan timeSpan, WebProxy myP , string IP, Func<string,int,List<string>> f) {
            List<string> temp = new List<string>();
            try {
                Task task = Task.Factory.StartNew(() => temp = f(IP,1));
                if (task.Wait(timeSpan) == true) 
                    return temp;
                temp = new List<string> { "?", "?" };
            }
            catch (AggregateException ae) {
                temp = new List<string> { "?", "?" };
                Console.WriteFormatted(" >> Failing to connect IP-api with given time", Color.Green);
            }
            return temp;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<string> getReportedIPs() {
            // get ip from Acunn,Scanning,TXT ayrı ayrı fonksiyon burda her birinin try catch i olsun
            // get ip from Acunn,Scanning,TXT ayrı ayrı fonksiyon burda her birinin try catch i olsun
            // get ip from Acunn,Scanning,TXT ayrı ayrı fonksiyon burda her birinin try catch i olsun
            List<string> IPs = new List<string>();
            List<string> files = Directory.GetFiles(Directory.GetCurrentDirectory()).ToList();
            foreach (var file in files) {
                var reportType = "";
                var fileName = file.Split('\\').Last();
                if (fileName.Contains("Acunn666"))
                    reportType = "Acunn666";
                else if (fileName.Contains("Scanning IP Addresses"))
                    reportType = "Scanning IP Addresses";
                else
                    continue; // if file is not a file we want pass it
                int counter = 0;
                Console.WriteFormatted("\n │ Getting IPs from ",Color.Gray);
                Console.WriteFormatted(fileName,Color.AntiqueWhite);
                Console.WriteFormatted(" ... ", Color.Gray);

                try {
                    var csvReader = new StreamReader(File.OpenRead(file));
                    csvReader.ReadLine();
                    while (!csvReader.EndOfStream) { // now we need Attacker Address column
                        var line = csvReader.ReadLine();
                        if (reportType == "Acunn666") // End Time, Exact Time, Attacker Address, Target Address
                            IPs.Add(line.Split(',')[2]);
                        else if(reportType == "Scanning IP Addresses") { // Attacker Address, Target Address, Count
                            var parsedLine = line.Split(',');
                            if (Int32.Parse(parsedLine[2]) < 5)
                                break;
                            else
                                IPs.Add(parsedLine[0]);
                        }
                        counter++;
                        Console.WriteFormatted("\r\t│ Getting IPs from \"{2}\" file [{0}] [{1}]...",
                            Color.AntiqueWhite, Color.Gray,counter, IPs.Last(), fileName);
                        Thread.Sleep(20);
                        IPs = IPs.Distinct().ToList();
                        Console.WriteFormatted("\r\t│ Getting IPs from \"{2}\" DONE [{0} record {1} unique IPs]",
                            Color.AntiqueWhite,Color.Gray, counter, IPs.Count, fileName);
                        // everthing is done. Check IP count
                        if (IPs.Count < 1) {
                            Console.WriteFormatted("\n\t│ 0 IP found. Press any to try again (q quit) ...", red);
                            if (Console.ReadLine() == "q")
                                System.Environment.Exit(1);
                        }
                    }
                }//try
                catch (System.Exception e) {
                    Console.WriteFormatted("\n\t│ Exception: " + e.Message, red);
                    Console.WriteFormatted("\n\t│ Maybe content is deformed.", red, red, fileName);
                    Console.Write("\n\t│ Press enter to continue...");
                    Console.ReadLine();
                }
            }//foreach 
            IPs.AddRange(readTxt());
            return IPs;
        }
        // <summary>
        /// stores current api keys to proper api key holders
        /// </summary>
        static public void SetApiKeys() {
            //static string HYBRID_API_KEY = "soscsgcok4ocks8c8kw40cck40g4wgkcwcsk48c00g4ksw4cgoc448sko4w04gsg";
            int counter = 0;

            var strkeys = MainClass.speconfig.ibmApiKeys;
            strkeys.ForEach(kk => MainClass.ibmApiKeys.Add(new ApiKey(kk.id, kk.pass,counter,"available")));

            strkeys = MainClass.speconfig.ntApiKeys;
            strkeys.ForEach(kk => MainClass.ntApiKeys.Add(new ApiKey(kk.id, kk.pass, counter, "available")));

            strkeys = MainClass.speconfig.mtApiKeys;
            strkeys.ForEach(kk => MainClass.mtApiKeys.Add(new ApiKey(kk.id, kk.pass, counter, "available")));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<string> readTxt() { // to read IPs from txt file
            int counter = 0;
            string line;
            List<string> IPs = new List<string>();
            Console.WriteFormatted("\n\t│ Getting IPs from TXT ",Color.Gray);
            // Read the file and display it line by line. 
            try {
                System.IO.StreamReader file = new System.IO.StreamReader(ipTXTfile);
                while ((line = file.ReadLine()) != null) {
                    Thread.Sleep(40);
                    IPs.Add(line);
                    Console.WriteFormatted("\r\t│ Getting IPs from {2} file [{0}] [{1}] ...",
                        Color.AntiqueWhite, Color.Gray, counter, line, ipTXTfile.Split('\\').Last());
                    counter++;
                }
            }
            catch (System.Exception e) {
                Console.WriteFormatted("\n\t│ Exception: " + e.Message, red);
                return new List<string>() { };
            }
            Console.WriteFormatted("\r\t│ Getting IPs from TXT DONE [{0} record {1} unique IPs]",
                Color.AntiqueWhite, Color.Gray, counter, IPs.Distinct().Count());
            return IPs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rows"></param>
        public static void printResults(List<List<string>> rows) {
            Console.Write("\n\n | << Reading responses");
            foreach (List<string> row in rows) {
                Console.Write("\n │\t├─ < ");
                foreach (string word in row) {
                    Console.Write(" :: " + word);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rows"></param>
        public static void writeRowsToExcel(List<Result> results) {
            int counter = 3; // counter for rows
            try {
                if (Type.GetTypeFromProgID("Excel.Application") != null) {
                    Console.Write("\n\n │ System has excel installed.");
                    Console.Write("\n │\t├─ >> Writing responses to excel ...");
                    using (ExcelPackage excel = new ExcelPackage()) {
                        var headerRow = new List<string[]>() { new string[] { DateTime.Now.ToString("dd/MM/yy - HH:mm"), "IBM-XFORCE", " ", " ", "IPAPI", " ", "METADEFENDER", "NEUTRINO" } };
                        var headerRow2 = new List<string[]>() { new string[] {
                            "IP",
                            "IBM-Country",
                            "ASN",
                            "Severity(1-10)",
                            "Organization",
                            "IPapi-Country",
                            "Blacklisted",
                            "Blocklisted"
                            }
                        };
                        // Determine the header range to write over it (e.g. A1:D1)
                        string headerRange = "A1:H1";
                        string headerRange2 = "A2:H2";
                        // Target a worksheet
                        excel.Workbook.Worksheets.Add("IP Reputations");
                        var worksheet = excel.Workbook.Worksheets["IP Reputations"];
                        // Populate the cells
                        worksheet.Cells[headerRange].LoadFromArrays(headerRow);
                        worksheet.Cells[headerRange].Style.Font.Size = 12;
                        worksheet.Cells[headerRange].Style.Font.Color.SetColor(System.Drawing.Color.Blue);

                        worksheet.Cells[headerRange2].LoadFromArrays(headerRow2);
                        worksheet.Cells[headerRange2].Style.Font.Bold = true;
                        worksheet.Cells[headerRange2].Style.Font.Size = 12;
                        worksheet.Cells[headerRange2].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                        // finally, write rows list to Excel's rows 
                        foreach (Result result in results) {
                            worksheet.Cells["A" + counter + ":H" + counter].LoadFromArrays(new List<string[]>() { result.ToArray() });
                            Console.Write("\n │\t\t├─ " + result.ToString());
                            counter++;
                        }
                        File.Delete(outputFileName); // Delete previously created results
                        FileInfo excelFile = new FileInfo(outputFileName);
                        excel.SaveAs(excelFile);
                    }
                    Console.Write("\n │\t├─ >> Writing responses to excel DONE [{0} record written to \\result.xls]", counter - 3);
                }
                else {
                    Console.Write("\n │ !System has not excel installed. \n │\t├─ Writing to txt ... ");
                    results.ForEach(rr => File.WriteAllText(@".\result.txt", rr.ToString() + Environment.NewLine));
                    Console.Write("\n │\t├─ Writing to txt DONE [{0} record is written]", results.Count);
                }
            }
            catch (System.Exception e) {
                Console.WriteFormatted("\n │ !!! EXCEPTION while writing to excel: " +  e.Message, red);
            }
        }
        static public string base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        static public string base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rows"></param>
        static public void WriteSomeMail(List<Result> results) {
            var resultTable = "NA";
            var cellProperty = @"<td width=130 nowrap valign=bottom style='width:97.75pt;border:none;height:15.0pt'>
                                 <p class=MsoNormal><span style='font-family:Calibri;mso-fareast-language:TR'>";
            var cellEnd = @"<o:p></o:p></span></p></td>";
            try {
                //string headerSpan = @"<p class=MsoNormal><span style='font-size:12.0pt;font-family:Consolas;color:blue;mso-fareast-language:TR'>";
                string tableHeader = "TABLE HEADER"; //Resources.tableHeader.Replace("01.08.19 - 11:07", DateTime.Now.ToString("dd/MM/yy - HH:mm"));
                List <string> excelRows = new List<string>();
                foreach (Result result in results) {
                    var rowData = "";
                    result.ToArray().ToList().ForEach(rr => rowData += cellProperty + rr + cellEnd);
                    excelRows.Add("<tr style='mso-yfti-irow:2;height:15.0pt'> " + rowData + "</tr>"); // a table raw created
                }
                resultTable = tableHeader + String.Join("", excelRows) + "</table>"; // end of the creating table
            }
            catch (System.Exception ee) {
                Console.Write("\n Creating table for mailing is failed . . . ", Color.Red);
                Console.Write("\n Exception: " + ee.Message, Color.Orange);
                resultTable = "No Table Created";
            }
            Outlook.Application OutApp  = new Outlook.Application();
            MailItem mailItem = (MailItem)OutApp.CreateItem(OlItemType.olMailItem);
            mailItem.Importance = OlImportance.olImportanceHigh;
            mailItem.Subject = "Scanning IP Addresses";
            // "pre" tag is standing for render as it is dont change anything, thats why we cannot tab on there
            mailItem.HTMLBody = "<pre " + "style=\"font-family:'Arial TUR'\" >" + @"Merhaba,<br/>
Ekteki Scanning IP raporlarına istinaden aşağıdaki <strong style='color:red;'>kırmızı olmayan IP'lerin</strong> erişimi blissadmin ile kesilmiştir.<br/>
Syg.<br/><br/>" + resultTable + "</pre>";

            mailItem.To = MainClass.speconfig.csirtMail + "," + MainClass.speconfig.altyapiMail;
            //mailItem.CC = MainClass.speconfig.atarMail;
            var attachFiles = Directory.GetFiles(Directory.GetCurrentDirectory()).
                Where(ff => ff.Contains("Scanning IP Addresses") || ff.Contains("Acunn666")).ToList();
            if (attachFiles.Where(ff => !File.Exists(ff)).Count() > 0 )
                Console.Write("\nSome of the Attached documents( " + String.Join(",",attachFiles.Where(ff => !File.Exists(ff))) + " ) are missing", Color.Red);
            else {
                System.Net.Mail.Attachment attachment;
                foreach ( string att in attachFiles) {
                    attachment = new System.Net.Mail.Attachment(att);
                    mailItem.Attachments.Add(att, OlAttachmentType.olByValue, Type.Missing, Type.Missing);
                }
            }
            mailItem.Display();
        }
        static public void WriteAtarMail(List<Result> results) {
            // finally, write rows list to Excel's rows 
            string mailBody = String.Join("", results.Select(rr => rr.ip + "<br/>"));
            Outlook.Application outlookApp = new Outlook.Application();
            MailItem mailItem = outlookApp.CreateItem(OlItemType.olMailItem);
            mailItem.To = MainClass.speconfig.atarMail;
            mailItem.CC = MainClass.speconfig.csirtMail;
            mailItem.Subject = MainClass.speconfig.atarTitle + MainClass.userName + "_" + DateTime.Now.ToString("ddMMMMyyyy");
            mailItem.HTMLBody = "<p style=\"font-family:'consolas'\" >" + mailBody + "</p>";
            mailItem.Importance = OlImportance.olImportanceHigh;
            mailItem.Display();
        }
        /*
        /// <summary>
        /// executes to login operation for the login page
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="tabs"></param>
        public void OpenLogInPage() {
            try {
                Driver.ffoxDriver.ExecuteJS("window.open('" + this.mainPage + "')");
                var tabs = Driver.ffoxDriver.WindowHandles;
                Driver.ffoxDriver.SwitchTo().Window(tabs[++(Driver.currTab)]);
                Driver.ffoxDriver.ExecuteJS("window.open('" + this.mainPage + "')");
                tabID = Driver.ffoxDriver.CurrentWindowHandle;
                Console.WriteLineFormatted("\t > DONE: " + this.mainPage, Color.Green);
            }
            catch (Exception ee) {
                Console.WriteLineFormatted("\t > Exception: Page " + this.mainPage + " is problematic . . .", Color.Red);
                var error = ee.Message;
                Console.WriteLineFormatted("\t\t> " + error, Color.Orange);
            }
            Thread.Sleep(500);
        }
            try {
                Driver.ffoxDriver.SwitchTo().Window(this.tabID);
                Driver.ffoxDriver.FindElement(By.XPath("//*[@id=\"userName-input\"]")).SendKeys(username);
                Driver.ffoxDriver.FindElement(By.XPath("//*[@id=\"password-input\"]")).SendKeys(new System.Net.NetworkCredential(string.Empty, password).Password);
                Driver.ffoxDriver.FindElement(By.XPath("//*[@type=\"button\"]")).Click();
                Console.WriteLineFormatted("\t > DONE: " + this.mainPage, Color.Green);
            }
            catch (Exception ee) {
                Console.WriteLineFormatted("\t > Exception: Page " + this.mainPage + " is problematic . . .", Color.Red);
                var error = ee.Message;
                Console.WriteLineFormatted("\t\t> " + error, Color.Orange);
            }
        }*/
    }
}
