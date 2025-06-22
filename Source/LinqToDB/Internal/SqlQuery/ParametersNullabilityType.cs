namespace LinqToDB.Internal.SqlQuery
{
	// !!! must be in sync by value with Sql.IsNullableType

	/// <summary>
	/// Provides information when function or expression could return null.
	/// </summary>
	public enum ParametersNullabilityType
	{
		/// <summary>
		/// Nullability not specified, and other sources (like <see cref="Sql.ExpressionAttribute.CanBeNull"/> or return type)
		/// will be used to identify nullability.
		/// </summary>
		Undefined = 0,
		/// <summary>
		/// Expression could always return NULL.
		/// </summary>
		Nullable,
		/// <summary>
		/// Expression never returns NULL.
		/// </summary>
		NotNullable,
		/// <summary>
		/// Expression could return NULL if at least one parameter of expression could contain NULL.
		/// </summary>
		IfAnyParameterNullable,
		/// <summary>
		/// Expression could return NULL if first parameter of expression could contain NULL.
		/// </summary>
		SameAsFirstParameter,
		/// <summary>
		/// Expression could return NULL if second parameter of expression could contain NULL.
		/// </summary>
		SameAsSecondParameter,
		/// <summary>
		/// Expression could return NULL if third parameter of expression could contain NULL.
		/// </summary>
		SameAsThirdParameter,
		/// <summary>
		/// Expression could return NULL if last parameter of expression could contain NULL.
		/// </summary>
		SameAsLastParameter,
		/// <summary>
		/// Expression could return NULL if all parameters of expression could contain NULL.
		/// </summary>
		IfAllParametersNullable,
	}

}
