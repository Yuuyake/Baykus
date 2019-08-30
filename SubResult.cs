using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baykus {
    public class SubResult {
        public string country   = "!NA!";
        public string organization = "!NA!";
        public string severity  = "!NA!";
        public string errorMess = "!NA!";
        public string error     = "!NA!";
        public string apiName   = "ip-api";
        public SubResult(string apiName = "!NA!", string country = "!NA!", string organiz = "!NA!", string severity = "!NA!", string error = "!NA!", string errorMess = "!NA!") {
            this.apiName  = apiName;
            this.country  = country;
            this.organization = organiz;
            this.severity = severity;
            this.error    = error;
            this.errorMess = errorMess;
        }
        public string[] ToArray() {
            return new string[]{ };
        }
    }
}
