using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	interface IBuildProxy
	{
		public IBuildContext Owner           { get; }
		public Expression    InnerExpression { get; }
		public Expression    HandleTranslated(Expression? path, SqlPlaceholderExpression placeholder);
	}

	abstract class BuildProxyBase<TOwner> : BuildContextBase, IBuildProxy
		where TOwner: IBuildContext
	{
		public IBuildContext        Owner           => OwnerContext;
		public TOwner               OwnerContext    { get; }
		public IBuildContext        BuildContext    { get; }
		public ContextRefExpression OwnerContextRef { get; }
		public Expression?          CurrentPath     { get; }
		public Expression           InnerExpression { get; }

		public BuildProxyBase(TOwner ownerContext, IBuildContext buildContext, Expression? currentPath, Expression innerExpression) 
			: base(ownerContext.TranslationModifier, ownerContext.Builder, innerExpression.Type, buildContext.SelectQuery)
		{
			OwnerContext    = ownerContext;
			BuildContext    = buildContext;
			CurrentPath     = currentPath;

			InnerExpression = innerExpression;

			OwnerContextRef = new ContextRefExpression(ownerContext.ElementType, ownerContext);
		}

		public abstract Expression    HandleTranslated(Expression? path, SqlPlaceholderExpression placeholder);

		public abstract BuildProxyBase<TOwner> CreateProxy(TOwner ownerContext, IBuildContext buildContext, Expression? currentPath, Expression innerExpression);

		public override MappingSchema MappingSchema => OwnerContext.MappingSchema;
		public override Expression    MakeExpression(Expression path,  ProjectFlags flags)
		{
			if (flags.IsRoot() || flags.IsAssociationRoot())
				return path;

			Expression currentExpression;
			Expression? ownersPath;

			if (path is MemberExpression member)
			{
				if (member.Member.DeclaringType?.IsAssignableFrom(InnerExpression.Type) == true && (CurrentPath == null || member.Member.DeclaringType?.IsAssignableFrom(CurrentPath.Type) == true))
				{
					currentExpression = member.Update(InnerExpression);
					ownersPath        = CurrentPath == null ? null : member.Update(CurrentPath);
				}
				else
				{
					return path;
				}
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

			if (!(flags.IsExpression() && !flags.IsForSetProjection())) 
			{
				if (ExpressionEqualityComparer.Instance.Equals(translated, currentExpression))
					return path;

				if (translated is ContextRefExpression { BuildContext: var buildContext } && buildContext.Equals(OwnerContext))
					return path;
			}

			var handled = ProcessTranslated(translated, ownersPath);

			return handled;
		}

		protected Expression ProcessTranslated(Expression expression, Expression? toPath)
		{
			switch (expression)
			{
				case ContextRefExpression refExpr:
				{
					var newProxy = CreateProxy(OwnerContext, BuildContext, toPath, refExpr);
					return SequenceHelper.CreateRef(newProxy);
				}

				case MemberExpression memberExpression:
				{
					if (memberExpression.Expression != null && toPath is MemberExpression toMember)
					{
						var processed = ProcessTranslated(memberExpression.Expression, toMember.Expression);
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
					return unary.Update(processed);
				}

				case SqlAdjustTypeExpression adjust:
				{
					var processed = ProcessTranslated(adjust.Expression, toPath);
					return adjust.Update(processed);
				}

				case SqlDefaultIfEmptyExpression defaultIfEmptyExpression:
				{
					var processed = ProcessTranslated(defaultIfEmptyExpression.InnerExpression, toPath);

					var notNull = defaultIfEmptyExpression.NotNullExpressions.Select(n => ProcessTranslated(n, null)!)
						.ToList();

					return defaultIfEmptyExpression.Update(processed, notNull.AsReadOnly());
				}
			}

			var visitor = new BuildProxyVisitor(this);
			var result = visitor.Visit(expression);

			return result;
		}

		public override void SetRunQuery<T>(Query<T>   query, Expression   expr)
		{
			throw new NotImplementedException();
		}

		public override SqlStatement  GetResultStatement()
		{
			throw new NotImplementedException();
		}

		protected bool Equals(BuildProxyBase<TOwner> other)
		{
			return EqualityComparer<TOwner>.Default.Equals(OwnerContext, other.OwnerContext)
			       && BuildContext.Equals(other.BuildContext)
			       && OwnerContextRef.Equals(other.OwnerContextRef)
			       && ExpressionEqualityComparer.Instance.Equals(CurrentPath, other.CurrentPath)
			       && ExpressionEqualityComparer.Instance.Equals(InnerExpression, other.InnerExpression);
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((BuildProxyBase<TOwner>)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = EqualityComparer<TOwner>.Default.GetHashCode(OwnerContext);
				hashCode = (hashCode * 397) ^ BuildContext.GetHashCode();
				hashCode = (hashCode * 397) ^ OwnerContextRef.GetHashCode();
				hashCode = (hashCode * 397) ^ (CurrentPath != null ? ExpressionEqualityComparer.Instance.GetHashCode(CurrentPath) : 0);
				hashCode = (hashCode * 397) ^ ExpressionEqualityComparer.Instance.GetHashCode(InnerExpression);
				return hashCode;
			}
		}

		sealed class BuildProxyVisitor : ExpressionVisitorBase
		{
			public BuildProxyVisitor(BuildProxyBase<TOwner> proxy)
			{
				Proxy = proxy;
			}

			BuildProxyBase<TOwner> Proxy { get; }

			internal override Expression VisitContextRefExpression(ContextRefExpression node)
			{
				var newProxy = Proxy.CreateProxy(Proxy.OwnerContext, Proxy.BuildContext, null, node);
				return SequenceHelper.CreateRef(newProxy);
			}
		}
	}
}
