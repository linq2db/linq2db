using System;
using System.Collections.Generic;

using LinqToDB.SqlQuery;

namespace LinqToDB.ServiceModel
{
	interface IQueryExtendible
	{
		List<SqlQueryExtension>? SqlQueryExtensions { get; set; }
	}
}
