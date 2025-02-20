using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(
		nameof(LinqExtensions.TableName),
		nameof(LinqExtensions.ServerName),
		nameof(LinqExtensions.DatabaseName),
		nameof(LinqExtensions.SchemaName),
		nameof(TableExtensions.IsTemporary),
		nameof(TableExtensions.TableOptions),
		nameof(LinqExtensions.TableID)
	)]
	sealed class TableAttributeBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var table    = SequenceHelper.GetTableContext(sequence) ?? throw new LinqToDBException($"Cannot get table context from {sequence.GetType()}");
			var value = methodCall.Arguments.Count == 1 && methodCall.Method.Name == nameof(TableExtensions.IsTemporary)
				? true
				: builder.EvaluateExpression(methodCall.Arguments[1]);

			switch (methodCall.Method.Name)
			{
				case nameof(LinqExtensions.TableName)     : table.SqlTable.TableName = table.SqlTable.TableName with { Name     = (string)value! }; break;
				case nameof(LinqExtensions.ServerName)    : table.SqlTable.TableName = table.SqlTable.TableName with { Server   = (string?)value }; break;
				case nameof(LinqExtensions.DatabaseName)  : table.SqlTable.TableName = table.SqlTable.TableName with { Database = (string?)value }; break;
				case nameof(LinqExtensions.SchemaName)    : table.SqlTable.TableName = table.SqlTable.TableName with { Schema   = (string?)value }; break;
				case nameof(TableExtensions.TableOptions) : table.SqlTable.TableOptions  = (TableOptions)value!; break;
				case nameof(LinqExtensions.TableID)       : table.SqlTable.ID            = (string?)     value;  break;
				case nameof(TableExtensions.IsTemporary)  : table.SqlTable.Set((bool)value!, TableOptions.IsTemporary); break;
			}

			return BuildSequenceResult.FromContext(sequence);
		}
	}
}
