﻿using System;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using SqlProvider;

	public interface ISqlExtensionBuilder
	{
	}

	public interface ISqlQueryExtensionBuilder : ISqlExtensionBuilder
	{
		void Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension);
	}

	public interface ISqlTableExtensionBuilder : ISqlExtensionBuilder
	{
		void Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension, SqlTable table, string alias);
	}
}
