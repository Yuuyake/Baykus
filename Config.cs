using System.Collections.Generic;
using Newtonsoft.Json;

namespace BAYKUS {
    public partial class Config {
        [JsonProperty("proxyAdress")]
        public string proxyAdress { get; set; }

        [JsonProperty("proxyUsername")]
        public string proxyUsername { get; set; }

        [JsonProperty("proxyPassword")]
        public string proxyPassword { get; set; }

        [JsonProperty("csirtMail")]
        public string csirtMail { get; set; }

        [JsonProperty("altyapiMail")]
        public string altyapiMail { get; set; }

        [JsonProperty("atarMail")]
        public string atarMail { get; set; }

        [JsonProperty("atarTitle")]
        public string atarTitle { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("ibmApiKeys")]
        public List<VtApiKey> ibmApiKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("ntApiKeys")]
        public List<VtApiKey> ntApiKeys { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("mtApiKeys")]
        public List<VtApiKey> mtApiKeys { get; set; }
    }

    public partial class VtApiKey {
        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("pass")]
        public string pass { get; set; }
    }
}
