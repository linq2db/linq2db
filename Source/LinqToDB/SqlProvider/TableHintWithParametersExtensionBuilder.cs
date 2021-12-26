using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	class TableHintWithParametersExtensionBuilder : ISqlExtensionBuilder
	{
		public void Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var hint  = ((SqlValue)     sqlQueryExtension.Arguments["tableHint"]).           Value;
			var count = (int)((SqlValue)sqlQueryExtension.Arguments["hintParameters.Count"]).Value!;

			stringBuilder.Append(hint);

			if (count > 0)
			{
				stringBuilder.Append('(');

				for (var i = 0; i < count; i++)
				{
					var value = ((SqlValue)sqlQueryExtension.Arguments[$"hintParameters.{i}"]).Value;
					stringBuilder
						.Append(value)
						.Append(", ");
				}

				stringBuilder.Length -= 2;
				stringBuilder.Append(')');
			}
		}
	}
}
