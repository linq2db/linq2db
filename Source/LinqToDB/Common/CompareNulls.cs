namespace LinqToDB.Common
{
	/// <summary>Defines how null values are compared in compiled queries.</summary>
	public enum CompareNulls
	{
		/// <summary>CLR semantics: nulls values are equal.</summary>
		/// <note>This option may add significant complexity in generated SQL, possibly preventing index usage.</note>
		LikeClr,
		/// <summary>SQL semantics: nulls values are unknown.</summary>
		LikeSql,
		/// <summary>SQL semantics, except null parameters are treated like null constants (they compile to "IS NULL").</summary>
		LikeSqlExceptParameters,
	}
}
