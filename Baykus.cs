/*
|                                 ``\":.     .     .:"/``              
| BAYKUS Reputation Reporter        \\";;;\"~^~"/;;;"//        
|                                    O( (O)\\|//(O) )O       
|                                    OOo~~_)\|/(_~~oOO                 
|                                   doO/~/~).Y.(~\~\OOb               
|                                  dob"_._~     ~_._'d0b
| Emre Ekinci                                     
| yunusemrem@windowslive.com	                   
|                                        
|                                      
|        
|      TODO:
           > rapor dosyasını plain table 3 yap
           > sonucu blissadmin kutucuğuna bas ( https://stackoverflow.com/questions/4015324/how-to-make-http-post-web-request  FLURRRR )
           > rapora Scanning IP raporlarını da dahil et (PDF okuyarak )   
           > projeyi linuxta çalıştır
           > amblemi düzenle     
           > logger fonksiyonu ekle, herşey loglansın 
           > OOP desing yap
           > Apı keyleri txt den al
           > APİ ler ayrı classlar olmalı error handle etmek zor, tempRow.add getCountry,getAsns metotlarından sonra doldurulmalı
*/
using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using System.Text;
using System.Net.Http;
using System.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using Console = Colorful.Console;
using Colorful;
using System.Security;

namespace BAYKUS {

    public class acun {
        static WebProxy myProxySetting = null;
        //here our API key-value pairs, there is at least 3 key for each API will be set in Helpers.cs file
        static public List<APIPAIR> IBMAPI;
        static public List<APIPAIR> NTAPI;
        static public List<APIPAIR> MTAPI;
        static public List<List<string>> rows     = new List<List<string>>(); // rows that will be written on a excel file
        static public List<List<string>> printers = new List<List<string>>(); // rows that will be written on a excel file
        // ==========================================   MAIN FUNC  ===============================================
        //
        static void Main(string[] args) {
            string bannerFile = new string(' ', Console.LargestWindowWidth / 2) + 
                String.Concat(Properties.Resources.owlBanner.Split('\n').Select(ss => ss = ss + new string(' ', Console.LargestWindowWidth / 2 - ss.Length) + '\n')).Replace("\r", "") + new string('─', Console.LargestWindowWidth / 2) + '\n';
            Console.Title = "BAYKUS";
            Console.OutputEncoding = Encoding.UTF8;
            Console.ForegroundColor = Color.LightGray;
            Console.SetWindowSize(Console.LargestWindowWidth / 2, Console.LargestWindowHeight -1);
            Console.WriteFormatted(bannerFile, Color.WhiteSmoke);

            Helpers.getApiKeys();
            List<string> IPs = Helpers.getReportedIPs();// read reported IPs from csv
            myProxySetting = initializeProxyConfigs();
            AsyncRequests(IPs);                         // this will make all needed requests from all APIs asynchronously
            Helpers.writeRowsToExcel(rows);             // responses wrote to the rows List variable, write them to report excel
            Console.ReadLine();
        }
        // ========================================   Network Configs  ===========================================
        //
        private static WebProxy initializeProxyConfigs() {

            Console.Write("\n\n │ Initializing proxy configs...");
            // setting PROXY config and Network CREDENTIALS
            // this is standing here (not in its own function) to get proxy settings
            HttpWebRequest tempReq = (HttpWebRequest)WebRequest.Create("http://google.com");
            WebProxy myProxySetting = new WebProxy();
            if (tempReq.Proxy != null) { // set grabbed proxy settings to myproxy
                Console.Write("\n │\t├─ Proxy   : {0}", tempReq.Proxy.GetProxy(tempReq.RequestUri));
                myProxySetting.Address = tempReq.Proxy.GetProxy(tempReq.RequestUri);
            }
            else {
                Console.Write("\n │\t├─ !No proxy detected.\n\t");
                Console.Write("Setting proxy to MYPROXY");
                //System.Environment.Exit(1);
                myProxySetting.Address = new Uri("MYPROXY");
            }
            // Setting User Creds to pass proxy
            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\')[1];
            Console.Write("\n │\t├─ Username: {0}", userName);
            SecureString securePwd = new SecureString();
            while (true) {
                Console.Write("\n │\t├─ Password: ");
                securePwd = Helpers.darker(); // ask and save user password on the quiet
                myProxySetting.Credentials = new NetworkCredential(userName, securePwd);
                List<string> tempResult = Helpers.ExecuteWithTimeLimit(TimeSpan.FromMilliseconds(2000), myProxySetting, "1.1.1.1", makeRequestIp_api);
                if (tempResult[0] == "FailProxyPass")
                    Console.WriteFormatted("\n │\t\t\tProbably wrong password..", Color.Red);
                else {
                    Console.WriteFormatted(" >> Passing proxy is OK", Color.Green);
                    break;
                }
            }
            securePwd.Dispose();
            return myProxySetting;
        }
        // ========================================   Make the Requests  =========================================
        //

