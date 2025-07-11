using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlCaseExpression : SqlExpressionBase
	{
		public class CaseItem
		{
			public CaseItem(ISqlPredicate condition, ISqlExpression resultExpression)
			{
				Condition        = condition;
				ResultExpression = resultExpression;
			}

			public void Modify(ISqlPredicate condition, ISqlExpression resultExpression)
			{
				Condition        = condition;
				ResultExpression = resultExpression;
			}

			public CaseItem Update(ISqlPredicate condition, ISqlExpression resultExpression)
			{
				if (ReferenceEquals(Condition, condition) && ReferenceEquals(ResultExpression, resultExpression))
					return this;

				return new CaseItem(condition, resultExpression);
			}

			public ISqlPredicate  Condition        { get; set; }
			public ISqlExpression ResultExpression { get; set; }
		}

		public SqlCaseExpression(DbDataType dataType, IReadOnlyCollection<CaseItem> cases, ISqlExpression? elseExpression)
		{
			_dataType      = dataType;
			_cases         = cases.ToList();
			ElseExpression = elseExpression;
		}

		internal List<CaseItem> _cases;
		readonly DbDataType     _dataType;

		public ISqlExpression?         ElseExpression { get; private set; }
		public IReadOnlyList<CaseItem> Cases          => _cases;
		public DbDataType              Type           => _dataType;

		public override int              Precedence  => SqlQuery.Precedence.Primary;
		public override Type?            SystemType  => _dataType.SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlCase;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("$CASE$")
				.AppendLine();

			using (writer.IndentScope())
			{
				foreach (var c in Cases)
				{
					writer
						.Append("WHEN ")
						.AppendElement(c.Condition)
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
			hash.Add(ElementType);
			hash.Add(_dataType);
			foreach (var c in Cases)
			{
				hash.Add(c.Condition.GetElementHashCode());
				hash.Add(c.ResultExpression.GetElementHashCode());
			}

			if (ElseExpression != null)
				hash.Add(ElseExpression.GetElementHashCode());
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlCaseExpression caseOther)
				return false;

			if (ElseExpression != null && caseOther.ElseExpression == null)
				return false;

			if (ElseExpression == null && caseOther.ElseExpression != null)
				return false;

			if (ElseExpression != null && caseOther.ElseExpression != null && !comparer(ElseExpression, caseOther.ElseExpression))
				return false;

			if (Cases.Count != caseOther.Cases.Count) 
				return false;

			for (var index = 0; index < Cases.Count; index++)
			{
				var c = Cases[index];
				var o = caseOther.Cases[index];
				if (!c.Condition.Equals(o.Condition))
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

		public void Modify(List<CaseItem> cases, ISqlExpression? resultExpression)
		{
			_cases         = cases;
			ElseExpression = resultExpression;
		}

		public void Modify(ISqlExpression? resultExpression)
		{
			ElseExpression = resultExpression;
		}
	}
}
