using System;
using System.Text;
using System.Text.RegularExpressions;
namespace NAppUpdate.Updater
{

	public class ArgumentsParser
	{
		public bool HasArgs { get; private set; }
        public string ProcessName { get; private set; }
		public bool ShowConsole { get; private set; }
		public bool Log { get; private set; }
		public string CallingApp { get; set; }

        private static ArgumentsParser _instance;
        protected ArgumentsParser()
        {
        }

        public static ArgumentsParser Get()
        {
        	return _instance ?? (_instance = new ArgumentsParser());
        }

		public ArgumentsParser(string[] args)
		{
            Parse(args);
        }

        public void ParseCommandLineArgs()
        {
            Parse(Environment.GetCommandLineArgs());
        }

	    public string DumpArgs()
	    {
	        var sb = new StringBuilder();
            sb.AppendLine(string.Format("HasArgs: '{0}'", HasArgs));
            sb.AppendLine(string.Format("ProcessName: '{0}'", ProcessName));
            sb.AppendLine(string.Format("ShowConsole: '{0}'", ShowConsole));
            sb.AppendLine(string.Format("Log: '{0}'", Log));
            sb.AppendLine(string.Format("CallingApp: '{0}'", CallingApp));
	        return sb.ToString();
	    }

        public void Parse(string[] args)
        {
            for (int i=0; i < args.Length; i++)
            {
                string arg = args[i];

                // Skip any args that are our own executable (first arg should be this).
                // In Visual Studio, the arg will be the VS host starter instead of
                // actually ourself.
                if (arg.Equals(System.Reflection.Assembly.GetEntryAssembly().Location, StringComparison.InvariantCultureIgnoreCase)
                    || arg.EndsWith(".vshost.exe", StringComparison.InvariantCultureIgnoreCase))
                {
                	CallingApp = arg.ToLower().Replace(".vshost.exe", string.Empty);
                	continue;
                }

            	arg = CleanArg(arg);
				if (arg == "log") {
					this.Log = true;
					this.HasArgs = true;
				} else if (arg == "showconsole") {
					this.ShowConsole = true;
					this.HasArgs = true;
				//} else if (this.ProcessName == null) { //first time we will assign file name instead of process name! In windows we don't care, but under linux this will do the difference.
				} else {
                    // if we don't already have the processname set, assume this is it
                    this.ProcessName = args[i];
                }
			}
		}

		private static string CleanArg(string arg)
		{
			const string pattern1 = "^(.*)([=,:](true|0))";
			arg = arg.ToLower();
			if (arg.StartsWith("-") || arg.StartsWith("/")) {
				arg = arg.Substring(1);
			}
			Regex r = new Regex(pattern1);
			arg = r.Replace(arg, "{$1}");
			return arg;
		}
	}
}
