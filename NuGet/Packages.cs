using System;

namespace NuGet
{
	public static class Packages
	{
		public static void References()
		{
			_ = typeof(IBM.Data.DB2.DB2Connection);
//			_ = typeof(IBM.Data.Informix.IfxConnection);
			_ = typeof(AdoNetCore.AseClient.AseConnection);
			_ = typeof(Humanizer.CasingExtensions);
		}
	}
}
