using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using Common;
	using Expressions;
	using LinqToDB.Extensions;
	using Linq.Builder;
	using Mapping;
	using Reflection;

	public class SqlObjectExpression : ISqlExpression
	{
		readonly Dictionary<int, Func<object, object>> _getters = new ();
		readonly SqlInfo[]                             _infoParameters;

		public SqlObjectExpression(MappingSchema mappingSchema, SqlInfo[] infoParameters)
		{
			MappingSchema   = mappingSchema;
			_infoParameters = infoParameters;
		}

		public object? GetValue(object obj, int index)
		{
			var p  = _infoParameters[index];
			var mi = p.MemberChain[p.MemberChain.Length - 1];

			if (!_getters.TryGetValue(index, out var getter))
			{
				var ta        = TypeAccessor.GetAccessor(mi.DeclaringType!);
				var valueType = mi.GetMemberType();
				getter        = ta[mi.Name].Getter!;

				if (valueType.ToNullableUnderlying().IsEnum)
				{
					var toType           = Converter.GetDefaultMappingFromEnumType(MappingSchema, valueType)!;
					var convExpr         = MappingSchema.GetConvertExpression(valueType, toType)!;
					var convParam        = Expression.Parameter(typeof(object));
					var getterExpression = Expression.Constant(getter);
					var callGetter       = Expression.Invoke(getterExpression, convParam);


					var lex = Expression.Lambda<Func<object, object>>(
						Expression.Convert(convExpr.GetBody(Expression.Convert(callGetter, valueType)), typeof(object)),
						convParam);

					getter = lex.CompileExpression();
				}

				_getters.Add(index, getter);
			}

			return getter(obj);
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

		ISqlExpression ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			for (var i = 0; i < _infoParameters.Length; i++)
			{
				var parameter = _infoParameters[i];
				_infoParameters[i] = parameter.WithSql(parameter.Sql.Walk(options, func)!);
			}
			return func(this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return Equals(other, DefaultComparer);
		}

		#endregion

		#region ISqlExpression Members

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
		internal SqlInfo[] InfoParameters => _infoParameters;

	}
}
