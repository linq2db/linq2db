namespace LinqToDB.SqlQuery
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Text.RegularExpressions;

	using JetBrains.Annotations;

	public class SqlExtension: ISqlExpression
	{
		public class ExtensionParam: IQueryElement
		{
			public ExtensionParam(string name, ISqlExpression expression)
			{
				Name = name;
				Expression = expression;
			}

			public string Name { get; set; }
			public ISqlExpression Expression { get; set; }

			#region Implementation of IQueryElement

			public QueryElementType ElementType { get { return QueryElementType.SqlExtensionParam; } }
			public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				//TODO: Impelement
				return sb;
			}

			#endregion
		}

		public Dictionary<string, List<ExtensionParam>> NamedParameters
		{
			get { return _namedParameters; }
		}

		readonly Dictionary<string, List<ExtensionParam>> _namedParameters;
		public int ChainPrecedence { get; set; }

		public SqlExtension(Type systemType, string expr, int precedence, int chainPrecedence, params ExtensionParam[] parameters)
		{
			if (parameters == null) throw new ArgumentNullException("parameters");

			foreach (var value in parameters)
				if (value == null) throw new ArgumentNullException("parameters");

			SystemType       = systemType;
			Expr             = expr;
			Precedence       = precedence;
			ChainPrecedence  = chainPrecedence;
			_namedParameters = parameters.ToLookup(p => p.Name).ToDictionary(p => p.Key, p => p.ToList());
		}


		public SqlExtension(string expr, int precedence, int chainPrecedence, params ExtensionParam[] parameters)
			: this(null, expr, precedence, chainPrecedence, parameters)
		{
		}

		public SqlExtension(Type systemType, string expr, params ExtensionParam[] parameters)
			: this(systemType, expr, SqlQuery.Precedence.Unknown, 0, parameters)
		{
		}

		public SqlExtension(string expr, params ExtensionParam[] parameters)
			: this(null, expr, SqlQuery.Precedence.Unknown, 0, parameters)
		{
		}

		public Type             SystemType { get;         set; }
		public string           Expr       { get;         set; }
		public int              Precedence { get; private set; }
		public bool             IsRoot     { get; set;         }

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
			foreach (var pair in NamedParameters)
			{
				for (var i = 0; i < pair.Value.Count; i++)
					pair.Value[i].Expression = pair.Value[i].Expression.Walk(skipColumns, func);
			}
				
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

				return NamedParameters.Values.SelectMany(v => v).Any(p => p.Expression.CanBeNull);
			}

			set { _canBeNull = value; }
		}

		internal static Func<ISqlExpression,ISqlExpression,bool> DefaultComparer = (x, y) => true;

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (this == other)
				return true;

			var expr = other as SqlExtension;

			if (expr == null || SystemType != expr.SystemType || Expr != expr.Expr || NamedParameters.Count != expr.NamedParameters.Count
				|| NamedParameters.Count != expr.NamedParameters.Count)
				return false;

			foreach (var pair in NamedParameters)
			{
				List<ExtensionParam> second;
				if (!expr.NamedParameters.TryGetValue(pair.Key, out second))
					return false;
				if (pair.Value.Count != second.Count)
					return false;

				for (int i = 0; i < pair.Value.Count; i++)
				{
					if (!pair.Value[i].Expression.Equals(second[i].Expression))
						return false;
				}
			}

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
				objectTree.Add(this, clone = new SqlExtension(
					SystemType,
					Expr,
					Precedence,
					ChainPrecedence,
					NamedParameters.Values.SelectMany(p => p)
						.Select(e => new ExtensionParam(e.Name, (ISqlExpression) e.Expression.Clone(objectTree, doClone)))
						.ToArray()));
			}

			return clone;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.SqlExtension; } }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			//TODO: Implement
			var len = sb.Length;
			var ss  = NamedParameters.Values.SelectMany(p => p).Select(p =>
			{
				p.Expression.ToString(sb, dic);
				var s = sb.ToString(len, sb.Length - len);
				sb.Length = len;
				return (object)s;
			});
			
			return sb.AppendFormat(Expr, ss.ToArray());
		}

		#endregion

		public ExtensionParam AddParameter(string name, ISqlExpression sqlExpression)
		{
			return AddParameter(new ExtensionParam(name ?? string.Empty, sqlExpression));
		}

		public ExtensionParam AddParameter(ExtensionParam param)
		{
			List<ExtensionParam> list;
			if (!_namedParameters.TryGetValue(param.Name, out list))
			{
				list = new List<ExtensionParam>();
				_namedParameters.Add(param.Name, list);
			}
			list.Add(param);
			return param;
		}

		public static string ResolveExpressionValues([NotNull] string expression, [NotNull] Func<string, string, string> valueProvider)
		{
			if (expression == null) throw new ArgumentNullException("expression");
			if (valueProvider == null) throw new ArgumentNullException("valueProvider");

			const string pattern = @"{([0-9a-z_A-Z?]*)(,\s'(.*)')?}";

			int  prevMatch         = -1;
			int  prevNotEmptyMatch = -1;
			bool spaceNeeded       = false;

			var str = Regex.Replace(expression, pattern, match =>
			{
				var paramName = match.Groups[1].Value;
				var canBeOptional = paramName.EndsWith("?");
				if (canBeOptional)
					paramName = paramName.TrimEnd('?');

				if (paramName == "_")
				{
					spaceNeeded = true;
					prevMatch = match.Index + match.Length;
					return string.Empty;
				}

				var delimiter  = match.Groups[3].Success ? match.Groups[3].Value : null;
				var calculated = valueProvider(paramName, delimiter);

				if (string.IsNullOrEmpty(calculated) && !canBeOptional)
					throw new InvalidOperationException(string.Format("Non optional parameter '{0}' not found", paramName));

				var res = calculated;
				if (spaceNeeded)
				{
					if (!string.IsNullOrEmpty(calculated))
					{
						var e = expression;
						if (prevMatch == match.Index && prevNotEmptyMatch == match.Index - 3 || (prevNotEmptyMatch >= 0 && e[prevNotEmptyMatch] != ' '))
							res = " " + calculated;
					}
					spaceNeeded = false;
				}

				if (!string.IsNullOrEmpty(calculated))
				{
					prevNotEmptyMatch = match.Index + match.Length;
				}

				return res;
			});

			return str;
		}

		public IEnumerable<ExtensionParam> GetParametersByName(string name)
		{
			List<ExtensionParam> list;
			if (_namedParameters.TryGetValue(name, out list))
				return list;
			return Enumerable.Empty<ExtensionParam>();
		}

		public ExtensionParam[] GetParameters()
		{
			return _namedParameters.Values.SelectMany(_ => _).ToArray();
		}
	}

}