using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore
{
	public partial class LinqToDBForEFTools
	{
		static void InitializeMapping()
		{
			Linq.Expressions.MapMember((DbFunctions f, string m, string p) => f.Like(m, p), (f, m, p) => Sql.Like(m, p));
		}
	}
}
