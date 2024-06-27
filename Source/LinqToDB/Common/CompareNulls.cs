namespace LinqToDB.Common
{
	/// <summary>Defines how null values are compared in compiled queries.</summary>
	public enum CompareNulls
	{
		/// <summary>C# semantics: nulls values are equal.</summary>
		LikeCSharp,
		/// <summary>SQL semantics: nulls values are unknown.</summary>
		LikeSql,
		/// <summary>SQL semantics, except null parameters are treated like null constants (they compile to "IS NULL").</summary>
		LikeSqlExceptParameters,
	}
}
