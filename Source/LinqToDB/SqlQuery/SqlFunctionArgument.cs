using System;
using System.Globalization;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.SqlQuery
{
	public class SqlFunctionArgument : QueryElement
	{
		public SqlFunctionArgument(ISqlExpression expression, Sql.AggregateModifier modifier = Sql.AggregateModifier.None, ISqlExpression? suffix = default)
		{
			Expression = expression;
			Modifier   = modifier;
			Suffix     = suffix;
		}

		public ISqlExpression        Expression { get; private set; }
		public Sql.AggregateModifier Modifier   { get; }
		public ISqlExpression?       Suffix     { get; private set; }

		public override QueryElementType ElementType => QueryElementType.SqlFunctionArgument;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (Modifier != Sql.AggregateModifier.None)
			{
				writer
					.Append(Modifier.ToString().ToUpper(CultureInfo.InvariantCulture))
					.Append(' ');

			}

			writer.AppendElement(Expression);
			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(Expression.GetElementHashCode());
			hash.Add(Modifier);
			hash.Add(Suffix?.GetElementHashCode());

			return hash.ToHashCode();
		}

		public void Modify(ISqlExpression sqlExpression, ISqlExpression? suffix)
		{
			Expression = sqlExpression;
			Suffix     = suffix;
		}

		public SqlFunctionArgument WithExpression(SqlConditionExpression sqlExpression)
		{
			if (ReferenceEquals(Expression, sqlExpression))
				return this;

			return new SqlFunctionArgument(sqlExpression, Modifier, Suffix);
		}
	}
}
