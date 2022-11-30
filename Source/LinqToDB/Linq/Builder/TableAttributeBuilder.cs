using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	sealed class TableAttributeBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames = 
		{
			nameof(LinqExtensions.TableName),
			nameof(LinqExtensions.ServerName),
			nameof(LinqExtensions.DatabaseName),
			nameof(LinqExtensions.SchemaName),
			nameof(TableExtensions.IsTemporary),
			nameof(TableExtensions.TableOptions),
			nameof(LinqExtensions.TableID),
		};

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
				methodCall.Arguments[1].EvaluateExpression();

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

			return sequence;
		}
	}
}
