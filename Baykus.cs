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
|      TODO:
           > API keyleri ve validasyınu düzenle, Hash Checkerdaki gibi
           > rapor dosyasını plain table 3 yap
           > sonucu blissadmin kutucuğuna bas ( https://stackoverflow.com/questions/4015324/how-to-make-http-post-web-request  FLURRRR )
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
using System.Text;
using System.Net.Http;
using System.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using Console = Colorful.Console;
using System.Security;
using Baykus.Properties;

namespace Baykus {

    public class MainClass {
        static public string userName;
        static WebProxy myProxySetting = null;
        //here our API key-value pairs, there is at least 3 key for each API will be set in Helpers.cs file
        static public List<ApiKey> ibmApiKeys;
        static public List<ApiKey> ntApiKeys;
        static public List<ApiKey> mtApiKeys;
        static public List<Result> results  = new List<Result>(); // rows that will be written on a excel file
        static public List<List<string>> printers = new List<List<string>>(); // print out to user what happening
        static public Config speconfig;
        // ==========================================   MAIN FUNC  ===============================================
        //
        static void Main(string[] args) {
            var largestW = Console.LargestWindowWidth / 2;
            string banner = 
                " ".PadRight(largestW) +
                String.Concat(Resources.banner.Split('\n').Select(ss => ss = ss +
                " ".PadRight(largestW - ss.Length) + '\n')).Replace("\r", "") +
                " ".PadRight(largestW) + '\n';
            Console.Title = "BAYKUS";
            Console.OutputEncoding = Encoding.UTF8;
            Console.ForegroundColor = Color.LightGray;
            Console.SetWindowSize(largestW, Console.LargestWindowHeight -1);
            Console.WriteFormatted(banner, Color.WhiteSmoke);

            try { speconfig = JsonConvert.DeserializeObject<Config>(Resources.speconfig); }
            catch (System.Exception ee) {
                Console.WriteFormatted("\n Config file problem:\n\t" + ee.Message, Color.Red);
                Console.ReadLine();
                return;
            }

            Helpers.SetApiKeys();
            List<string> IPs = Helpers.getReportedIPs();// read reported IPs from csv
            myProxySetting = initializeProxyConfigs();
            AsyncRequests(IPs);                         // this will make all needed requests from all APIs asynchronously
            results = results.OrderBy(ss => ss.ipapiResult[2]).ToList();
            Helpers.writeRowsToExcel(results);             // responses wrote to the rows List variable, write them to report excel
            Helpers.WriteSomeMail(results);
            Helpers.WriteAtarMail(results);
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
                Console.Write("Setting proxy to \"" + speconfig.proxyAdress + "\"");
                //System.Environment.Exit(1);
                myProxySetting.Address = new Uri(speconfig.proxyAdress);
            }
            // Setting User Creds to pass proxy
            userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\')[1];
            userName = ( userName.Length < 3 || userName.Length > 10 ) == true ? "unknown" : userName;

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
                if (
                    ipParts[0] == 10 
                    || (ipParts[0] == 192 && ipParts[1] == 168) 
                    || (ipParts[0] == 172 && (ipParts[1] >= 16 && ipParts[1] <= 31))) 
                {
                    results.Add(new Result(counter+1,IP, " ", "PRIVATE IP RANGE", " ", " "));
                    continue;
                }
                int myC = counter;
                //ASYNC = oneIpRequest(IP, myC);
                var eachTask = Task.Factory.StartNew(() => OneIpRequests(IP, myC));
                allReq.Add(eachTask);
                counter++;
                Thread.Sleep(500);
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
        static Result OneIpRequests(string ip,int counter) {
            Result tResult = new Result(counter, ip);
            string rawIbm    = "NA";
            string rawIpapi  = "NA";
            string rawIp_api = "NA";
            string rawMd     = "NA";
            string rawNt  = "NA";

            //get api RESPONSES from requests asynchronous, choose whatever  
            //var taskIPAPI = Task.Run(() => rawIpapi  = makeRequestIpapi(IP,counter));
            var taskIP_API = Task.Run(() => rawIp_api = makeRequestIp_api(ip, counter));
            var taskIBM    = Task.Run(() => rawIbm    = makeRequestIBM(ip, counter));
            var taskMD     = Task.Run(() => rawMd     = makeRequestMDef(ip, counter));
            var taskNTRNO  = Task.Run(() => rawNt     = makeRequestNtrno(ip, counter));

            // await makes execution block for further lines
            Task.WaitAll(taskIBM, taskNTRNO, taskMD, taskIP_API);
            tResult = new Result(counter, ip, rawIbm, rawIpapi, rawMd, rawNt);

            lock (results) {
                // add data to the list of rows that will be written to the output CSV
                results.Add(tResult);
                //rows.Last().AddRange(responseIPAPI);
                //select one of the other IP location API, if the one fails go for the other. 
                Helpers.printStat(printers[counter], counter);
            }
            return tResult;
        }
        /// <summary>
        /// see\Resources\resultIp_api.json
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        static string makeRequestIp_api(string IP, int counter) {
            string apiName = "ip-api";
            List<string> tempRow = new List<string>();
            string responseIP_API = "NA";
            try {   // create request , read response
                HttpWebRequest requestIP_API = (HttpWebRequest)WebRequest.Create("http://ip-api.com/json/" + IP);
                requestIP_API.Proxy = myProxySetting;
                using (HttpWebResponse response = (HttpWebResponse)requestIP_API.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream)) { responseIP_API = reader.ReadToEnd(); }
            }
            catch (Exception e) {
                if (e.Message.Contains("Proxy Authentication Required"))
                    return responseIP_API; // with this return value checked, blocks this app to continue 
                else {
                    printers[counter].Add(Helpers.printBad(apiName, "exception", e.Message));
                    if (e.Message.Contains("Forbidden")) {
                        DUZENLEEEEEEEEEE
                        return responseIP_API;
                    }
                }
            }
            return responseIP_API;
        }
        /// <summary>
        /// see\Resources\resultIpapi.json
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        static List<string> makeRequestIpapi(string IP, int counter) {
            string apiName = "ipapi";
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
                printers[counter].Add(Helpers.printBad(apiName, "exception", e.Message));
                return new List<string>() { "?", "?" };
            }
            if (responseIPAPI.Contains("error"))
                printers[counter].Add(Helpers.printBad(apiName, "error", responseIPAPI.ToString()));
            else if (responseIPAPI == null)
                printers[counter].Add(Helpers.printBad(apiName, "null returned", responseIPAPI.ToString()));
            else {
                try {
                    // process the response if no failure exists
                    dynamic retIPAPIjson = JsonConvert.DeserializeObject(responseIPAPI);
                    // add values to row > country+region,organization
                    tempRow.Add(retIPAPIjson.org.ToString());
                    tempRow.Add(retIPAPIjson.country_name.ToString() + ", " + retIPAPIjson.region.ToString());
                    printers[counter].Add(Helpers.printOk(apiName));
                    return tempRow;
                }
                catch (Exception e) {
                    printers[counter].Add(Helpers.printBad(apiName, "exception when parsing", e.Message));
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
        static string makeRequestIBM(string IP, int counter) {
            int currKeyCounter = 0;
            string responseIBM = "";
            List<string> tempRow = new List<string>();
            while (currKeyCounter < ibmApiKeys.Count) {
                if (ibmApiKeys[currKeyCounter].state == "KeyLimit") {
                    currKeyCounter++;
                    continue;
                }
                try {   // create request , read response
                    HttpWebRequest requestIBM = (HttpWebRequest)WebRequest.Create("https://api.xforce.ibmcloud.com/ipr/" + IP);
                    requestIBM.Proxy = myProxySetting;
                    string apicreds = ibmApiKeys[currKeyCounter].id + ":" + ibmApiKeys[currKeyCounter].pass;
                    requestIBM.Headers.Add("Authorization", "Basic " + System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(apicreds)));
                    using (HttpWebResponse response = (HttpWebResponse)requestIBM.GetResponse()) {
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                            responseIBM = reader.ReadToEnd();
                    }
                    if(responseIBM.Contains("402") && responseIBM.Contains("error")) {
                        ibmApiKeys[currKeyCounter].state = "KeyLimit";
                        continue;
                    }
                }
                catch (Exception e) {
                    printers[counter].Add(Helpers.printBad("ibm-xforce", "exception", e.Message));
                }
                /////////////
                return responseIBM;
            }
            return responseIBM;
        }
        /// <summary>
        /// see\Resources\resultMD.json
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        static List<string> makeRequestMDef(string IP, int counter) {
            int currKeyCounter = 0;
            string apiName = "metadefender";
            string responseMDef = "";

            while (currKeyCounter < mtApiKeys.Count) {
                if (mtApiKeys[currKeyCounter].state == "KeyLimit") {
                    currKeyCounter++;
                    continue;
                }
                List<string> tempRow = new List<string>();
                try {// create request , read response
                    HttpWebRequest requestMETADEF = (HttpWebRequest)WebRequest.Create("http://api.metadefender.com/v1/scan/" + IP);
                    requestMETADEF.Proxy = myProxySetting;
                    requestMETADEF.Headers.Add("apikey: " + mtApiKeys[currKeyCounter].pass);
                    using (HttpWebResponse response = (HttpWebResponse)requestMETADEF.GetResponse()) {
                        using (Stream stream = response.GetResponseStream()) {
                            using (StreamReader reader = new StreamReader(stream)) { responseMDef = reader.ReadToEnd(); }
                        }
                    }
                }
                catch (Exception e) {
                    printers[counter].Add(Helpers.printBad(apiName, "exception", e.Message));
                    if (e.Message.Contains("Forbidden")) {
                        mtApiKeys[currKeyCounter].state = "KeyLimit";
                        continue;
                    }
                }
                if (responseMDef.Contains("error") && responseMDef.Contains("code"))
                    printers[counter].Add(Helpers.printBad(apiName, "error", responseMDef));
                else if (responseMDef == null)
                    printers[counter].Add(Helpers.printBad(apiName, "null returned", responseMDef.ToString()));
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
                        printers[counter].Add(Helpers.printOk(apiName));
                        return tempRow;
                    }
                    catch (Exception e) {
                        printers[counter].Add(Helpers.printBad(apiName, "exception when parsing", e.Message));
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
            string apiName = "neutrino";
            string responseNtrno = "";
            //becuse of that i cannot figure out how to send API creds as previous patterns, name of this function decleared like this
            List<string> tempRow = new List<string>();
            //now create a client handler which uses that proxy
            while (currKeyCounter < ntApiKeys.Count) {
                if (ntApiKeys[currKeyCounter].state == "KeyLimit") {
                    currKeyCounter++;
                    continue;
                }
                var httpClientHandler = new HttpClientHandler() { Proxy = myProxySetting, };
                try {
                    using (var client = new HttpClient(handler: httpClientHandler, disposeHandler: true)) {
                        var req = new List<KeyValuePair<string, string>>();
                        req.Add(new KeyValuePair<string, string>("user-id", ntApiKeys[currKeyCounter].id));
                        req.Add(new KeyValuePair<string, string>("api-key", ntApiKeys[currKeyCounter].pass));
                        req.Add(new KeyValuePair<string, string>("ip", IP));

                        var content = new FormUrlEncodedContent(req);
                        var response = client.PostAsync("https://neutrinoapi.com/ip-blocklist", content).Result;
                        responseNtrno = response.Content.ReadAsStringAsync().Result;
                    }
                }
                catch (Exception e) {
                    if (e.Message.Contains("The given key was not present in the dictionary") == true) {
                        ntApiKeys[currKeyCounter].state = "KeyLimit";
                        continue;
                    }
                }
                if (responseNtrno.Contains("error") && responseNtrno.Contains("code")) {
                    printers[counter].Add(Helpers.printBad(apiName, "error", responseNtrno.ToString()));
                }
                else if (responseNtrno == null)
                    printers[counter].Add(Helpers.printBad(apiName, "null returned", responseNtrno.ToString()));
                else {
                    try {
                        //process the response
                        JsonObject retNtrnoJson = (JsonObject)JsonValue.Parse(responseNtrno);
                        //add response values to row cell > blocklisted 
                        tempRow.Add(retNtrnoJson["list-count"].ToString() + " | "
                            + String.Join("", retNtrnoJson["blocklists"].ToString().Split('[', ']', '"')));
                        printers[counter].Add(Helpers.printOk(apiName));
                        return tempRow;
                    }
                    catch (Exception e) {
                        printers[counter].Add(Helpers.printBad(apiName, "exception when parsing", e.Message));
                        if (e.Message.Contains("The given key was not present in the dictionary") == true) {
                            ntApiKeys[currKeyCounter].state = "KeyLimit";
                            continue;
                        }
                    }
                }
                return new List<string>() { "?" };
            }
            return new List<string>() { "KeyLimit" };
        }
    }
}