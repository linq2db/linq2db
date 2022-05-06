using System;
using System.Linq.Expressions;

namespace LinqToDB
{
	using Expressions;
	using Linq.Builder;
	using SqlQuery;

	public partial class Sql
	{
		public struct SqlID : IToSqlConverter
		{
			public SqlIDType Type { get; }
			public string    ID   { get; }

			public SqlID(SqlIDType type, string id)
			{
				ID   = id;
				Type = type;
			}

			public override string ToString()
			{
				return $"{Type}:{ID}";
			}

			public override bool Equals(object? obj)
			{
				return obj is SqlID id && Type == id.Type && ID == id.ID;
			}

			public override int GetHashCode()
			{
				return (int)Type | (ID.GetHashCode() >> 3);
			}

			public ISqlExpression ToSql(Expression expression)
			{
				var value = expression.EvaluateExpression();
				return new SqlValue(typeof(SqlID), value, value);
			}

			public static SqlID Parse(string value)
			{
				var idx = value.IndexOf(':');

				if (idx == -1)
					throw new InvalidOperationException($"Cannot parse '{value}' to SqlID.");

				var type = value.Substring(0, idx);
				var id   = value.Substring(idx + 1);

				return new ((SqlIDType)Enum.Parse(typeof(SqlIDType), type), id);
			}
		}

		public static SqlID TableAlias(string id)
		{
			return new(SqlIDType.TableAlias, id);
		}

		public static SqlID TableName(string id)
		{
			return new(SqlIDType.TableName, id);
		}

		public static SqlID TableSpec(string id)
		{
			return new(SqlIDType.TableSpec, id);
		}
	}
}
