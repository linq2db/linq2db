using System;

namespace LinqToDB.DataProvider.Informix
{
	public static class InformixConfiguration
	{
		/// <summary>
		/// Enables use of explicit fractional seconds separator in datetime values. Must be enabled for Informix starting from v11.70.xC8 and v12.10.xC2.
		/// More details at: https://www.ibm.com/support/knowledgecenter/SSGU8G_12.1.0/com.ibm.po.doc/new_features_ce.htm#newxc2__xc2_datetime
		/// </summary>
		public static bool ExplicitFractionalSecondsSeparator = false;
	}
}