        static void AsyncRequests(List<string> IPs) {

            for (int i = 0; i < IPs.Count; i++) { printers.Add(new List<string>() {IPs[i]});}
            int counter = 0;
            List<Task> allReq = new List<Task>();
            foreach (string IP in IPs) {
                int[] ipParts = IP.Split(new String[] { "." }, StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s)).ToArray();
                // in private ip range
                if (ipParts[0] == 10 
                    || (ipParts[0] == 192 && ipParts[1] == 168) 
                    || (ipParts[0] == 172 && (ipParts[1] >= 16 && ipParts[1] <= 31))) 
                {
                    rows.Add(new List<string>() { IP, " ", "PRIVATE IP RANGE", " ", " ", " ", " ", " " });
                    continue;
                }
                int myC = counter;
                //ASYNC = oneIpRequest(IP, myC);
                var eachTask = Task.Factory.StartNew(() => oneIpRequests(IP, myC));
                allReq.Add(eachTask);
                counter++;
            }
            Task.WaitAll(allReq.ToArray());
            Console.Write("\n │\n │ DONE _____________________________________________");
            return;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="IPs"></param>
        /// <returns></returns>
        static void oneIpRequests(string IP,int counter) {
            List<string> responseIBM    = new List<string>(new string[] { });
            List<string> responseIPAPI  = new List<string>(new string[] { });
            List<string> responseIP_API = new List<string>(new string[] { });
            List<string> responseMD     = new List<string>(new string[] { });
            List<string> responseNtrno  = new List<string>(new string[] { });

            //get api RESPONSES from requests asynchronous, choose whatever  
            //var taskIPAPI = Task.Run(() => responseIPAPI  = makeRequestIpapi(IP,counter));
            var taskIBM     = Task.Run(() => responseIBM    = makeRequestIBM(IP,counter));
            var taskIP_API  = Task.Run(() => responseIP_API = makeRequestIp_api(IP,counter));
            var taskMD      = Task.Run(() => responseMD     = makeRequestMDef(IP,counter));
            var taskNTRNO   = Task.Run(() => responseNtrno  = makeRequestNtrno(IP,counter));

            // await makes execution block for further lines
            Task.WaitAll(taskIBM, taskNTRNO, taskMD, taskIP_API);
            lock (rows) {
                // add data to the list of rows that will be written to the output CSV
                rows.Add(new List<string> { IP });
                rows.Last().AddRange(responseIBM);
                //rows.Last().AddRange(responseIPAPI);
                // select one of the other IP location API, if the one fails go for the other. 
                rows.Last().AddRange(responseIP_API);
                rows.Last().AddRange(responseMD);
                rows.Last().AddRange(responseNtrno);
                Helpers.printStat(printers[counter], counter);
            }
        }
        /// <summary>
        /// see\Resources\resultIp_api.json
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        static List<string> makeRequestIp_api(string IP, int counter) {
            string apiname = "ip-api";
            List<string> tempRow = new List<string>();
            string responseIP_API = "";
            try {   // create request , read response
                HttpWebRequest requestIP_API = (HttpWebRequest)WebRequest.Create("http://ip-api.com/json/" + IP);
                requestIP_API.Proxy = myProxySetting;
                using (HttpWebResponse response = (HttpWebResponse)requestIP_API.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream)) { responseIP_API = reader.ReadToEnd(); }
            }
            catch (Exception e) {
                if (e.Message.Contains("Proxy Authentication Required"))
                    return new List<string>() { "FailProxyPass", "?" }; // with this return value checked, blocks this app to continue 
                else {
                    printers[counter].Add(Helpers.printBad(apiname, "exception", e.Message));
                    if (e.Message.Contains("Forbidden")) {
                        return new List<string>() { "!KeyLimit!", "!KeyLimit!" };
                    }
                    return new List<string>() { "?", "?" };
                }
            }
            if (responseIP_API.Contains("error"))
                printers[counter].Add(Helpers.printBad(apiname, "error", responseIP_API.ToString()));
            else if (responseIP_API == null)
                printers[counter].Add(Helpers.printBad(apiname, "null returned", responseIP_API.ToString()));
            else {
                try {
                    // process the response if no failure exists
                    dynamic retIP_APIjson = JsonConvert.DeserializeObject(responseIP_API);
                    // add values to row > country+region,organization
                    tempRow.Add(retIP_APIjson.isp.ToString());
                    tempRow.Add(retIP_APIjson.country.ToString() + ", " + retIP_APIjson.countryCode.ToString());
                    printers[counter].Add(Helpers.printOK(apiname));
                    return tempRow;
                }
                catch (Exception e) {
                    printers[counter].Add(Helpers.printBad(apiname, "exception when parsing", e.Message));
                }
            }
            return new List<string>() { "?", "?" }; // if any fail happens, instruction comes to here to return properly
        }
        /// <summary>
        /// see\Resources\resultIpapi.json
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        static List<string> makeRequestIpapi(string IP, int counter) {
            string apiname = "ipapi";
            List<string> tempRow = new List<string>();
            string responseIPAPI = "";
            try {   // create request , read response
                HttpWebRequest requestIPAPI = (HttpWebRequest)WebRequest.Create("https://ipapi.co/" + IP + "/json/");
                requestIPAPI.Proxy = myProxySetting;
                using (HttpWebResponse response = (HttpWebResponse)requestIPAPI.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream)) { responseIPAPI = reader.ReadToEnd(); }
            }
            catch (Exception e) {
                printers[counter].Add(Helpers.printBad(apiname, "exception", e.Message));
                return new List<string>() { "?", "?" };
            }
            if (responseIPAPI.Contains("error"))
                printers[counter].Add(Helpers.printBad(apiname, "error", responseIPAPI.ToString()));
            else if (responseIPAPI == null)
                printers[counter].Add(Helpers.printBad(apiname, "null returned", responseIPAPI.ToString()));
            else {
                try {
                    // process the response if no failure exists
                    dynamic retIPAPIjson = JsonConvert.DeserializeObject(responseIPAPI);
                    // add values to row > country+region,organization
                    tempRow.Add(retIPAPIjson.org.ToString());
                    tempRow.Add(retIPAPIjson.country_name.ToString() + ", " + retIPAPIjson.region.ToString());
                    printers[counter].Add(Helpers.printOK(apiname));
                    return tempRow;
                }
                catch (Exception e) {
                    printers[counter].Add(Helpers.printBad(apiname, "exception when parsing", e.Message));
                }
            }
            return new List<string>() { "?", "?" }; // if any failure occurs, instruction comes to here to return properly
        }
        /// <summary>
        /// see\Resources\resultIBM.json
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        static List<string> makeRequestIBM(string IP, int counter) {
            int currKeyCounter = 0;
            string apiname = "ibm-xforce";
            string responseIBM = "";
            string loc = "?";
            string score = "?";
            List<string> tempRow = new List<string>();
            while (currKeyCounter < MTAPI.Count) {
                if (IBMAPI[currKeyCounter].state == false) {
                    currKeyCounter++;
                    continue;
                }
                try {   // create request , read response
                    HttpWebRequest requestIBM = (HttpWebRequest)WebRequest.Create("https://api.xforce.ibmcloud.com/ipr/" + IP);
                    requestIBM.Proxy = myProxySetting;
                    string apicreds = IBMAPI[0].id + ":" + IBMAPI[currKeyCounter].pass;
                    requestIBM.Headers.Add("Authorization", "Basic " + System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(apicreds)));
                    using (HttpWebResponse response = (HttpWebResponse)requestIBM.GetResponse()) {
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                            responseIBM = reader.ReadToEnd();
                    }
                    if(responseIBM.Contains("402") && responseIBM.Contains("error")) {
                        IBMAPI[currKeyCounter].state = false;
                        continue;
                    }
                }
                catch (Exception e) {
                    printers[counter].Add(Helpers.printBad(apiname, "exception", e.Message));
                }
                if (responseIBM.Contains("error") && responseIBM.Contains("code")) {
                    printers[counter].Add(Helpers.printBad(apiname, "error", responseIBM.ToString()));
                }
                else if (responseIBM == null)
                    printers[counter].Add(Helpers.printBad(apiname, "null returned", responseIBM.ToString()));
                else {
                    try {
                        // process the response
                        dynamic retIBMjson = JsonConvert.DeserializeObject(responseIBM);
                        //add values to row > score,country,company
                        loc = retIBMjson.geo.country.ToString() ?? "?";
                        tempRow.Add(loc);
                        var asns = retIBMjson.history.Last.asns;
                        // because of that we do not know the number that represents asns, foreach necessarry. Look data resultIBM.json 
                        try {
                            foreach (var asn in asns) {
                                string _orgName;
                                string orgName = asn.First.Company.ToString();
                                if (orgName.LastIndexOf("-") == -1)
                                    tempRow.Add(orgName);
                                else {
                                    _orgName = orgName.Substring(0, orgName.LastIndexOf("-"));
                                    int len = _orgName.Length - 4;
                                    tempRow.Add(_orgName.Substring(0, len >= 0 ? len : 0));
                                }
                                break;
                            }
                        }
                        catch {
                            tempRow.Add("?");
                            printers[counter].Add(Helpers.printBad(apiname, "exception when parsing", "ASNS,company"));
                        }
                        tempRow.Add(retIBMjson.score.ToString());
                        printers[counter].Add(Helpers.printOK(apiname));
                        return tempRow;
                    }
                    catch (Exception e) {
                        printers[counter].Add(Helpers.printBad(apiname, "exception when parsing", e.Message));
                        if (e.Message.Contains("Forbidden")) {
                            MTAPI[currKeyCounter].state = false;
                            continue;
                        }
                    }
                }
                return new List<string>() { loc, "?", score }; // if any fail happens, instruction comes to here to return properly
            }
            return new List<string>() { "!KeyLimit!", "!KeyLimit!", "!KeyLimit!" };
        }
        /// <summary>
        /// see\Resources\resultMD.json
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        static List<string> makeRequestMDef(string IP, int counter) {
            int currKeyCounter = 0;
            string apiname = "metadefender";
            string responseMDef = "";

            while (currKeyCounter < MTAPI.Count) {
                if (MTAPI[currKeyCounter].state == false) {
                    currKeyCounter++;
                    continue;
                }
                List<string> tempRow = new List<string>();
                try {// create request , read response
                    HttpWebRequest requestMETADEF = (HttpWebRequest)WebRequest.Create("http://api.metadefender.com/v1/scan/" + IP);
                    requestMETADEF.Proxy = myProxySetting;
                    requestMETADEF.Headers.Add("apikey: " + MTAPI[currKeyCounter].pass);
                    using (HttpWebResponse response = (HttpWebResponse)requestMETADEF.GetResponse()) {
                        using (Stream stream = response.GetResponseStream()) {
                            using (StreamReader reader = new StreamReader(stream)) { responseMDef = reader.ReadToEnd(); }
                        }
                    }
                }
                catch (Exception e) {
                    printers[counter].Add(Helpers.printBad(apiname, "exception", e.Message));
                    if (e.Message.Contains("Forbidden")) {
                        MTAPI[currKeyCounter].state = false;
                        continue;
                    }
                }
                if (responseMDef.Contains("error") && responseMDef.Contains("code"))
                    printers[counter].Add(Helpers.printBad(apiname, "error", responseMDef));
                else if (responseMDef == null)
                    printers[counter].Add(Helpers.printBad(apiname, "null returned", responseMDef.ToString()));
                else {
                    try {
                        // process the response
                        dynamic retMDjson = JsonConvert.DeserializeObject(responseMDef);
                        //Console.Write("\n" + JsonConvert.SerializeObject(retMDjson, Formatting.Indented) + "\n");
                        // add values to row > count,listOfBlacklists
                        tempRow.Add(retMDjson.detected_by.ToString() + " |");
                        foreach (var item in retMDjson.scan_results)
                            if (item.results.First.result.ToString() == "blacklisted")
                                tempRow[tempRow.Count - 1] = tempRow.Last() + ", " + item.source.ToString();
                        printers[counter].Add(Helpers.printOK(apiname));
                        return tempRow;
                    }
                    catch (Exception e) {
                        printers[counter].Add(Helpers.printBad(apiname, "exception when parsing", e.Message));
                    }
                }
            }
            return new List<string>() { "!KeyLimit!" };
        }
        /// <summary>
        /// see\Resources\resultNtrno.json
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        static List<string> makeRequestNtrno(string IP, int counter) {
            int currKeyCounter = 0;
            string apiname = "neutrino";
            string responseNtrno = "";
            //becuse of that i cannot figure out how to send API creds as previous patterns, name of this function decleared like this
            List<string> tempRow = new List<string>();
            //now create a client handler which uses that proxy
            while (currKeyCounter < NTAPI.Count) {
                if (NTAPI[currKeyCounter].state == false) {
                    currKeyCounter++;
                    continue;
                }
                var httpClientHandler = new HttpClientHandler() { Proxy = myProxySetting, };
                try {
                    using (var client = new HttpClient(handler: httpClientHandler, disposeHandler: true)) {
                        var req = new List<KeyValuePair<string, string>>();
                        req.Add(new KeyValuePair<string, string>("user-id", NTAPI[currKeyCounter].id));
                        req.Add(new KeyValuePair<string, string>("api-key", NTAPI[currKeyCounter].pass));
                        req.Add(new KeyValuePair<string, string>("ip", IP));

                        var content = new FormUrlEncodedContent(req);
                        var response = client.PostAsync("https://neutrinoapi.com/ip-blocklist", content).Result;
                        responseNtrno = response.Content.ReadAsStringAsync().Result;
                    }
                }
                catch (Exception e) {
                    if (e.Message.Contains("The given key was not present in the dictionary") == true) {
                        NTAPI[currKeyCounter].state = false;
                        continue;
                    }
                }
                if (responseNtrno.Contains("error") && responseNtrno.Contains("code")) {
                    printers[counter].Add(Helpers.printBad(apiname, "error", responseNtrno.ToString()));
                }
                else if (responseNtrno == null)
                    printers[counter].Add(Helpers.printBad(apiname, "null returned", responseNtrno.ToString()));
                else {
                    try {
                        //process the response
                        JsonObject retNtrnoJson = (JsonObject)JsonValue.Parse(responseNtrno);
                        //add response values to row cell > blocklisted 
                        tempRow.Add(retNtrnoJson["list-count"].ToString() + " | "
                            + String.Join("", retNtrnoJson["blocklists"].ToString().Split('[', ']', '"')));
                        printers[counter].Add(Helpers.printOK(apiname));
                        return tempRow;
                    }
                    catch (Exception e) {
                        printers[counter].Add(Helpers.printBad(apiname, "exception when parsing", e.Message));
                        if (e.Message.Contains("The given key was not present in the dictionary") == true) {
                            NTAPI[currKeyCounter].state = false;
                            continue;
                        }
                    }
                }
                return new List<string>() { "?" };
            }
            return new List<string>() { "!KeyLimit!" };
        }
    }
}
