﻿using System;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	public class MarkerExpression : Expression
	{
		public MarkerType MarkerType   { get; }
		public Expression InnerExpression { get; }

		public MarkerExpression(Expression innerExpression, MarkerType markerType)
		{
			MarkerType = markerType;
			InnerExpression = innerExpression;
		}

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => InnerExpression.Type;
		public override bool           CanReduce => true;

		public override Expression     Reduce() => InnerExpression;

		public static Expression PreferClientSide(Expression innerExpression)
		{
			return innerExpression switch
			{
				SqlPlaceholderExpression => innerExpression,
				_ => new MarkerExpression(innerExpression, MarkerType.PreferClientSide),
			};
		}

		public MarkerExpression Update(Expression closureExpression)
		{
			if (ReferenceEquals(InnerExpression, closureExpression))
				return this;
			return new MarkerExpression(closureExpression, MarkerType);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			return visitor switch
			{
				ExpressionVisitorBase baseVisitor => baseVisitor.VisitMarkerExpression(this),
				_ => base.Accept(visitor),
			};
		}

		public override string ToString()
		{
			return $"$({InnerExpression})";
		}
	}
}
