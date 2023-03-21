using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;
	using Common;

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

			return cteTableContext;
		}

		public sealed class CteTableContext: BuildContextBase, ITableContext
		{
			public CteContext  CteContext { get; }
			public SqlCteTable CteTable   { get; }

			public Type          ObjectType   => CteTable.ObjectType;
			public SqlTable      SqlTable     => CteTable;
			public LoadWithInfo  LoadWithRoot { get; set; } = new();
			public MemberInfo[]? LoadWithPath { get; set; }

			public CteTableContext(ExpressionBuilder builder, IBuildContext? parent, Type objectType, SelectQuery selectQuery, CteContext cteContext, bool isTest) 
				: base(builder, objectType, selectQuery)
			{
				Parent     = parent;
				CteContext = cteContext;
				CteTable = new SqlCteTable(CteContext.CteClause,
					builder.MappingSchema.GetEntityDescriptor(objectType,
						builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated));

				if (!isTest)
					SelectQuery.From.Table(CteTable);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				if (!buildInfo.CreateSubQuery)
					return this;

				var expr    = Builder.GetSequenceExpression(this);
				var context = Builder.BuildSequence(new BuildInfo(buildInfo, expr));

				return context;
			}

			Dictionary<SqlPlaceholderExpression, SqlPlaceholderExpression> _fieldsMap = new ();

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot) || flags.HasFlag(ProjectFlags.Table))
					return path;

				if (flags.HasFlag(ProjectFlags.Expand))
				{
					CteContext.InitQuery();
					return path;
				}

				var ctePath = SequenceHelper.CorrectExpression(path, this, CteContext);
				if (!ReferenceEquals(ctePath, path))
				{
					CteContext.InitQuery();

					var translated = Builder.MakeExpression(CteContext, ctePath, flags);

					if (!flags.HasFlag(ProjectFlags.Test))
					{
						// replace tracking path back
						translated = SequenceHelper.CorrectTrackingPath(translated, path);

						var placeholders = ExpressionBuilder.CollectPlaceholders(translated).Where(p =>
							p.SelectQuery == CteContext.SubqueryContext?.SelectQuery && p.Index != null)
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
						var field = placeholder.Sql as SqlField;

						var newField = field != null
							? new SqlField(field)
							: null;

						if (newField == null)
						{
							newField = new SqlField(QueryHelper.GetDbDataType(placeholder.Sql), "field",
								placeholder.Sql.CanBeNullable(NullabilityContext.NonQuery));

							Utils.MakeUniqueNames(new []{newField}, staticNames: CteTable.Fields.Select(f => f.Name), f => f.Name, (f, n, _) => f.Name = n);
						}

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

			public override IBuildContext Clone(CloningContext context)
			{
				throw new NotImplementedException();
			}

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(SelectQuery);
			}
		}
	}
}
