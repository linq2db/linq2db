﻿using System;

namespace LinqToDB.SqlQuery
{
	public interface ISqlExpression : IQueryElement, IEquatable<ISqlExpression>, ISqlExpressionWalkable, ICloneableElement
	{
		bool CanBeNull();
		bool Equals   (ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer);

		int  Precedence { get; }
		Type SystemType { get; }
	}
}
