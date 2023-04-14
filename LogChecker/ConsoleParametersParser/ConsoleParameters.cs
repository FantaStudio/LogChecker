using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogChecker.ConsoleParametersParser
{
    public class ConsoleParameters
    {
        public string DirectoryPath { get; set; }

        public bool IsIgnoreTimeoutException { get; set; } = false;
    }
}
