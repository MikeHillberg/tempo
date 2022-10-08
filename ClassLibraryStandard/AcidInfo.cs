using System;
using System.Collections.Generic;

namespace Tempo
{

    public class AcidInfo
    { 
        public AcidInfo()
        {
            DllPath = String.Empty;
            TrustLevel = String.Empty;
            TrustLevelValue = Tempo.TrustLevel.Unset;
            ActivationType = String.Empty;
            Threading = String.Empty;
        } 

        public string DllPath { get; set; }
        public string TrustLevel { get; set; }
        public TrustLevel TrustLevelValue { get; set; }
        public string Clsid { get; set; }
        public string ActivationType { get; set; }
        public string Threading { get; set; }

        static public Dictionary<TypeViewModel, AcidInfo> AcidMap = new Dictionary<TypeViewModel, AcidInfo>();

    }

}
