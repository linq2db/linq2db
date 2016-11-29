using System;

namespace LinqToDB.Common
{
	public static class Configuration
	{
		public static bool IsStructIsScalarType = true;
		public static bool AvoidSpecificDataProviderAPI;

		public static class Linq
		{
			public static bool PreloadGroups;
			public static bool IgnoreEmptyUpdate;
			public static bool AllowMultipleQuery;
			public static bool GenerateExpressionTest;
			public static bool DoNotClearOrderBys;
		}

		public static class LinqService
		{
			public static bool SerializeAssemblyQualifiedName;
			public static bool ThrowUnresolvedTypeException;
		}
	}
}
