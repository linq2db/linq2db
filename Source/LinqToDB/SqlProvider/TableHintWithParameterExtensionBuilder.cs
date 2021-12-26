using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	class TableHintWithParameterExtensionBuilder : ISqlExtensionBuilder
	{
		public void Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var hint  = ((SqlValue)sqlQueryExtension.Arguments["tableHint"]).    Value;
			var param = ((SqlValue)sqlQueryExtension.Arguments["hintParameter"]).Value;

			stringBuilder
				.Append(hint)
				.Append('(')
				.Append(param)
				.Append(')');
		}
	}
}
