using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlExpression : ISqlExpression
	{
		public SqlExpression(Type? systemType, string expr, int precedence, SqlFlags flags, params ISqlExpression[] parameters)
		{
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			foreach (var value in parameters)
				if (value == null) throw new ArgumentNullException(nameof(parameters));

			SystemType  = systemType;
			Expr        = expr;
			Precedence  = precedence;
			Parameters  = parameters;
			Flags       = flags;
		}

		public SqlExpression(Type? systemType, string expr, int precedence, params ISqlExpression[] parameters)
			: this(systemType, expr, precedence, SqlFlags.IsPure, parameters)
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

		public Type?            SystemType  { get; }
		public string           Expr        { get; }
		public int              Precedence  { get; }
		public ISqlExpression[] Parameters  { get; }
		public SqlFlags         Flags       { get; }

		public bool             IsAggregate      => (Flags & SqlFlags.IsAggregate)      != 0;
		public bool             IsPure           => (Flags & SqlFlags.IsPure)           != 0;
		public bool             IsPredicate      => (Flags & SqlFlags.IsPredicate)      != 0;
		public bool             IsWindowFunction => (Flags & SqlFlags.IsWindowFunction) != 0;

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			for (var i = 0; i < Parameters.Length; i++)
				Parameters[i] = Parameters[i].Walk(options, func)!;

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
		public  bool   CanBeNull
		{
			get
			{
				if (_canBeNull.HasValue)
					return _canBeNull.Value;

				foreach (var value in Parameters)
					if (value.CanBeNull)
						return true;

				return false;
			}

			set => _canBeNull = value;
		}

		internal static Func<ISqlExpression,ISqlExpression,bool> DefaultComparer = (x, y) => true;

		int? _hashCode;

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			if (_hashCode != null)
				return _hashCode.Value;

			var hashCode = Expr.GetHashCode();

			if (SystemType != null)
				hashCode = unchecked(hashCode + (hashCode * 397) ^ SystemType.GetHashCode());

			for (var i = 0; i < Parameters.Length; i++)
				hashCode = unchecked(hashCode + (hashCode * 397) ^ Parameters[i].GetHashCode());

			_hashCode = hashCode;

			return hashCode;
		}

		public bool Equals(ISqlExpression? other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (this == other)
				return true;

			if (!(other is SqlExpression expr) || SystemType != expr.SystemType || Expr != expr.Expr || Parameters.Length != expr.Parameters.Length)
				return false;

			for (var i = 0; i < Parameters.Length; i++)
				if (!Parameters[i].Equals(expr.Parameters[i], comparer))
					return false;

			return comparer(this, expr);
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			if (!objectTree.TryGetValue(this, out var clone))
			{
				objectTree.Add(this, clone = new SqlExpression(
					SystemType,
					Expr,
					Precedence,
					Parameters.Select(e => (ISqlExpression)e.Clone(objectTree, doClone)).ToArray()));
			}

			return clone;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SqlExpression;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			var len = sb.Length;
			var ss  = Parameters.Select(p =>
			{
				p.ToString(sb, dic);
				var s = sb.ToString(len, sb.Length - len);
				sb.Length = len;
				return (object)s;
			});

			if (Parameters.Length == 0)
				return sb.Append(Expr);

			return sb.AppendFormat(Expr, ss.ToArray());
		}

		#endregion

		#region Public Static Members

		public static bool NeedsEqual(IQueryElement ex)
		{
			switch (ex.ElementType)
			{
				case QueryElementType.SqlParameter:
				case QueryElementType.SqlField    :
				case QueryElementType.Column      : return true;
				case QueryElementType.SqlExpression:
				{
					var expr = (SqlExpression)ex;
					if (expr.IsPredicate)
						return false;
					if (QueryHelper.IsTransitiveExpression(expr))
						return NeedsEqual(expr.Parameters[0]);
					return true;
				}
				case QueryElementType.SqlFunction :

					var f = (SqlFunction)ex;

					return f.Name switch
					{
						"EXISTS" => false,
						_        => true,
					};
			}

			return false;
		}

		#endregion
	}
}
