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
			public static bool OptimizeJoins = true;

			/// <summary>
			/// If set to true unllable fields would be checked for IS NULL when comparasion type is NotEqual 
			/// <example>
			/// public class MyEntity
			/// {
			///     public int? Value;
			/// }
			/// 
			/// db.MyEntity.Where(e => e.Value != 10)
			/// 
			/// Would be converted to
			/// 
			/// SELECT Value FROM MyEntity WHERE Value IS NULL OR Value != 10
			/// </example>
			/// </summary>
			public static bool CheckNullForNotEquals = true;
		}

		public static class LinqService
		{
			public static bool SerializeAssemblyQualifiedName;
			public static bool ThrowUnresolvedTypeException;
		}
	}
}
