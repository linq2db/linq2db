using LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Pomelo.Models.IssueModel
{
	public class IssueContext(DbContextOptions options) : IssueContextBase(options)
	{
	}
}
