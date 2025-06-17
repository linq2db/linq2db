﻿using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public sealed class ISqlExpressionEqualityComparer : IEqualityComparer<ISqlExpression>
	{
		public static readonly IEqualityComparer<ISqlExpression> Instance = new ISqlExpressionEqualityComparer();

		private ISqlExpressionEqualityComparer()
		{
		}

		bool IEqualityComparer<ISqlExpression>.Equals(ISqlExpression? x, ISqlExpression? y)
		{
			if (ReferenceEquals(x, y))
				return true;

			if (x is not null && y is not null)
				return x.Equals(y, SqlExpression.DefaultComparer);

			return false;
		}

		int IEqualityComparer<ISqlExpression>.GetHashCode(ISqlExpression obj)
		{
			// TODO: we don't have GetHashCode for ISqlExpression currently
			return obj.GetType().GetHashCode();
		}
	}
}
