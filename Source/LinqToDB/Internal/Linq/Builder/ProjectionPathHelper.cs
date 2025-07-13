using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	sealed class ProjectionPathHelper
	{
		public ExpressionBuilder  Builder       { get; }
		public MappingSchema      MappingSchema { get; }
		public TraverseProjection TraverseFunc  { get; }

		public delegate Expression TraverseProjection(ExpressionBuilder builder, Expression path, Expression expression);

		public ProjectionPathHelper(ExpressionBuilder builder, MappingSchema mappingSchema, TraverseProjection traverseFunc)
		{
			Builder       = builder;
			MappingSchema = mappingSchema;
			TraverseFunc  = traverseFunc;
		}

		[return: NotNullIfNotNull(nameof(expression))]
		public Expression? AnalyseExpression(Expression? expression, Expression root)
		{
			if (expression == null)
				return null;

			expression = TraverseFunc(Builder, root, expression);

			if (root is not (ContextRefExpression or MemberExpression))
				return expression;

			switch (expression)
			{
				case SqlGenericConstructorExpression generic:
				{
					List<SqlGenericConstructorExpression.Assignment>? assignments = null;
					List<SqlGenericConstructorExpression.Parameter>?  parameters  = null;

					for (int i = 0; i < generic.Assignments.Count; i++)
					{
						var assignment = generic.Assignments[i];

						var currentPath = root;

						var applicable = true;
						if (assignment.MemberInfo.DeclaringType != null)
						{
							applicable = assignment.MemberInfo.DeclaringType.IsAssignableFrom(currentPath.Type);
							if (applicable)
								currentPath = currentPath.EnsureType(assignment.MemberInfo.DeclaringType);
						}

						if (!applicable)
						{
							assignments?.Add(assignment);
							continue;
						}

						var memberTrackingPath = Expression.MakeMemberAccess(currentPath, assignment.MemberInfo);
						var newExpression      = AnalyseExpression(assignment.Expression, memberTrackingPath);

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

					if (assignments != null)
					{
						generic = generic.ReplaceAssignments(assignments.AsReadOnly());
					}

					for (var i = 0; i < generic.Parameters.Count; i++)
					{
						var parameter     = generic.Parameters[i];
						var currentPath   = root;
						var newExpression = parameter.Expression;

						if (parameter.MemberInfo != null)
						{
							var memberTrackingPath = Expression.MakeMemberAccess(currentPath, parameter.MemberInfo);
							newExpression = AnalyseExpression(parameter.Expression, memberTrackingPath);
						}

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
							parameters?.Add(parameter.WithExpression(newExpression));

					}

					if (parameters != null)
					{
						generic = generic.ReplaceParameters(parameters.AsReadOnly());
					}

					return generic;
				}

				case NewExpression or MemberInitExpression:
				{
					var parsed = Builder.ParseGenericConstructor(expression, ProjectFlags.SQL, null);
					if (!ReferenceEquals(parsed, expression))
						return AnalyseExpression(parsed, root);
					break;
				}

				case SqlDefaultIfEmptyExpression defaultIfEmptyExpression:
				{
					var newExpr = defaultIfEmptyExpression.Update(
						AnalyseExpression(defaultIfEmptyExpression.InnerExpression, root),
						defaultIfEmptyExpression.NotNullExpressions
					);

					return newExpr;
				}

				case ConstantExpression:
					return expression;

				case ConditionalExpression conditional:
				{
					return conditional.Update(
						conditional.Test,
						AnalyseExpression(conditional.IfTrue, root),
						AnalyseExpression(conditional.IfFalse, root));
				}
			}

			return expression;
		}
	}
}
