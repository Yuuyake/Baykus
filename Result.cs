using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baykus
{
    public class Result {
        // you can think this holds all raw info in output files
        public int orderNo;
        public bool isCompleted;
        public string ip;
        public List<string> ibmResult;   // error,country,asn,severity
        public List<string> ip_apiResult;// error,country,org,"NA"
        public List<string> mdResult;    // error,blacklist,"NA","NA"
        public List<string> ntResult;    // error,blocklist,"NA","NA"

        public Result(int orderNo = -1, string ip = "NA", string ibmRaw = "NA", string ipapiRaw = "NA", string mdRaw = "NA", string ntRaw = "NA") {
            // IBM, MD vs hepsi ayrı ayrı resolve ve sonuçları buraya getir
            this.orderNo = orderNo;
            this.ip = ip;
            this.isCompleted = false;

            ibmResult   = ResolveIbmData(ibmRaw);
            ipapiResult = ResolveIpapiData(ipapiRaw);
            mdResult    = ResolveMdData(mdRaw);
            ntResult    = ResolveNtData(ntRaw);
        }
        private List<string> ResolveNtData(string ntRaw)
        {
            throw new NotImplementedException();
        }
        private List<string> ResolveMdData(string mdRaw)
        {
            throw new NotImplementedException();
        }
        private List<string> ResolveIp_apiData(string ipapiRaw)
        {
            // FINDDDDDDDDDDD   error, country, org
            string country  = "NA";
            string asn      = "NA";
            string severity = "NA";
            string error    = "NA";
            string apiName  = "ip-api";
            if (ipapiRaw.Contains("error"))
                MainClass.printers[orderNo].Add(Helpers.printBad(apiName, "error", ipapiRaw));
            else if (ipapiRaw == null)
                MainClass.printers[orderNo].Add(Helpers.printBad(apiName, "null returned", ipapiRaw));
            else
            {
                try
                {
                    // process the response if no failure exists
                    dynamic retIP_APIjson = JsonConvert.DeserializeObject(ipapiRaw);
                    // add values to row > country+region,organization
                    tempRow.Add(retIP_APIjson.isp.ToString());
                    tempRow.Add(retIP_APIjson.country.ToString() + ", " + retIP_APIjson.countryCode.ToString());
                    MainClass.printers[orderNo].Add(Helpers.printOk(apiName));
                    return tempRow;
                }
                catch (Exception e) {
                    MainClass.printers[orderNo].Add(Helpers.printBad(apiName, "exception when parsing", e.Message));
                }
            }
            return new List<string>() { error, country, asn, severity }; // if any fail happens, instruction comes to here to return properly
        }
        /// <summary>
        /// returns 4 sized string list: country,asn(organization),severity,error
        /// </summary>
        /// <param name="ibmRaw"></param>
        /// <returns></returns>
        private List<string> ResolveIbmData(string ibmRaw)
        {
            string country  = "NA";
            string asn      = "NA";
            string severity = "NA";
            string error    = "NA";
            string apiName = "ibm-xforce";
            if (ibmRaw.Contains("error") && ibmRaw.Contains("code"))
            {
                error = "ibmerror";
                MainClass.printers[orderNo].Add(Helpers.printBad(apiName, error, ibmRaw.ToString()));
            }
            else if (ibmRaw == null)
            {
                error = "null returned";
                MainClass.printers[orderNo].Add(Helpers.printBad(apiName, error, ibmRaw.ToString()));
            }
            else
            {
                try
                {
                    // process the response
                    dynamic retIBMjson = JsonConvert.DeserializeObject(ibmRaw);
                    country = retIBMjson.geo.country.ToString() ?? "??";
                    severity = retIBMjson.score.ToString() ?? "??";
                    var asns = retIBMjson.history.Last.asns;
                    // because of that we do not know the number that represents asns, foreach necessarry. Look data resultIBM.json 
                    try
                    {
                        foreach (var tAsn in asns)
                        {
                            asn = tAsn.First.Company.ToString();
                            if (asn.LastIndexOf("-") != -1) {
                                asn = asn.Substring(0, asn.LastIndexOf("-"));
                                int len = asn.Length - 4 >= 0 ? asn.Length : 0;
                                asn = asn.Substring(0, len);
                            }
                            break;
                        }
                    }
                    catch
                    {
                        asn = "??";
                        error = "asnExcepton";
                        MainClass.printers[orderNo].Add(Helpers.printBad(apiName, "exception when parsing", "ASNS,company"));
                    }
                    MainClass.printers[orderNo].Add(Helpers.printOk(apiName));
                }
                catch (Exception e)
                {
                    MainClass.printers[orderNo].Add(Helpers.printBad(apiName, "exception when parsing", e.Message));
                    if (e.Message.Contains("Forbidden"))
                    {
                        MainClass.ibmApiKeys[currKeyCounter].state = "KeyLimit";
                        error = "KeyLimit";
                    }
                    else
                        error = "exception";

                }
            }
            return new List<string>() { error, country, asn, severity }; // if any fail happens, instruction comes to here to return properly
        }
        public override string ToString() {
            return
                " [" + ip.PadRight(20) + "] " +
                ibmResult[1].PadRight(10) +
                ibmResult[2].PadRight(15) +
                ibmResult[3].PadRight(5) +
                ipapiResult[1].PadRight(10) +
                ipapiResult[2].PadRight(15) +
                mdResult[1].PadRight(15) +
                ntResult[1].PadRight(15);
        }
        public string DashPrint() {
            return
                ibmResult[1].PadRight(10) +
                ibmResult[2].PadRight(15) +
                ibmResult[3].PadRight(5) +
                ipapiResult[1].PadRight(10) +
                ipapiResult[2].PadRight(15) +
                mdResult[1].PadRight(15) +
                ntResult[1].PadRight(15);
        }

        public string[] ToArray()
        {
            return ibmResult.Concat(ipapiResult).Concat(mdResult).Concat(ntResult).ToArray();
        }
    }
}
