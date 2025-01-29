using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.XPath;

using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	abstract class BuildProxyBase<TOwner> : BuildContextBase
		where TOwner: IBuildContext
	{
		public TOwner               OwnerContext    { get; }
		public IBuildContext        BuildContext    { get; }
		public ContextRefExpression OwnerContextRef { get; }
		public Expression           CurrentPath     { get; }
		public Expression           InnerExpression { get; }

		public BuildProxyBase(TOwner ownerContext, IBuildContext buildContext, Expression currentPath, Expression innerExpression) 
			: base(ownerContext.TranslationModifier, ownerContext.Builder, innerExpression.Type, buildContext.SelectQuery)
		{
			OwnerContext    = ownerContext;
			BuildContext    = buildContext;
			CurrentPath     = currentPath;
			InnerExpression = innerExpression;

			OwnerContextRef = new ContextRefExpression(ownerContext.ElementType, ownerContext);
		}

		public abstract Expression HandleTranslated(Expression? path, SqlPlaceholderExpression placeholder);

		public abstract BuildProxyBase<TOwner> CreateProxy(TOwner ownerContext, IBuildContext buildContext, Expression currentPath, Expression innerExpression);

		public override MappingSchema MappingSchema => OwnerContext.MappingSchema;
		public override Expression    MakeExpression(Expression path,  ProjectFlags flags)
		{
			if (flags.IsRoot() || flags.IsAssociationRoot())
				return path;

			Expression currentExpression;
			Expression ownersPath;

			if (path is MemberExpression member)
			{
				currentExpression = member.Update(InnerExpression);
				ownersPath = member.Update(CurrentPath);
			}
			else if (path is ContextRefExpression contextRefExpression)
			{
				currentExpression = InnerExpression;
				ownersPath = CurrentPath;
			}
			else
				return path;

			var buildFlags = BuildFlags.ResetPrevious;

			if (flags.IsKeys())
				buildFlags |= BuildFlags.ForKeys;

			var translated = Builder.BuildExpression(BuildContext, currentExpression, buildFlags: buildFlags);

			if (ExpressionEqualityComparer.Instance.Equals(translated, currentExpression))
				return path;

			var handled = ProcessTranslated(translated, ownersPath);

			if (handled != null)
				return handled;

			return path;
		}

		protected Expression? ProcessTranslated(Expression expression, Expression? toPath)
		{
			switch (expression)
			{
				case ContextRefExpression refExpr:
				{
					if (toPath == null)
						return null;

					var newProxy = CreateProxy(OwnerContext, BuildContext, toPath, refExpr);
					return SequenceHelper.CreateRef(newProxy);
				}

				case MemberExpression memberExpression:
				{
					if (memberExpression.Expression != null)
					{
						var processed = ProcessTranslated(memberExpression.Expression, toPath);
						if (processed == null)
							return null;
						return memberExpression.Update(processed);
					}

					break;
				}

				case SqlGenericConstructorExpression generic:
				{
					List<SqlGenericConstructorExpression.Assignment>? assignments = null;
					List<SqlGenericConstructorExpression.Parameter>?  parameters  = null;

					for (int i = 0; i < generic.Assignments.Count; i++)
					{
						var assignment = generic.Assignments[i];

						var newExpression = ProcessTranslated(
						assignment.Expression,
							toPath == null ? toPath : Expression.MakeMemberAccess(toPath, assignment.MemberInfo)
						);

						if (newExpression == null)
							return null;

						if (!ReferenceEquals(assignment.Expression, newExpression))
						{
							if (assignments == null)
							{
								assignments = new();
								for (int j = 0; j < i; j++)
								{
									assignments.Add(generic.Assignments[j]);
								}
							}

							assignments.Add(assignment.WithExpression(newExpression));
						}
						else
							assignments?.Add(assignment);
					}

					for (int i = 0; i < generic.Parameters.Count; i++)
					{
						var parameter = generic.Parameters[i];

						Expression? paramAccess = null;

						if (toPath != null)
						{
							if (parameter.MemberInfo != null)
							{
								paramAccess = Expression.MakeMemberAccess(toPath, parameter.MemberInfo);
							}
							else
							{
								paramAccess = new SqlGenericParamAccessExpression(toPath, parameter.ParameterInfo);
							}
						}

						var newExpression = ProcessTranslated(parameter.Expression, paramAccess);

						if (newExpression == null)
							return null;

						if (!ReferenceEquals(parameter.Expression, newExpression))
						{
							if (parameters == null)
							{
								parameters = new();
								for (int j = 0; j < i; j++)
								{
									parameters.Add(generic.Parameters[j]);
								}
							}

							parameters.Add(parameter.WithExpression(newExpression));
						}
						else
							parameters?.Add(parameter);
					}

					if (assignments != null)
					{
						generic = generic.ReplaceAssignments(assignments.AsReadOnly());
					}

					if (parameters != null)
					{
						generic = generic.ReplaceParameters(parameters.AsReadOnly());
					}

					if (toPath != null)
						generic = generic.WithConstructionRoot(toPath);

					return generic;
				}

				case NewExpression or MemberInitExpression:
				{
					return ProcessTranslated(Builder.ParseGenericConstructor(expression, ProjectFlags.SQL, null), toPath);
				}

				case SqlPlaceholderExpression placeholder:
				{
					return HandleTranslated(toPath, placeholder);
				}

				case { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked }:
				{
					var unary = (UnaryExpression)expression;
					var processed = ProcessTranslated(unary.Operand, toPath);
					if (processed == null)
						return null;
					return unary.Update(processed);
				}

				case SqlAdjustTypeExpression adjust:
				{
					var processed = ProcessTranslated(adjust.Expression, toPath);
					if (processed == null)
						return null;
					return adjust.Update(processed);
				}

				case SqlDefaultIfEmptyExpression defaultIfEmptyExpression:
				{
					var processed = ProcessTranslated(defaultIfEmptyExpression.InnerExpression, toPath);
					if (processed == null)
						return null;

					var notNull = defaultIfEmptyExpression.NotNullExpressions.Select(n => ProcessTranslated(n, null)!)
						.ToList();

					if (notNull.Any(n => n == null))
						return null;

					return defaultIfEmptyExpression.Update(processed, notNull.AsReadOnly());
				}
			}

			if (null != expression.Find(1, (_, x) => x is SqlPlaceholderExpression or ContextRefExpression))
				return null;

			return expression;
		}

		public override void SetRunQuery<T>(Query<T>   query, Expression   expr)
		{
			throw new NotImplementedException();
		}

		public override SqlStatement  GetResultStatement()
		{
			throw new NotImplementedException();
		}
	}
}
