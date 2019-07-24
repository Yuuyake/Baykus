using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Console = Colorful.Console;
using Colorful;

namespace BAYKUS {
    public class APIPAIR {
        public string id;
        public string pass;
        public bool state;
        public APIPAIR(string _id, string _pass,bool _state) { id = _id; pass = _pass; state = _state; }
    }
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
        static public string base64Encode(string plainText) {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        static public string base64Decode(string base64EncodedData) {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
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
        static public string printOK(string apiName) { // to edit easier because of it is written many places 
            //Console.WriteFormatted("\n │\t├─ " + apiName + " OK", Color.Green);
            return "\n │\t├─ " + apiName + " OK";
        }
        static public string printBad(string api, string problem, string msg) { // to edit easier because of it is written many places 
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
            List<string> IPs = new List<string>();
            List<string> fis = Directory.GetFiles(Directory.GetCurrentDirectory()).ToList();

            foreach (var fi in fis) {
                if (fi.Contains("Acunn666") == false)
                    continue;
                int counter = 0;
                Console.WriteFormatted("\n │ Getting IPs from ",Color.Gray);
                Console.WriteFormatted(fi.Split('\\').Last(),Color.AntiqueWhite);
                Console.WriteFormatted(" ... ", Color.Gray);

                try {
                    var csvReader = new StreamReader(File.OpenRead(fi));
                    csvReader.ReadLine();
                    while (!csvReader.EndOfStream) {
                        var line = csvReader.ReadLine();
                        IPs.Add(line.Split(',')[2]);
                        counter++;
                        Console.WriteFormatted("\r\t│ Getting IPs from \"{2}\" file [{0}] [{1}]...",
                            Color.AntiqueWhite, Color.Gray,counter, IPs.Last(), fi.Split('\\').Last());
                        Thread.Sleep(20);
                    }
                    IPs = IPs.Distinct().ToList();
                    Console.WriteFormatted("\r\t│ Getting IPs from \"{2}\" DONE [{0} record {1} unique IPs]",
                        Color.AntiqueWhite,Color.Gray, counter, IPs.Count, fi.Split('\\').Last());
                    // everthing is done. Check IP count
                    if (IPs.Count < 1) {
                        Console.WriteFormatted("\n\t│ 0 IP found. Press any to try again (q quit) ...", red);
                        if (Console.ReadLine() == "q")
                            System.Environment.Exit(1);
                    }
                }//try
                catch (Exception e) {
                    Console.WriteFormatted("\n\t│ Exception: " + e.Message, red);
                    Console.WriteFormatted("\n\t│ Maybe content is deformed.", red, red, fi.Split('\\').Last());
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
        static public void getApiKeys() {
            //static string HYBRID_API_KEY = "soscsgcok4ocks8c8kw40cck40g4wgkcwcsk48c00g4ksw4cgoc448sko4w04gsg";
            acun.IBMAPI= new List<APIPAIR>() {
                new APIPAIR("3c765336-549c-4fc8-966f-0fdb44c15c90", "79bfd4cf-d7da-4f1f-a9e5-663cfca1b4be",true)
                // place as many as u can
            };
            acun.NTAPI = new List<APIPAIR>() {
                new APIPAIR("some", "1LYwU0hYYcne9zUIpiqtCHQCsxwi1Pkjp24WStVvxHqvAGTv",true)
                // place as many as u can
            };
            acun.MTAPI = new List<APIPAIR>() {
                new APIPAIR("1", "38332de852a8f329212b8f4575b9a766",true)
                // place as many as u can
            };
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
            catch (Exception e) {
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
        public static void writeRowsToExcel(List<List<string>> rows) {
            int counter = 3; // counter for rows
            try {
                if (Type.GetTypeFromProgID("Excel.Application") != null) {
                    Console.Write("\n\n │ System has excel installed.");
                    Console.Write("\n │\t├─ >> Writing responses to excel ...");
                    using (ExcelPackage excel = new ExcelPackage()) {
                        var headerRow = new List<string[]>() { };
                        headerRow.Add(new string[] {
                            DateTime.Now.ToString("dd/MM/yy - HH:mm"),
                            "IBM-XFORCE",
                            " ",
                            " ",
                            "IPAPI",
                            " ",
                            "METADEFENDER",
                            "NEUTRINO"
                        });
                        var headerRow2 = new List<string[]>() { };
                        headerRow2.Add(new string[] {
                            "IP",
                            "IBM-Country",
                            "ASN",
                            "Severity(1-10)",
                            "Organization",
                            "IPapi-Country",
                            "Blacklisted",
                            "Blocklisted"
                        });
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
                        foreach (List<string> row in rows) {
                            worksheet.Cells["A" + counter + ":H" + counter].LoadFromArrays(new List<string[]>() { row.ToArray() });
                            Console.Write("\n │\t\t├─ [{0}] {1} {2}", counter, row[0], row[1]);
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
                    foreach (List<string> row in rows) {
                        File.WriteAllText(@"result.txt", String.Join(", ", row.ToArray()));
                        counter++;
                    }
                    Console.Write("\n │\t├─ Writing to txt DONE [{0} record is written]", counter - 3);
                }

            }
            catch (Exception e) {
                Console.WriteFormatted("\n │ !!! EXCEPTION while writing to excel: " +  e.Message, red);
            }
        }
    }
}

/* here, trying to make request with OOP methods, but aborted bc of i see no advantage
public class makeRequest {
    List<string> rowList = new List<string>();
    WebProxy myProxySetting;
    string apiName;
    string apiAddress;
    string IP;

    public makeRequest(string _apiName, WebProxy _proxySettings, string _IP) {
        apiName = _apiName;
        myProxySetting = _proxySettings;
        IP = _IP;
        switch (apiName) {
            case "ipapi":
                apiAddress = "https://ipapi.co/" + IP + "/json/";
                Console.WriteLine("ipapi 1");
                break;
            case "ip_api":
                apiAddress = "http://ip-api.com/json/" + IP;
                Console.WriteLine("ip_api 2");
                break;
            case "metadefender":
                apiAddress = "http://api.metadefender.com/v1/scan/" + IP;
                Console.WriteLine("metadefender 3");
                break;
            case "ibm":
                apiAddress = "https://api.xforce.ibmcloud.com/ipr/" + IP;
                Console.WriteLine("ibm 4");
                break;
            case "neutrino":
                apiAddress = "https://neutrinoapi.com/ip-blocklist";
                Console.WriteLine("neutrino 5");
                break;
            default:
                Console.WriteLine("\t!!! NO CHOOSEN API ");
                break;
        }
        doRequest();
    }
    public List<string> doRequest() { return rowList; }
}
*/
