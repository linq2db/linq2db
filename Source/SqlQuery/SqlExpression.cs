using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlExpression : ISqlExpression
	{
		public SqlExpression(Type systemType, string expr, int precedence, params ISqlExpression[] parameters)
		{
			if (parameters == null) throw new ArgumentNullException("parameters");

			foreach (var value in parameters)
				if (value == null) throw new ArgumentNullException("parameters");

			SystemType = systemType;
			Expr       = expr;
			Precedence = precedence;
			Parameters = parameters;
		}

		public SqlExpression(string expr, int precedence, params ISqlExpression[] parameters)
			: this(null, expr, precedence, parameters)
		{
		}

		public SqlExpression(Type systemType, string expr, params ISqlExpression[] parameters)
			: this(systemType, expr, PrecedenceLevel.Unknown, parameters)
		{
		}

		public SqlExpression(string expr, params ISqlExpression[] parameters)
			: this(null, expr, PrecedenceLevel.Unknown, parameters)
		{
		}

		public Type             SystemType { get; private set; }
		public string           Expr       { get; private set; }
		public int              Precedence { get; private set; }
		public ISqlExpression[] Parameters { get; private set; }

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			for (var i = 0; i < Parameters.Length; i++)
				Parameters[i] = Parameters[i].Walk(skipColumns, func);

			return func(this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
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

			set { _canBeNull = value; }
		}

		internal static Func<ISqlExpression,ISqlExpression,bool> DefaultComparer = (x, y) => true;

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (this == other)
				return true;

			var expr = other as SqlExpression;

			if (expr == null || SystemType != expr.SystemType || Expr != expr.Expr || Parameters.Length != expr.Parameters.Length)
				return false;

			for (var i = 0; i < Parameters.Length; i++)
				if (!Parameters[i].Equals(expr.Parameters[i], comparer))
					return false;

			return comparer(this, other);
		}
	
		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
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

		public QueryElementType ElementType { get { return QueryElementType.SqlExpression; } }

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
				case QueryElementType.SqlFunction :

					var f = (SqlFunction)ex;

					switch (f.Name)
					{
						case "EXISTS" : return false;
					}

					return true;
			}

			return false;
		}

		#endregion
	}
}
