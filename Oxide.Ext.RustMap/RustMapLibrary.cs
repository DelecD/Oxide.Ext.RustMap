using System;
using Oxide.Core.Libraries;

namespace Oxide.Ext.RustMap
{
	public class RustMapLibrary : Library
	{
		public RustMapLibrary(RustMapExtension ex)
		{
			this.ext = ex;
		}

		private RustMapExtension ext;
	}
}