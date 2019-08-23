using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashChecker {
    public class Result {
        // you can think this holds all raw info in output files
        public int orderNo;
        public string ip;
        public string ibmCountry;
        public string ibmAsn;
        public string ibmSeverity;
        public string ipapiOrgz;
        public string ipapiCountry;
        public string mdBlacklist;
        public string ntBlocklist;
        public bool isCompleted = false;
        public Result(int orderNo, string ip, string ibmRaw, string mdRaw, string ntRaw, string ipapiRaw) {
            // IBM, MD vs hepsi ayrı ayrı resolve ve sonuçları buraya getir
            this.orderNo = orderNo;
            this.ip = ip;

            this.ibmCountry  = ResolveIbmData(ibmRaw)[0];
            this.ibmAsn      = ResolveIbmData(ibmRaw)[1];
            this.ibmSeverity = ResolveIbmData(ibmRaw)[2];

            this.ipapiOrgz    = ResolveIpapiData(ipapiRaw)[1];
            this.ipapiCountry = ResolveIpapiData(ipapiRaw)[2];

            this.mdBlacklist = ResolveMdData(mdRaw)[0];
            this.ntBlocklist = ResolveNtData(ntRaw)[0]; 
        }

        public override string ToString() {
            return
                " [" + ip.PadRight(20) + "] " +
                ibmCountry.PadRight(10) +
                ibmAsn.PadRight(15) +
                ibmSeverity.PadRight(5) +
                ipapiCountry.PadRight(10) +
                ipapiOrgz.PadRight(15) +
                mdBlacklist.PadRight(15) +
                ntBlocklist.PadRight(15);
        }
        public string DashPrint() {
            return
                ibmCountry.PadRight(10) +
                ibmAsn.PadRight(15) +
                ibmSeverity.PadRight(5) +
                ipapiCountry.PadRight(10) +
                ipapiOrgz.PadRight(15) +
                mdBlacklist.PadRight(15) +
                ntBlocklist.PadRight(15);
        }
    }
}
