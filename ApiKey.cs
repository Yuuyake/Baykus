using System;
using System.Linq;
using System.Text;

namespace Baykus
{
    public class ApiKey {
        public string id;
        public string pass;
        public string state;
        public int lastUsage;
        public int index;
        public int usageLeft;
        public ApiKey(string id, string pass, int index, string state) {
            this.id        = id;
            this.pass      = pass;
            this.index     = index;
            this.state     = state;
            this.usageLeft = 100;
        }
    }
}
