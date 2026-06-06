#if !NETFRAMEWORK
using LinqToDB.EntityFrameworkCore.Tests.Models.ManyToMany;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer.Models.ManyToMany
{
	public class ManyToManyContext(DbContextOptions options) : ManyToManyContextBase(options)
	{
	}
}
#endif
