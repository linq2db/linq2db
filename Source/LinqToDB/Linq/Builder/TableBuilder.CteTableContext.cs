using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	partial class TableBuilder
	{
		static IBuildContext BuildCteContext(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			Expression  bodyExpr;
			IQueryable? query = null;
			string?     name  = null;
			bool        isRecursive = false;

			switch (methodCall.Arguments.Count)
			{
				case 1 :
					bodyExpr = methodCall.Arguments[0].Unwrap();
					break;
				case 2 :
					bodyExpr = methodCall.Arguments[0].Unwrap();
					name     = methodCall.Arguments[1].EvaluateExpression() as string;
					break;
				case 3 :
					query    = methodCall.Arguments[0].EvaluateExpression() as IQueryable;
					bodyExpr = methodCall.Arguments[1].Unwrap();
					name     = methodCall.Arguments[2].EvaluateExpression() as string;
					isRecursive = true;
					break;
				default:
					throw new InvalidOperationException();
			}

			bodyExpr = builder.ConvertExpression(bodyExpr);
			var cteContext = builder.RegisterCte(query, bodyExpr, () => new CteClause(null, bodyExpr.Type.GetGenericArguments()[0], isRecursive, name));

			var objectType      = methodCall.Method.GetGenericArguments()[0];
			var cteTableContext = new CteTableContext(builder, buildInfo.Parent, objectType, buildInfo.SelectQuery, cteContext, buildInfo.IsTest);
			cteTableContext.EnsureInitialized();

			// populate all fields
			if (isRecursive)
				_ = builder.MakeExpression(cteContext, new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], cteContext), ProjectFlags.SQL);

			return cteTableContext;
		}

		static CteTableContext BuildCteContextTable(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var queryable = (IQueryable)buildInfo.Expression.EvaluateExpression()!;
			var cteContext = builder.RegisterCte(queryable, null, () => new CteClause(null, queryable.ElementType, false, ""));

			var cteTableContext = new CteTableContext(builder, buildInfo.Parent, queryable.ElementType, buildInfo.SelectQuery, cteContext, buildInfo.IsTest);
			cteTableContext.EnsureInitialized();

			return cteTableContext;
		}

		public class CteTableContext: IBuildContext
		{
#if DEBUG
			public string SqlQueryText => SelectQuery == null ? "" : SelectQuery.SqlText;
			public string Path         => this.GetPath();
			public int    ContextId    { get; private set; }
#endif

			CteContext _cteContext;

			public SelectQuery    SelectQuery { get; set; }
			public SqlStatement?  Statement   { get; set; }
			public IBuildContext? Parent      { get; set; }

			public ExpressionBuilder Builder    { get; }
			public Expression?       Expression { get; }
			public SqlCteTable       CteTable   { get; }

			public CteTableContext(ExpressionBuilder builder, IBuildContext? parent, Type objectType, SelectQuery selectQuery, CteContext cteContext, bool isTest)
			{
				Builder     = builder;
				Parent      = parent;
				_cteContext = cteContext;
				SelectQuery = selectQuery;
				CteTable    = new SqlCteTable(objectType, _cteContext.CteClause);

				if (!isTest)
					SelectQuery.From.Table(CteTable);

#if DEBUG
				ContextId = Builder.GenerateContextId();
#endif
			}

			public void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				if (!buildInfo.CreateSubQuery)
					return this;

				var expr    = Builder.GetSequenceExpression(this);
				var context = Builder.BuildSequence(new BuildInfo(buildInfo, expr));

				return context;
			}

			public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			Dictionary<SqlPlaceholderExpression, SqlPlaceholderExpression> _fieldsMap = new ();

			public Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot) || flags.HasFlag(ProjectFlags.Table))
					return path;

				if (flags.HasFlag(ProjectFlags.Expand))
				{
					_cteContext.InitQuery();
					return path;
				}

				var ctePath = SequenceHelper.CorrectExpression(path, this, _cteContext);
				if (!ReferenceEquals(ctePath, path))
				{
					_cteContext.InitQuery();

					var translated = Builder.MakeExpression(_cteContext, ctePath, flags);

					if (!flags.HasFlag(ProjectFlags.Test))
					{
						// replace tracking path back
						translated = SequenceHelper.CorrectTrackingPath(translated, _cteContext, this);

						var placeholders = ExpressionBuilder.CollectPlaceholders(translated).Where(p =>
							p.SelectQuery == _cteContext.SubqueryContext?.SelectQuery && p.Index != null)
							.ToList();

						translated = RemapFields(translated, placeholders);
					}

					return translated;
				}

				return ctePath;
			}

			Expression RemapFields(Expression expression, List<SqlPlaceholderExpression> placeholders)
			{
				if (placeholders.Count == 0)
					return expression;

				var newPlaceholders = new SqlPlaceholderExpression[placeholders.Count];

				for (var index = 0; index < placeholders.Count; index++)
				{
					var placeholder = placeholders[index];
					if (!_fieldsMap.TryGetValue(placeholder, out var newPlaceholder))
					{
						var field = (SqlField)placeholder.Sql;

						var newField = new SqlField(field);
						CteTable.Add(newField);

						newPlaceholder = ExpressionBuilder.CreatePlaceholder(SelectQuery, newField,
							placeholder.TrackingPath ?? throw new InvalidOperationException(),
							index: placeholder.Index);

						_fieldsMap[placeholder] = newPlaceholder;
					}

					newPlaceholders[index] = newPlaceholder;
				}

				var transformed = expression.Transform((placeholders, newPlaceholders), (ctx, e) =>
				{
					if (e.NodeType == ExpressionType.Extension && e is SqlPlaceholderExpression placeholder)
					{
						var index = ctx.placeholders.IndexOf(placeholder);
						if (index >= 0)
						{
							return ctx.newPlaceholders[index];
						}
					}

					return e;
				});

				return transformed;
			}

			public IBuildContext Clone(CloningContext context)
			{
				throw new NotImplementedException();
			}

			public int ConvertToParentIndex(int index, IBuildContext? context)
			{
				throw new NotImplementedException();
			}

			public void SetAlias(string? alias)
			{
			}

			public ISqlExpression? GetSubQuery(IBuildContext context)
			{
				throw new NotImplementedException();
			}

			public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();
			}

			public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public SqlStatement GetResultStatement()
			{
				return Statement ??= new SqlSelectStatement(SelectQuery);
			}

			public void CompleteColumns()
			{
				throw new NotImplementedException();
			}

			public void EnsureInitialized()
			{
			}

		}
	}
}
