using System;
using Oxide.Core;
using Oxide.Core.Extensions;

namespace Oxide.Ext.RustMap
{
	public class RustMapExtension : Extension
	{
		public override string Name
		{
			get
			{
				return "RustMap";
			}
		}

		public override VersionNumber Version
		{
			get
			{
				return new VersionNumber(1, 1, 0);
			}
		}

		public override string Author
		{
			get
			{
				return "Alez";
			}
		}

		public RustMapExtension(ExtensionManager em) : base(em)
		{
		}

		public override void Load()
		{
			base.Manager.RegisterPluginLoader(new RustMapLoader(this));
			base.Manager.RegisterLibrary("RustMap", new RustMapLibrary(this));
		}

		public override void OnShutdown()
		{
		}
	}
}