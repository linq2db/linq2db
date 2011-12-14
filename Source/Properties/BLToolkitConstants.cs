using System;

namespace LinqToDB
{
	/// <summary>
	/// Global library constants.
	/// </summary>
	public static partial class LinqToDBConstants
	{
		/// <summary>
		/// Major component of version.
		/// </summary>
		public const string MajorVersion = "1";

		/// <summary>
		/// Minor component of version.
		/// </summary>
		public const string MinorVersion = "0";

		/// <summary>
		/// Build component of version.
		/// </summary>
		public const string Build = "0";

		/// <summary>
		/// Full version string.
		/// </summary>
		public const string FullVersionString = MajorVersion + "." + MinorVersion + "." + Build + "." + Revision;

		/// <summary>
		/// Full BLT version.
		/// </summary>
		public static readonly Version FullVersion = new Version(FullVersionString);

		public const string ProductName        = "Linq to DB";
		public const string ProductDescription = "Linq to DB";
		public const string Copyright          = "\xA9 2011-2012 www.linq2db.net";
	}
}
