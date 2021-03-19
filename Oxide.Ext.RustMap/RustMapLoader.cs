using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

namespace Oxide.Ext.RustMap
{
	public class RustMapLoader : PluginLoader
	{
		public static RustMap rm;
		public static Logger logger;
		public RustMapLoader(RustMapExtension ext)
		{
			RustMapLoader.rm = new RustMap();
			RustMapLoader.logger = Interface.GetMod().RootLogger;
		}

		public override void Unloading(Plugin plugin)
		{
		}

		public override Plugin Load(string directory, string name)
		{
			this.LoadedPlugins["RustMap"] = RustMapLoader.rm;
			return RustMapLoader.rm;
		}

		public override IEnumerable<string> ScanDirectory(string directory)
		{
			return (IEnumerable<string>)new string[]
			{
				"RustMapPlugin"
			};
		}
	}
}
