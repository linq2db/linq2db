#if !NETFRAMEWORK
using LinqToDB.EntityFrameworkCore.Tests.Models.ManyToMany;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.SQLite.Models.ManyToMany
{
	public class ManyToManyContext(DbContextOptions options) : ManyToManyContextBase(options)
	{
	}
}
#endif
