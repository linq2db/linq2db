using NUnit.Framework;
using LinqToDB;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LinqToDB.Expressions;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2364Tests : TestBase
	{
		[Test]
		public void Issue2364Test1()
		{
			var taskExpression = Expression.Constant(new ValueTask<long>());
			var mapper = new ValueTaskToTaskMapper();
			var result = ((ICustomMapper)mapper).Map(taskExpression);
			Assert.AreEqual(typeof(Task<long>), result.Type);
		}

		[Test]
		public void Issue2364Test2()
		{
			var taskExpression = Expression.Constant(new ValueTask());
			var mapper = new ValueTaskToTaskMapper();
			var result = ((ICustomMapper)mapper).Map(taskExpression);
			Assert.AreEqual(typeof(Task), result.Type);
		}

		[Test]
		public void Issue2364Test3()
		{
			var taskExpression = Expression.Constant(0);
			var mapper = new ValueTaskToTaskMapper();
			Assert.Throws(typeof(LinqToDBException), () => ((ICustomMapper)mapper).Map(taskExpression));
		}
	}
}
