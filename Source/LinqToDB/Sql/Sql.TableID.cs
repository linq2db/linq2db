using System;

using LinqToDB.Linq.Builder;
using LinqToDB.SqlQuery;

namespace LinqToDB
{
	public partial class Sql
	{
		public readonly struct SqlID : IToSqlConverter, IEquatable<SqlID>
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
				return obj is SqlID other && Equals(other);
			}

			public bool Equals(SqlID other)
			{
				return Type == other.Type && ID == other.ID;
			}

			public override int GetHashCode()
			{
				return (int)Type | (ID.GetHashCode() >> 3);
			}

			public ISqlExpression ToSql(object value)
			{
				return new SqlValue(typeof(SqlID), value);
			}

			public static SqlID Parse(string value)
			{
				var idx = value.IndexOf(':');

				if (idx == -1)
					throw new InvalidOperationException($"Cannot parse '{value}' to SqlID.");

				var type = value.Substring(0, idx);
				var id   = value.Substring(idx + 1);

#pragma warning disable CA2263 // Prefer generic overload when type is known
				return new ((SqlIDType)Enum.Parse(typeof(SqlIDType), type), id);
#pragma warning restore CA2263 // Prefer generic overload when type is known
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
