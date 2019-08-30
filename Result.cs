using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Json;

namespace Baykus
{
    public class Result {
        // you can think this holds all raw info in output files
        // !!! create an "extra" variable to hold extra infos
        public int orderNo;
        public bool isCompleted;
        public string ip;
        public SubResult ibmResult;   // error,country,asn,severity
        public SubResult ip_apiResult;// error,country,org,"!NA!"
        public SubResult mdResult;    // error,blacklist,"!NA!","!NA!"
        public SubResult ntResult;    // error,blocklist,"!NA!","!NA!"

        public Result(int orderNo = -1, string ip = "!NA!", string ibmRaw = "!NA!", string ipapiRaw = "!NA!", string mdRaw = "!NA!", string ntRaw = "!NA!") {
            this.orderNo = orderNo;
            this.ip = ip;
            this.isCompleted = false;
            ibmResult = new SubResult("ibm-xforce");
            ip_apiResult = new SubResult("ip-api");
            mdResult = new SubResult("metadefender");
            ntResult = new SubResult("neutrino");

            // this funcs fills proper fields in SubResult class: apiname,country,organization,severity,error,errorMess
            // blocklist and blacklists saved in country variable
            ResolveIbmData(ibmRaw);
            ResolveIp_apiData(ipapiRaw);
            ResolveMdData(mdRaw);
            ResolveNtData(ntRaw);

        }
        private void ResolveNtData(string ntRaw)
        {
            if (ntRaw == null) {
                ntResult.error = "null returned";
                ntResult.errorMess = ntRaw.ToString();
            }
            else if (ntRaw.Contains("error") && ntRaw.Contains("code")) {
                ntResult.error = "mderror";
                ntResult.errorMess = ntRaw.ToString();
            }
            else {
                try {
                    JsonObject retNtrnoJson = (JsonObject)JsonValue.Parse(ntRaw);
                    //add response values to row cell > blocklisted 
                    ntResult.country =
                        retNtrnoJson["list-count"].ToString() + " | " + String.Join("", retNtrnoJson["blocklists"].ToString().Split('[', ']', '"'));
                }
                catch (Exception e) {
                    ntResult.error = e.Message.Contains("The given key was not present in the dictionary") ? "KeyLimit" : "exception";
                    ntResult.errorMess = e.Message + "\n\t" + ntResult;
                }
            }
        }
        private void ResolveMdData(string mdRaw){
            if (mdRaw == null) {
                mdResult.error = "null returned";
                mdResult.errorMess = mdRaw.ToString();
            }
            else if (mdRaw.Contains("error") && mdRaw.Contains("code")) {
                mdResult.error = "mderror";
                mdResult.errorMess = mdRaw.ToString();
            }
            else {
                try {
                    dynamic responseJson = JsonConvert.DeserializeObject(mdRaw);
                    /// ----
                    mdResult.country = responseJson.detected_by.ToString() + " |";
                    foreach (var item in responseJson.scan_results)
                        if (item.results.First.result.ToString() == "blacklisted")
                            mdResult.country += ", " + item.source.ToString();
                    /// ---- if we can make this area generalized, we can generalize whole Resolve operations
                }
                catch (Exception e) {
                    mdResult.error = e.Message.Contains("Forbidden") ? "KeyLimit" : "exception";
                    mdResult.errorMess = e.Message + "\n\t" + mdRaw;
                }
            }
        }
        private void ResolveIp_apiData(string ip_apiRaw)
        {
            if (ip_apiRaw.Contains("error")) {
                ip_apiResult.error = "ip-apierror";
                ip_apiResult.errorMess = ip_apiRaw.ToString() ?? "null";
            }
            else if (ip_apiRaw == null) {
                ip_apiResult.error = "null returned";
                ip_apiResult.errorMess = ip_apiRaw.ToString() ?? "null";
            }
            else {
                try {
                    // isp,asn = organization
                    dynamic responseJson = JsonConvert.DeserializeObject(ip_apiRaw);
                    ip_apiResult.organization = responseJson.isp.ToString();
                    ip_apiResult.country = responseJson.country.ToString() + ", " + responseJson.countryCode.ToString();
                }
                catch (Exception e) {
                    ip_apiResult.error = e.Message.Contains("Forbidden") ? "KeyLimit" : "exception";
                    ip_apiResult.errorMess = e.Message + "\n\t" + ip_apiRaw;
                }
            }
        }
        private void ResolveIbmData(string ibmRaw)
        {
            if (ibmRaw.Contains("error") && ibmRaw.Contains("code")) {
                ibmResult.error = "ibmerror";
                ibmResult.errorMess = ibmRaw.ToString();
            }
            else if (ibmRaw == null) {
                ibmResult.error = "null returned";
                ibmResult.errorMess = ibmRaw.ToString();
            }
            else {
                try {
                    // process the response
                    dynamic responceJson = JsonConvert.DeserializeObject(ibmRaw);
                    ibmResult.country = responceJson.geo.country.ToString() ?? "??";
                    ibmResult.severity = responceJson.score.ToString() ?? "??";
                    var asns = responceJson.history.Last.asns;
                    // because of that we do not know the number that represents asns, foreach necessarry. Look data resultIBM.json 
                    try {
                        foreach (var tAsn in asns){
                            var tOrganization = tAsn.First.Company.ToString();
                            if (tOrganization.LastIndexOf("-") != -1) {
                                tOrganization = tOrganization.Substring(0, tOrganization.LastIndexOf("-"));
                                int len = tOrganization.Length - 4 >= 0 ? tOrganization.Length : 0;
                                tOrganization = tOrganization.Substring(0, len);
                            }
                            ibmResult.organization = tOrganization;
                            break;
                        }
                    }
                    catch {
                        ibmResult.error = "asnExcepton";
                        ibmResult.errorMess = "ASNS,company";
                    }
                }
                catch (Exception e){
                    ibmResult.errorMess = e.Message + "\n\t" + ibmRaw;
                    ibmResult.error = e.Message.Contains("Forbidden") ? "KeyLimit" : "exception";
                }
            }
        }
        public override string ToString() {
            return
                " [" + ip.PadRight(20) + "] " +
                ibmResult.country.PadRight(10) +
                ibmResult.organization.PadRight(15) +
                ibmResult.severity.PadRight(5) +
                ip_apiResult.country.PadRight(10) +
                ip_apiResult.organization.PadRight(15) +
                mdResult.country.PadRight(15) +
                ntResult.country.PadRight(15);
        }
        public void DashPrint() {
            var allResults = new List<SubResult>() {ibmResult,ip_apiResult,mdResult,ntResult };
            foreach(SubResult result in allResults) {
                if (result.error != "!NA!")
                    MainClass.printers[orderNo].Add(Helpers.printBad(result.apiName, result.error, result.errorMess));
                else
                    MainClass.printers[orderNo].Add(Helpers.printOk(result.apiName));
            }
        }
        public string ToBaseString() {
            return
                ibmResult.country.PadRight(10) +
                ibmResult.organization.PadRight(15) +
                ibmResult.severity.PadRight(5) +
                ip_apiResult.country.PadRight(10) +
                ip_apiResult.organization.PadRight(15) +
                mdResult.country.PadRight(15) +
                ntResult.country.PadRight(15);
        }
        public string[] ToArray()
        {
            return ibmResult.Concat(ipapiResult).Concat(mdResult).Concat(ntResult).ToArray();
        }
    }
}
