using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall("InsertOrUpdate")]
	sealed class InsertOrUpdateBuilder : MethodCallBuilder
	{
		#region InsertOrUpdateBuilder

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var tableArg = methodCall.Arguments[0];
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, tableArg));

			var insertOrUpdateStatement = new SqlInsertOrUpdateStatement(sequence.SelectQuery);

			var insertExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
			List<UpdateBuilder.SetExpressionEnvelope>? updateExpressions = null;

			var contextRef       = new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], sequence);
			var insertSetterExpr = SequenceHelper.PrepareBody(methodCall.Arguments[1].UnwrapLambda(), sequence);

			UpdateBuilder.ParseSetter(builder, contextRef, contextRef, insertSetterExpr, insertExpressions);

			var updateExpr = methodCall.Arguments[2].Unwrap();
			if (!updateExpr.IsNullValue())
			{
				updateExpressions = new List<UpdateBuilder.SetExpressionEnvelope>();
				var updateSetterExpr = SequenceHelper.PrepareBody(updateExpr.UnwrapLambda(), sequence);

				UpdateBuilder.ParseSetter(builder, contextRef, contextRef, updateSetterExpr, updateExpressions);
			}

			var tableContext = SequenceHelper.GetTableContext(sequence);
			if (tableContext == null)
				throw new LinqToDBException("Could not retrieve table information from query.");

			UpdateBuilder.InitializeSetExpressions(builder, tableContext, sequence,
				insertExpressions, insertOrUpdateStatement.Insert.Items, createColumns : true);

			if (updateExpressions != null)
			{
				UpdateBuilder.InitializeSetExpressions(builder, tableContext, sequence,
					updateExpressions, insertOrUpdateStatement.Update.Items, createColumns : true);
			}

			insertOrUpdateStatement.Insert.Into  = tableContext.SqlTable;
			insertOrUpdateStatement.Update.Table = tableContext.SqlTable;
			/*
			insertOrUpdateStatement.SelectQuery.From.Tables.Clear();
			insertOrUpdateStatement.SelectQuery.From.Table(insertOrUpdateStatement.Update.Table);
			*/

			if (methodCall.Arguments.Count == 3)
			{
				var table = insertOrUpdateStatement.Insert.Into;
				var keys  = table.GetKeys(false);

				if (!(keys?.Count > 0))
					throw new LinqToDBException($"InsertOrUpdate method requires the '{table.NameForLogging}' table to have a primary key.");

				var q =
				(
					from k in keys
					join i in insertOrUpdateStatement.Insert.Items on k equals i.Column
					select new { k, i }
				).ToList();

				var missedKey = keys.Except(q.Select(i => i.k)).FirstOrDefault();

				if (missedKey != null)
					throw new LinqToDBException($"InsertOrUpdate method requires the '{table.NameForLogging}.{((SqlField)missedKey).Name}' field to be included in the insert setter.");

				insertOrUpdateStatement.Update.Keys.AddRange(q.Select(i => i.i));
			}
			else
			{
				var keysExpressions  = new List<UpdateBuilder.SetExpressionEnvelope>();

				var keysExpr = SequenceHelper.PrepareBody(methodCall.Arguments[3].UnwrapLambda(), sequence);

				UpdateBuilder.ParseSetter(builder, contextRef, contextRef, keysExpr, keysExpressions);

				UpdateBuilder.InitializeSetExpressions(builder, tableContext, sequence,
					keysExpressions, insertOrUpdateStatement.Update.Keys, false);
			}

			return BuildSequenceResult.FromContext(new InsertOrUpdateContext(sequence.TranslationModifier, builder, sequence, insertOrUpdateStatement));
		}

		#endregion

		#region UpdateContext

		sealed class InsertOrUpdateContext : BuildContextBase
		{
			public override MappingSchema MappingSchema => Context.MappingSchema;

			public IBuildContext Context { get; }

			public SqlInsertOrUpdateStatement InsertOrUpdateStatement { get; }

			public InsertOrUpdateContext(TranslationModifier translationModifier, ExpressionBuilder buider, IBuildContext sequence,
				SqlInsertOrUpdateStatement                   insertOrUpdateStatement) : base(translationModifier, buider, typeof(object), sequence.SelectQuery)
			{
				Context                 = sequence;
				InsertOrUpdateStatement = insertOrUpdateStatement;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this))
					return Expression.Default(path.Type);
				throw new InvalidOperationException();
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				if (Builder.DataContext.SqlProviderFlags.IsInsertOrUpdateSupported)
					QueryRunner.SetNonQueryQuery(query);
				else
					QueryRunner.MakeAlternativeInsertOrUpdate(Builder.DataContext.MappingSchema, query);
			}

			public override SqlStatement GetResultStatement()
			{
				return InsertOrUpdateStatement;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new InsertOrUpdateContext(TranslationModifier, Builder, context.CloneContext(Context), context.CloneElement(InsertOrUpdateStatement));
			}
		}

		#endregion
	}
}
