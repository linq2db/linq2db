using System;

namespace NuGet
{
	class Packages
	{
		static void References()
		{
			_ = typeof(IBM.Data.DB2.DB2Connection);
			_ = typeof(IBM.Data.Informix.IfxConnection);
			_ = typeof(AdoNetCore.AseClient.AseConnection);
		}
	}
}
