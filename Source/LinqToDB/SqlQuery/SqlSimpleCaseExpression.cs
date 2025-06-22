using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public class SqlSimpleCaseExpression : SqlExpressionBase
	{
		public class CaseExpression
		{
			public CaseExpression(ISqlExpression matchValue, ISqlExpression resultExpression)
			{
				MatchValue       = matchValue;
				ResultExpression = resultExpression;
			}

			public void Modify(ISqlExpression matchValue, ISqlExpression resultExpression)
			{
				MatchValue       = matchValue;
				ResultExpression = resultExpression;
			}

			public CaseExpression Update(ISqlExpression matchValue, ISqlExpression resultExpression)
			{
				if (ReferenceEquals(MatchValue, matchValue) && ReferenceEquals(ResultExpression, resultExpression))
					return this;

				return new CaseExpression(matchValue, resultExpression);
			}

			public ISqlExpression MatchValue       { get; set; }
			public ISqlExpression ResultExpression { get; set; }
		}

		public SqlSimpleCaseExpression(DbDataType dataType, ISqlExpression primaryExpression, IReadOnlyCollection<CaseExpression> cases, ISqlExpression? elseExpression)
		{
			_dataType         = dataType;
			PrimaryExpression = primaryExpression;
			_cases            = cases.ToList();
			ElseExpression    = elseExpression;
		}

		internal List<CaseExpression> _cases;
		readonly DbDataType           _dataType;

		public ISqlExpression                PrimaryExpression { get; private set; }
		public ISqlExpression?               ElseExpression    { get; private set; }
		public IReadOnlyList<CaseExpression> Cases             => _cases;

		public override int              Precedence  => SqlQuery.Precedence.Primary;
		public override Type?            SystemType  => _dataType.SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlSimpleCase;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("$CASE$ ")
				.AppendElement(PrimaryExpression)
				.AppendLine();

			using (writer.IndentScope())
			{
				foreach (var c in Cases)
				{
					writer
						.Append("WHEN ")
						.AppendElement(c.MatchValue)
						.Append(" THEN ")
						.AppendElement(c.ResultExpression)
						.AppendLine();
				}

				if (ElseExpression != null)
				{
					writer
						.Append("ELSE ")
						.AppendElement(ElseExpression)
						.AppendLine();
				}
			}

			writer.AppendLine("END");

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(PrimaryExpression.GetElementHashCode());
			hash.Add(ElseExpression?.GetElementHashCode());
			foreach (var c in Cases)
			{
				hash.Add(c.MatchValue.GetElementHashCode());
				hash.Add(c.ResultExpression.GetElementHashCode());
			}
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlSimpleCaseExpression caseOther)
				return false;

			if (!comparer(PrimaryExpression, caseOther.PrimaryExpression))
				return false;

			if (ElseExpression != null && caseOther.ElseExpression == null)
				return false;

			if (ElseExpression == null && caseOther.ElseExpression != null)
				return false;

			if (ElseExpression != null && caseOther.ElseExpression != null && !ElseExpression.Equals(caseOther.ElseExpression, comparer))
				return false;

			if (Cases.Count != caseOther.Cases.Count) 
				return false;

			for (var index = 0; index < Cases.Count; index++)
			{
				var c = Cases[index];
				var o = caseOther.Cases[index];

				if (!c.MatchValue.Equals(o.MatchValue))
					return false;

				if (!c.ResultExpression.Equals(o.ResultExpression, comparer)) 
					return false;
			}

			return true;
		}

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			foreach (var c in Cases)
			{
				if (c.ResultExpression.CanBeNullable(nullability))
					return true;
			}

			if (ElseExpression != null)
			{
				if (!ElseExpression.CanBeNullable(nullability)) 
					return false;
			}

			return true;
		}

		public void Modify(ISqlExpression primaryExpression, List<CaseExpression> cases, ISqlExpression? resultExpression)
		{
			PrimaryExpression = primaryExpression;
			_cases            = cases;
			ElseExpression    = resultExpression;
		}
	}
}
