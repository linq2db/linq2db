using System;
using System.Collections.Generic;

using LinqToDB.SqlQuery;

namespace LinqToDB.Remote;

interface IQueryExtendible
{
	List<SqlQueryExtension>? SqlQueryExtensions { get; set; }
}
