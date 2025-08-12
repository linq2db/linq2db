using System;
using System.Runtime.CompilerServices;

using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlObjectExpression : SqlExpressionBase
	{
		readonly SqlGetValue[] _infoParameters;

		public SqlObjectExpression(MappingSchema mappingSchema, SqlGetValue[] infoParameters)
		{
			MappingSchema   = mappingSchema;
			_infoParameters = infoParameters;
		}

		public SqlValue GetSqlValue(object obj, int index)
		{
			var p  = _infoParameters[index];

			object? value;

			if (p.ColumnDescriptor != null)
			{
				return p.ColumnDescriptor.GetSqlValueFromObject(obj);
			}

			if (p.GetValueFunc != null)
			{
				value = p.GetValueFunc(obj);
			}
			else
				throw new InvalidOperationException();

			return MappingSchema.GetSqlValue(p.ValueType, value, value == null ? DbDataType.Undefined : MappingSchema.GetDbDataType(value.GetType()));
		}

		public override Type? SystemType => null;
		public override int Precedence => LinqToDB.SqlQuery.Precedence.Unknown;

		#region IEquatable<ISqlExpression> Members

		public override bool Equals(ISqlExpression? other)
		{
			return Equals(other, SqlExtensions.DefaultComparer);
		}

		#endregion

		#region ISqlExpression Members

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			foreach (var parameter in _infoParameters)
				if (parameter.Sql.CanBeNullable(nullability))
					return true;

			return false;
		}

		public override bool Equals(ISqlExpression? other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return ReferenceEquals(this, other);
		}

		#endregion

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.SqlObjectExpression;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.Append('(');

			for (var index = 0; index < _infoParameters.Length; index++)
			{
				var parameter = _infoParameters[index];
				writer.AppendElement(parameter.Sql);
				if (index < _infoParameters.Length - 1)
					writer.Append(", ");
			}

			writer.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);

			foreach (var parameter in _infoParameters)
				hash.Add(parameter.Sql.GetElementHashCode());

			return hash.ToHashCode();
		}

		#endregion

		public MappingSchema MappingSchema { get; }

		internal SqlGetValue[] InfoParameters => _infoParameters;
	}
}
