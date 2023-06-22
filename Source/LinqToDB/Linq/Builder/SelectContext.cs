using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using SqlQuery;

	// This class implements double functionality (scalar and member type selects)
	// and could be implemented as two different classes.
	// But the class means to have a lot of inheritors, and functionality of the inheritors
	// will be doubled as well. So lets double it once here.
	//

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class SelectContext : BuildContextBase
	{
		#region Init

#if DEBUG
		public MethodCallExpression? Debug_MethodCall;
#endif

		public Expression     Body       { [DebuggerStepThrough] get; set; }
		public bool           IsSubQuery { get; }

		public override Expression? Expression => Body;

		public readonly Dictionary<MemberInfo,Expression> Members = new (new MemberInfoComparer());

		public SelectContext(IBuildContext? parent, ExpressionBuilder builder, Expression body, SelectQuery selectQuery, bool isSubQuery)
			: base(builder, body.Type, selectQuery)
		{
			Parent     = parent;
			IsSubQuery = isSubQuery;
			Body       = body;
		}

		public SelectContext(IBuildContext? parent, LambdaExpression lambda, bool isSubQuery, params IBuildContext[] sequences)
			: this(parent, SequenceHelper.PrepareBody(lambda, sequences), sequences[0], isSubQuery)
		{
		}

		public SelectContext(IBuildContext? parent, Expression body, IBuildContext sequence, bool isSubQuery)
			: this(parent, sequence.Builder, body, sequence.SelectQuery, isSubQuery)
		{
		}

		#endregion

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			Expression result;

			if (SequenceHelper.IsSameContext(path, this))
			{
				// Eager load case
				if ((flags.IsExpand() || flags.IsSql() || flags.IsExpression()) && !(path.Type.IsAssignableFrom(ElementType) || ElementType.IsAssignableFrom(path.Type)) && !path.Type.IsValueType)
				{
					return path;
				}

				if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot) /*|| flags.HasFlag(ProjectFlags.Expand)*/ || flags.HasFlag(ProjectFlags.Table) || flags.HasFlag(ProjectFlags.Traverse))
				{
					if (Body is ContextRefExpression bodyRef)
					{
						// updating type for Inheritance mapping
						//
						return bodyRef.WithType(path.Type);
					}

					if (Body.NodeType == ExpressionType.MemberAccess)
					{
						return Body;
					}

					if (Body.NodeType == ExpressionType.TypeAs)
					{
						result = Builder.Project(this, path, null, 0, flags, Body, true);
						return result;
					}

					return path;
				}

				/*
				if (!(path.Type.IsSameOrParentOf(Body.Type) || Body.Type.IsSameOrParentOf(path.Type)))
				{
					if (flags.IsExpression())
						return new SqlEagerLoadExpression((ContextRefExpression)path, path, GetEagerLoadExpression(path));
					return ExpressionBuilder.CreateSqlError(this, path);
				}
				*/

				if (Body.NodeType == ExpressionType.TypeAs)
				{
					result = Builder.Project(this, path, null, 0, flags, Body, true);
					return result;
				}

				result = Body;
			}
			else
			{
				// We can omit strict for expression building. It will help to do not crash when user uses Automapper and it tries to map non accessible fields
				//
				result = Builder.Project(this, path, null, 0, flags, Body, strict: !flags.IsExpression());

				if (!ReferenceEquals(result, Body))
				{
					if (!flags.IsTable())
					{
						if ((flags.IsRoot() || flags.IsTraverse()) &&
						    !(result is ContextRefExpression || result is MemberExpression ||
						      result is MethodCallExpression))
						{
							return path;
						}
					}
				}
			}

			return result;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var sc = context.CloneElement(SelectQuery);
			return new SelectContext(null, Builder, context.CloneExpression(Body), sc, IsSubQuery);
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		public virtual Expression GetEagerLoadExpression(Expression path)
		{
			return Builder.GetSequenceExpression(this);
		}

		public virtual void SetAlias(string? alias)
		{
			if (!string.IsNullOrEmpty(alias) && !alias!.Contains("<") && SelectQuery.Select.From.Tables.Count == 1)
			{
				SelectQuery.Select.From.Tables[0].Alias = alias;
			}
		}

		public override SqlStatement GetResultStatement()
		{
			return new SqlSelectStatement(SelectQuery);
		}

		public override void CompleteColumns()
		{
			ExpressionBuilder.EnsureAggregateColumns(this, SelectQuery);
		}
	}
}
