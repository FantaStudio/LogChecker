using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogChecker.ConsoleParametersParser
{
    public class ConsoleParametersBuilder
    {
        private readonly string[] _args;

        private readonly ConsoleParameters _consoleParameters;

        public ConsoleParametersBuilder(string[] args)
        {
            _args = args;
            _consoleParameters = new ConsoleParameters();
        }

        public ConsoleParametersBuilder AddDirectoryPath()
        {
            if(_args.Length > 0)
            {
                var pathIndex = Array.IndexOf(_args, "-p");
                if(pathIndex >= 0 && pathIndex != _args.Length)
                {
                    _consoleParameters.DirectoryPath = _args[pathIndex + 1];
                }
            }

            return this;
        }

        public ConsoleParametersBuilder AddIgnoreTimeoutException()
        {
            if (_args.Length > 0)
            {
                var pathIndex = Array.IndexOf(_args, "-i");
                if (pathIndex >= 0 && pathIndex != _args.Length)
                {
                    _consoleParameters.IsIgnoreTimeoutException = true;
                }
            }

            return this;
        }

        public ConsoleParameters Build()
        {
            return _consoleParameters;
        }
    }
}
