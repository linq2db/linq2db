using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlRow : ISqlExpression
	{
		public SqlRow(ISqlExpression[] values)
		{
			Values = values;
			// SqlRow doesn't exactly have its own type and nullability, being a collection of values.
			// But it can be null in the sense that `(1, 2) IS NULL` can be true (when all values are null).
			CanBeNull = values.All(x => x.CanBeNull);
		}

		public ISqlExpression[] Values { get; }
		
		public bool CanBeNull { get; }

		public int Precedence => SqlQuery.Precedence.Primary;

		public Type? SystemType => null;

		public QueryElementType ElementType => QueryElementType.SqlRow;

		public bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			=> other is SqlRow row && Values.Zip(row.Values, comparer).All(x => x);

		public bool Equals([AllowNull] ISqlExpression other)
			=> other is SqlRow row && Values.SequenceEqual(row.Values);

#if OVERRIDETOSTRING
		public override string ToString()
		{
			var sb  = new StringBuilder();
			var dic = new Dictionary<IQueryElement, IQueryElement>();
			return ToString(sb, dic).ToString();
		}
#endif

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			sb.Append('(');
			foreach (var value in Values)
			{
				value.ToString(sb, dic);
				sb.Append(", ");
			}
			sb.Length -= 2;	// Note that Values is guaranteed not to be empty, there's no API to build a 0 element row.
			return sb.Append(')');
		}

		public ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			for (int i = 0; i < Values.Length; ++i)
				Values[i] = Values[i].Walk(options, context, func)!;
			return func(context, this);
		}
	}
}
