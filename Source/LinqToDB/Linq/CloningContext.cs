using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Linq
{
	using Builder;
	using SqlQuery;

	class CloningContext
	{
		Dictionary<IQueryElement, IQueryElement> _queryElements    = new ();
		Dictionary<IBuildContext, IBuildContext> _buildContexts    = new ();
		HashSet<IBuildContext>                   _currentlyCloning = new ();

		public bool IsCloned(IQueryElement queryElement)
		{
			return _queryElements.ContainsKey(queryElement);
		}

		public bool IsCloned(IBuildContext buildContext)
		{
			return _buildContexts.ContainsKey(buildContext);
		}

		[return: NotNullIfNotNull("context")]
		public TContext? CorrectContext<TContext>(TContext? context)
			where TContext : IBuildContext
		{
			if (context == null)
				return default;

			if (_buildContexts.TryGetValue(context, out var replaced))
				return (TContext)replaced;

			return context;
		}

		[return: NotNullIfNotNull("element")]
		public TElement? CorrectElement<TElement>(TElement? element)
			where TElement : IQueryElement
		{
			if (element == null)
				return default;

			return (TElement)CorrectRawElement(element);
		}

		[return: NotNullIfNotNull("element")]
		IQueryElement? CorrectRawElement(IQueryElement? element)
		{
			if (element == null)
				return default;

			if (_queryElements.TryGetValue(element, out var replacement))
				return replacement;

			return element;
		}

		[return: NotNullIfNotNull("expression")]
		public TExpression? CorrectExpression<TExpression>(TExpression? expression)
			where TExpression : Expression
		{
			if (expression == null)
				return default;

			return (TExpression)CorrectExpressionRaw(expression);
		}

		[return: NotNullIfNotNull("expression")]
		Expression? CorrectExpressionRaw(Expression? expression)
		{
			if (expression == null)
				return default;

			var newExpression = expression.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Extension)
				{
					if (e is SqlPlaceholderExpression sqlPlaceholder)
					{
						return new SqlPlaceholderExpression(CorrectElement(sqlPlaceholder.SelectQuery),
							CorrectElement(sqlPlaceholder.Sql), CorrectExpression(sqlPlaceholder.Path), sqlPlaceholder.ConvertType,
							sqlPlaceholder.Alias, sqlPlaceholder.Index, CorrectExpression(sqlPlaceholder.TrackingPath));
					}

					if (e is ContextRefExpression contextRef)
					{
						return contextRef.WithContext(CorrectContext(contextRef.BuildContext));
					}
				}

				return e;
			});

			return newExpression;
		}

		[return: NotNullIfNotNull("expression")]
		public TExpression? CloneExpression<TExpression>(TExpression? expression)
			where TExpression : Expression
		{
			if (expression == null)
				return default;

			return (TExpression)CloneExpressionRaw(expression);
		}

		[return: NotNullIfNotNull("expression")]
		Expression? CloneExpressionRaw(Expression? expression)
		{
			if (expression == null)
				return default;

			var newExpression = expression.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Extension)
				{
					if (e is SqlPlaceholderExpression sqlPlaceholder)
					{
						return new SqlPlaceholderExpression(CloneElement(sqlPlaceholder.SelectQuery),
							CloneElement(sqlPlaceholder.Sql), CloneExpression(sqlPlaceholder.Path), sqlPlaceholder.ConvertType,
							sqlPlaceholder.Alias, sqlPlaceholder.Index, CloneExpression(sqlPlaceholder.TrackingPath));
					}

					if (e is ContextRefExpression contextRef)
					{
						return contextRef.WithContext(CloneContext(contextRef.BuildContext));
					}
				}

				return e;
			});

			return newExpression;
		}


		[return: NotNullIfNotNull("buildContext")]
		public TContext? CloneContext<TContext>(TContext? buildContext)
		where TContext : IBuildContext
		{
			if (buildContext == null)
				return default;

			return (TContext)CloneRaw(buildContext);
		}

		[return: NotNullIfNotNull("buildContext")]
		public IBuildContext? CloneRaw(IBuildContext? buildContext)
		{
			if (buildContext == null)
				return null;

			if (_buildContexts.TryGetValue(buildContext, out var newContext))
				return newContext;


			if (!_currentlyCloning.Add(buildContext))
				throw new InvalidOperationException("Circular context cloning.");

			newContext = buildContext.Clone(this);

			_currentlyCloning.Remove(buildContext);

			_buildContexts[buildContext] = newContext;
			return newContext;
		}

		[return: NotNullIfNotNull("queryElement")]
		public TElement? CloneElement<TElement>(TElement? queryElement)
			where TElement : IQueryElement
		{
			if (queryElement == null)
				return default;

			return (TElement)Clone(queryElement);
		}

		[return: NotNullIfNotNull("queryElement")]
		IQueryElement? Clone(IQueryElement? queryElement)
		{
			if (queryElement == null)
				return null;

			if (_queryElements.TryGetValue(queryElement, out var newElement))
				return newElement;

			newElement = queryElement.Clone(_queryElements);

			_queryElements[queryElement] = newElement;
			return newElement;
		}

		public void UpdateContextParents()
		{
			foreach (var pair in _buildContexts.ToList())
			{
				if (!ReferenceEquals(pair.Key, pair.Value))
				{
					if (pair.Key.Parent != null && pair.Value.Parent == null)
					{
						pair.Value.Parent = CloneRaw(pair.Key.Parent);
					}
				}
			}
		}
	}
}
