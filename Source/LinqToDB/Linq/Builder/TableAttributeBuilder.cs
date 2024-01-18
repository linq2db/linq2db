using System.Linq.Expressions;

using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	sealed class TableAttributeBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames =
		[
			nameof(LinqExtensions.TableName),
			nameof(LinqExtensions.ServerName),
			nameof(LinqExtensions.DatabaseName),
			nameof(LinqExtensions.SchemaName),
			nameof(TableExtensions.IsTemporary),
			nameof(TableExtensions.TableOptions),
			nameof(LinqExtensions.TableID)
		];

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var table    = SequenceHelper.GetTableContext(sequence) ?? throw new LinqToDBException($"Cannot get table context from {sequence.GetType()}");
			var value    = methodCall.Arguments.Count == 1 && methodCall.Method.Name == nameof(TableExtensions.IsTemporary) ?
				true :
				methodCall.Arguments[1].EvaluateExpression(builder.DataContext);

			switch (methodCall.Method.Name)
			{
				case nameof(LinqExtensions.TableName)     : table.SqlTable.TableName = new SqlObjectName(table.SqlTable.TableName) { Name     = (string)value! }; break;
				case nameof(LinqExtensions.ServerName)    : table.SqlTable.TableName = new SqlObjectName(table.SqlTable.TableName) { Server   = (string?)value }; break;
				case nameof(LinqExtensions.DatabaseName)  : table.SqlTable.TableName = new SqlObjectName(table.SqlTable.TableName) { Database = (string?)value }; break;
				case nameof(LinqExtensions.SchemaName)    : table.SqlTable.TableName = new SqlObjectName(table.SqlTable.TableName) { Schema   = (string?)value }; break;
				case nameof(TableExtensions.TableOptions) : table.SqlTable.TableOptions  = (TableOptions)value!; break;
				case nameof(TableExtensions.IsTemporary)  : table.SqlTable.Set((bool)value!, TableOptions.IsTemporary); break;
				case nameof(LinqExtensions.TableID)       : table.SqlTable.ID            = (string?)     value;  break;
			}

			return sequence;
		}
	}
}
