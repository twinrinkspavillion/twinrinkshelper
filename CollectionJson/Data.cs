using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DynamicUtils;
using Newtonsoft.Json.Linq;

namespace CollectionJson
{
    [DebuggerDisplay("Nee = {Name}, Value= {Value}")]
    [DataContract]
    public class Data : ExtensibleObject
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "value")]
        public JToken Value { get; set; }

        [DataMember(Name = "prompt")]
        public string Prompt { get; set; }

    }
}
