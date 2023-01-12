using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using LinqToDB.Extensions;
	using Mapping;

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

			return MappingSchema.GetSqlValue(p.ValueType, value);
		}

		public Type? SystemType => null;
		public int Precedence => SqlQuery.Precedence.Unknown;

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			for (var i = 0; i < _infoParameters.Length; i++)
			{
				var parameter = _infoParameters[i];
				_infoParameters[i] = parameter.WithSql(parameter.Sql.Walk(options, context, func)!);
			}
			return func(context, this);
		}

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

				foreach (var parameter in _infoParameters)
					if (parameter.Sql.CanBeNull)
						return true;

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

		public QueryElementType ElementType => QueryElementType.SqlObjectExpression;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			sb.Append('(');
			foreach (var parameter in _infoParameters)
			{
				parameter.Sql.ToString(sb, dic)
					.Append(", ");
			}

			if (_infoParameters.Length > 0)
				sb.Length -= 2;

			sb.Append(')');

			return sb;
		}

		#endregion


		public MappingSchema MappingSchema { get; }

		internal SqlGetValue[] InfoParameters => _infoParameters;

	}
}
