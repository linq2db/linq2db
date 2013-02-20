using System;

namespace LinqToDB.Common
{
	public static class Configuration
	{
		public static bool IsStructIsScalarType = true;

		public static class Linq
		{
			public static bool PreloadGroups          { get; set; }
			public static bool IgnoreEmptyUpdate      { get; set; }
			public static bool AllowMultipleQuery     { get; set; }
			public static bool GenerateExpressionTest { get; set; }
		}
	}
}
