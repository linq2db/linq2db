using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using SqlQuery;
	using Common;

	// This class implements double functionality (scalar and member type selects)
	// and could be implemented as two different classes.
	// But the class means to have a lot of inheritors, and functionality of the inheritors
	// will be doubled as well. So lets double it once here.
	//

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class SelectContext : IBuildContext
	{
		#region Init

#if DEBUG
		public string SqlQueryText  => SelectQuery == null ? "" : SelectQuery.SqlText;
		public string Path          => this.GetPath();
		public int    ContextId     { get; }

		public MethodCallExpression? Debug_MethodCall;
#endif

		public IBuildContext[]   Sequence    { [DebuggerStepThrough] get; }
		public LambdaExpression  Lambda      { [DebuggerStepThrough] get; set; }
		public Expression        Body        { [DebuggerStepThrough] get; set; }
		public ExpressionBuilder Builder     { [DebuggerStepThrough] get; }
		public SelectQuery       SelectQuery { [DebuggerStepThrough] get; set; }
		public SqlStatement?     Statement   { [DebuggerStepThrough] get; set; }
		public IBuildContext?    Parent      { [DebuggerStepThrough] get; set; }

		Expression IBuildContext.Expression => Lambda;

		public readonly Dictionary<MemberInfo,Expression> Members = new (new MemberInfoComparer());

		public SelectContext(IBuildContext? parent, ExpressionBuilder builder, LambdaExpression lambda, SelectQuery selectQuery, bool isSubQuery)
		{
			Parent      = parent;
			Sequence    = Array<IBuildContext>.Empty;
			Builder     = builder;
			Lambda      = lambda;
			Body        = lambda.Body;
			SelectQuery = selectQuery;
			IsSubQuery  = isSubQuery;

			Builder.Contexts.Add(this);
#if DEBUG
			ContextId = builder.GenerateContextId();
#endif
		}

		public SelectContext(IBuildContext? parent, LambdaExpression lambda, bool isSubQuery, params IBuildContext[] sequences)
		{
			Parent     = parent;
			Sequence   = sequences;
			Builder    = sequences[0].Builder;
			Lambda     = lambda;
			IsSubQuery = isSubQuery;
			Body       = SequenceHelper.PrepareBody(lambda, sequences);

			SelectQuery   = sequences[0].SelectQuery;

			foreach (var context in Sequence)
				context.Parent = this;

			Builder.Contexts.Add(this);
#if DEBUG
			ContextId = Builder.GenerateContextId();
#endif
		}

		#endregion

		#region BuildQuery

		public virtual void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			throw new NotImplementedException();

			/*var expr = Builder.FinalizeProjection(this,
				Builder.MakeExpression(new ContextRefExpression(typeof(T), this), ProjectFlags.Expression));

			var mapper = Builder.BuildMapper<T>(expr);

			QueryRunner.SetRunQuery(query, mapper);*/
		}

		#endregion

		#region BuildExpression

		public virtual Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
				{
			throw new NotImplementedException();
		}

		#endregion

		#region ConvertToSql

		public virtual SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
					throw new NotImplementedException();
				}

		#endregion

		#region ConvertToIndex

		public virtual SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
				}

		#endregion

		public virtual Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			Expression result;

			if (SequenceHelper.IsSameContext(path, this))
			{
				if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot))
				{
					if (Body is ContextRefExpression bodyRef)
					{
						// updating type for Inheritance mapping
						//
						return bodyRef.WithType(path.Type);
					}

					if (Body is MemberExpression me)
					{
						return Body;
					}

					return path;
				}

				if (!path.Type.IsSameOrParentOf(Body.Type) && flags.HasFlag(ProjectFlags.Expression))
					return new SqlEagerLoadExpression((ContextRefExpression)path, path, GetEagerLoadExpression(path));

				result = Body;
			}
			else
			{
				result = Builder.Project(this, path, null, 0, flags, Body);

				if (!ReferenceEquals(result, Body))
				{
					if (flags.HasFlag(ProjectFlags.Root) &&
					    !(result is ContextRefExpression || result is MemberExpression ||
					      result is MethodCallExpression))
					{
						return path;
					}
				}
			}

			return result;
		}

		public virtual IBuildContext Clone(CloningContext context)
		{
			return new SelectContext(null, context.Correct(Lambda), IsSubQuery,
				Sequence.Select(s => context.CloneContext(s)).ToArray());
		}

		public void SetRunQuery<T>(Query<T> query)
		{
		}

		public bool IsExecuteOnly { get; }

		public virtual Expression GetEagerLoadExpression(Expression path)
		{
			return Builder.GetSequenceExpression(this);
		}

		#region IsExpression

		public virtual IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region GetContext

		public virtual IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			return null;
		}

		#endregion

		#region ConvertToParentIndex

		public virtual int ConvertToParentIndex(int index, IBuildContext context)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region SetAlias

		public virtual void SetAlias(string? alias)
		{
			if (!string.IsNullOrEmpty(alias) && !alias!.Contains('<') && SelectQuery.Select.From.Tables.Count == 1)
			{
				SelectQuery.Select.From.Tables[0].Alias = alias;
			}
		}

		#endregion

		#region GetSubQuery

		public ISqlExpression? GetSubQuery(IBuildContext context)
		{
			return null;
		}

		#endregion

		public virtual SqlStatement GetResultStatement()
		{
			return Statement ??= new SqlSelectStatement(SelectQuery);
		}

		public virtual void CompleteColumns()
		{
			ExpressionBuilder.EnsureAggregateColumns(this, SelectQuery);

			foreach (var sequence in Sequence)
			{
				sequence.CompleteColumns();
			}
		}

		public bool IsSubQuery { get; }

		IBuildContext? GetSequence(Expression expression, int level)
		{
			return null;
		}

	}
}
