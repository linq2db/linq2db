using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public class SqlExpression : SqlExpressionBase
	{
		public SqlExpression(Type? systemType, string expr, int precedence, SqlFlags flags, ParametersNullabilityType nullabilityType, bool? canBeNull, params ISqlExpression[] parameters)
		{
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			foreach (var value in parameters)
				if (value == null) throw new ArgumentNullException(nameof(parameters));

			SystemType      = systemType;
			Expr            = expr;
			Precedence      = precedence;
			Parameters      = parameters;
			Flags           = flags;
			NullabilityType = nullabilityType;
			_canBeNull      = canBeNull;
		}

		public SqlExpression(Type? systemType, string expr, int precedence, params ISqlExpression[] parameters)
			: this(systemType, expr, precedence, SqlFlags.IsPure, ParametersNullabilityType.Undefined, null, parameters)
		{
		}

		public SqlExpression(string expr, int precedence, params ISqlExpression[] parameters)
			: this(null, expr, precedence, parameters)
		{
		}

		public SqlExpression(Type? systemType, string expr, params ISqlExpression[] parameters)
			: this(systemType, expr, SqlQuery.Precedence.Unknown, parameters)
		{
		}

		public SqlExpression(string expr, params ISqlExpression[] parameters)
			: this(null, expr, SqlQuery.Precedence.Unknown, parameters)
		{
		}

		public override Type?                     SystemType        { get; }
		public override int                       Precedence        { get; }

		public          string                    Expr              { get; }
		public          ISqlExpression[]          Parameters        { get; }
		public          SqlFlags                  Flags             { get; }
		public          bool?                     CanBeNullNullable => _canBeNull;
		public          ParametersNullabilityType NullabilityType   { get; }

		public bool             IsAggregate      => (Flags & SqlFlags.IsAggregate)      != 0;
		public bool             IsPure           => (Flags & SqlFlags.IsPure)           != 0;
		public bool             IsPredicate      => (Flags & SqlFlags.IsPredicate)      != 0;
		public bool             IsWindowFunction => (Flags & SqlFlags.IsWindowFunction) != 0;

		#region ISqlExpression Members

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return QueryHelper.CalcCanBeNull(SystemType, _canBeNull, NullabilityType,
				       Parameters.Select(p => p.CanBeNullable(nullability)));
		}

		bool? _canBeNull;
		public  bool   CanBeNull
		{
			get => _canBeNull ?? true;
			set => _canBeNull = value;
		}

		internal static Func<ISqlExpression,ISqlExpression,bool> DefaultComparer = (x, y) => true;

		int? _hashCode;

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			if (_hashCode != null)
				return _hashCode.Value;

			var hashCode = new HashCode();
			hashCode.Add(Expr);
			hashCode.Add(SystemType);

			foreach (var p in Parameters)
				hashCode.Add(p);

			return _hashCode ??= hashCode.ToHashCode();
		}

		public override bool Equals(ISqlExpression? other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (!(other is SqlExpression expr) || SystemType != expr.SystemType || Expr != expr.Expr || Parameters.Length != expr.Parameters.Length)
				return false;

			for (var i = 0; i < Parameters.Length; i++)
				if (!Parameters[i].Equals(expr.Parameters[i], comparer))
					return false;

			return comparer(this, expr);
		}

		#endregion

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.SqlExpression;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.DebugAppendUniqueId(this);

			var len = writer.Length;
			var arguments  = Parameters.Select(p =>
			{
				p.ToString(writer);
				var s = writer.ToString(len, writer.Length - len);
				writer.Length = len;
				return (object)s;
			});

			if (Parameters.Length == 0)
				return writer.Append(Expr);

			if (Expr.Contains("{"))
				writer.AppendFormat(Expr, arguments.ToArray());
			else
				writer.Append(Expr)
					.Append('{')
					.Append(string.Join(", ", arguments.Select(s => string.Format(CultureInfo.InvariantCulture, "{0}", s))))
					.Append('}');

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Expr);
			hash.Add(SystemType);
			hash.Add(Precedence);
			hash.Add(Flags);
			hash.Add(NullabilityType);
			foreach (var parameter in Parameters)
				hash.Add(parameter.GetElementHashCode());
			return hash.ToHashCode();
		}

		#endregion

		public override bool Equals(object? obj)
		{
			return Equals(obj, DefaultComparer);
		}

		#region Public Static Members

		public static bool NeedsEqual(IQueryElement ex)
		{
			switch (ex.ElementType)
			{
				case QueryElementType.SqlParameter:
				case QueryElementType.SqlField    :
				case QueryElementType.SqlQuery    :
				case QueryElementType.Column      : return true;
				case QueryElementType.SqlExpression:
				{
					var expr = (SqlExpression)ex;
					if (expr.IsPredicate)
						return false;
					if (QueryHelper.IsTransitivePredicate(expr))
						return false;
					return true;
				}
				case QueryElementType.SearchCondition :
					return false;
				case QueryElementType.SqlFunction :
					return true;
			}

			return false;
		}

		#endregion
	}
}
