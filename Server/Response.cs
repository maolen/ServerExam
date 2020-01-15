using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class Response
    {
        public string Action { get; set; }
        public string Path { get; set; }
        public string Value { get; set; }
        public string NewData { get; set; }
    }
}
