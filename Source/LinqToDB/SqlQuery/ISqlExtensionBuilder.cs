using System;
using System.Text;

using LinqToDB.SqlProvider;

namespace LinqToDB.SqlQuery
{
	public interface ISqlExtensionBuilder
	{
		void Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension);
	}
}
