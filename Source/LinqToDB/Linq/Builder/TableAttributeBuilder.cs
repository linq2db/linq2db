using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	class TableAttributeBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames = new []
		{
			nameof(LinqExtensions.TableName),
			nameof(LinqExtensions.ServerName),
			nameof(LinqExtensions.DatabaseName),
			nameof(LinqExtensions.SchemaName),
			nameof(TableExtensions.IsTemporary),
			nameof(TableExtensions.TableOptions)
		};

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var table    = (TableBuilder.TableContext)sequence;
			var value    = methodCall.Arguments.Count == 1 && methodCall.Method.Name == nameof(TableExtensions.IsTemporary) ?
				true :
				methodCall.Arguments[1].EvaluateExpression();

			switch (methodCall.Method.Name)
			{
				case nameof(LinqExtensions.TableName)     : table.SqlTable.PhysicalName  = (string?)     value!; break;
				case nameof(LinqExtensions.ServerName)    : table.SqlTable.Server        = (string?)     value;  break;
				case nameof(LinqExtensions.DatabaseName)  : table.SqlTable.Database      = (string?)     value;  break;
				case nameof(LinqExtensions.SchemaName)    : table.SqlTable.Schema        = (string?)     value;  break;
				case nameof(TableExtensions.TableOptions) : table.SqlTable.TableOptions  = (TableOptions)value!; break;
				case nameof(TableExtensions.IsTemporary)  : table.SqlTable.Set((bool)value!, TableOptions.IsTemporary); break;
			}

			return sequence;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}
	}
}
