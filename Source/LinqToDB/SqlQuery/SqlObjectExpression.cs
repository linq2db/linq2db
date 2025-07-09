using System;
using System.Runtime.CompilerServices;

using LinqToDB.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.SqlQuery
{
	public class SqlObjectExpression : ISqlExpression
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
				return MappingSchema.GetSqlValueFromObject(p.ColumnDescriptor, obj);
			}

			if (p.GetValueFunc != null)
			{
				value = p.GetValueFunc(obj);
			}
			else
				throw new InvalidOperationException();

			return MappingSchema.GetSqlValue(p.ValueType, value, null);
		}

		public Type? SystemType => null;
		public int Precedence => SqlQuery.Precedence.Unknown;

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return Equals(other, DefaultComparer);
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNullable(NullabilityContext nullability)
		{
			if (_canBeNull.HasValue)
				return _canBeNull.Value;

			foreach (var parameter in _infoParameters)
				if (parameter.Sql.CanBeNullable(nullability))
					return true;

			return false;
		}

		private bool? _canBeNull;

		public bool CanBeNull
		{
			get
			{
				if (_canBeNull.HasValue)
					return _canBeNull.Value;

				return false;
			}

			set => _canBeNull = value;
		}

		internal static Func<ISqlExpression,ISqlExpression,bool> DefaultComparer = (x, y) => true;

		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}

		public bool Equals(ISqlExpression? other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return ReferenceEquals(this, other);
		}

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public QueryElementType ElementType => QueryElementType.SqlObjectExpression;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
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

		public int GetElementHashCode()
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
