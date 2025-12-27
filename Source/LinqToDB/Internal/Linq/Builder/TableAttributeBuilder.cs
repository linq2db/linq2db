using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(
		nameof(LinqExtensions.TableName),
		nameof(LinqExtensions.ServerName),
		nameof(LinqExtensions.DatabaseName),
		nameof(LinqExtensions.SchemaName),
		nameof(TableExtensions.IsTemporary),
		nameof(TableExtensions.TableOptions),
		nameof(LinqInternalExtensions.UseTableDescriptor),
		nameof(LinqExtensions.TableID)
	)]
	sealed class TableAttributeBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var table    = SequenceHelper.GetTableContext(sequence) ?? throw new LinqToDBException($"Cannot get table context from {sequence.GetType()}");
			var value = methodCall.Arguments.Count == 1 && string.Equals(methodCall.Method.Name, nameof(TableExtensions.IsTemporary), System.StringComparison.Ordinal) ? true
				: builder.EvaluateExpression(methodCall.Arguments[1]);

			switch (methodCall.Method.Name)
			{
				case nameof(LinqExtensions.TableName)                 : table.SqlTable.TableName    = table.SqlTable.TableName with { Name     = (string)value! }; break;
				case nameof(LinqExtensions.ServerName)                : table.SqlTable.TableName    = table.SqlTable.TableName with { Server   = (string?)value }; break;
				case nameof(LinqExtensions.DatabaseName)              : table.SqlTable.TableName    = table.SqlTable.TableName with { Database = (string?)value }; break;
				case nameof(LinqExtensions.SchemaName)                : table.SqlTable.TableName    = table.SqlTable.TableName with { Schema   = (string?)value }; break;
				case nameof(TableExtensions.TableOptions)             : table.SqlTable.TableOptions = (TableOptions)value!; break;
				case nameof(LinqExtensions.TableID)                   : table.SqlTable.ID           = (string?)     value;  break;
				case nameof(TableExtensions.IsTemporary)              : table.SqlTable.Set((bool)value!, TableOptions.IsTemporary); break;
				case nameof(LinqInternalExtensions.UseTableDescriptor): MergeTableWithDescriptor(table.SqlTable, (EntityDescriptor)value!); break;
			}

			return BuildSequenceResult.FromContext(sequence);
		}

		static void MergeTableWithDescriptor(SqlTable table, EntityDescriptor entityDescriptor)
		{
			table.TableName    = entityDescriptor.Name;
			table.TableOptions = entityDescriptor.TableOptions;

			foreach (var column in entityDescriptor.Columns)
			{
				var newField   = new SqlField(column);

				var foundField = table.FindFieldByMemberName(column.MemberName);
				if (foundField != null)
				{
					foundField.Assign(newField);
				}
				else
				{
					table.Add(newField);

					if (newField.Type.DataType == DataType.Undefined)
					{
						newField.Type = SqlTable.SuggestType(newField.Type, entityDescriptor.MappingSchema, out var canBeNull);
						if (canBeNull is not null)
							newField.CanBeNull = canBeNull.Value;
					}
				}
			}

			table.ResetKeys();
		}
	}
}
