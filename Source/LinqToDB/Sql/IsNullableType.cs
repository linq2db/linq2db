using System;

namespace LinqToDB
{
	public static partial class Sql
	{
		public enum IsNullableType
		{
			Undefined = 0,
			Nullable,
			NotNullable,
			IfAnyParameterNullable,
			SameAsFirstParameter,
			SameAsSecondParameter,
			SameAsThirdParameter,
			SameAsLastParameter,
		}
	}
}
