using System;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB
{
	partial class Sql
	{
		[Extension("", "", PreferServerSide = true, IsPredicate = true, BuilderType = typeof(BetweenBuilder))]
		public static bool Between<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && value.CompareTo(low) >= 0 && value.CompareTo(high) <= 0;
		}

		[Extension("", "", PreferServerSide = true, IsPredicate = true, BuilderType = typeof(BetweenBuilder))]
		public static bool Between<T>(this T? value, T? low, T? high)
			where T : struct, IComparable
		{
			return value != null && value.Value.CompareTo(low) >= 0 && value.Value.CompareTo(high) <= 0;
		}

		[Extension("", "", PreferServerSide = true, IsPredicate = true, BuilderType = typeof(NotBetweenBuilder))]
		public static bool NotBetween<T>(this T value, T low, T high)
			where T : IComparable
		{
			return value != null && (value.CompareTo(low) < 0 || value.CompareTo(high) > 0);
		}

		[Extension("", "", PreferServerSide = true, IsPredicate = true, BuilderType = typeof(NotBetweenBuilder))]
		public static bool NotBetween<T>(this T? value, T? low, T? high)
			where T : struct, IComparable
		{
			return value != null && (value.Value.CompareTo(low) < 0 || value.Value.CompareTo(high) > 0);
		}

		private sealed class BetweenBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var args = Array.ConvertAll(builder.Arguments, x => builder.ConvertExpressionToSql(x)!);
				builder.ResultExpression = new SqlSearchCondition(false, canBeUnknown: null, new SqlPredicate.Between(args[0], false, args[1], args[2]));
			}
		}

		private sealed class NotBetweenBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var args = Array.ConvertAll(builder.Arguments, x => builder.ConvertExpressionToSql(x)!);
				builder.ResultExpression = new SqlSearchCondition(false, canBeUnknown: null, new SqlPredicate.Between(args[0], true, args[1], args[2]));
			}
		}
	}
}
