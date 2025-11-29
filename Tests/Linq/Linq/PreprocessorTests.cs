using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Internal.Linq;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class PreprocessorTests : TestBase
	{
		sealed class PostProcessorDataConnection : DataConnection, IExpressionPreprocessor
		{
			public PostProcessorDataConnection(string configurationString) : base(configurationString)
			{
			}

			public Expression ProcessExpression(Expression expression)
			{
				var result = expression.Transform<object?>(null, static (_, e) =>
				{
					if (e.NodeType == ExpressionType.Constant)
					{
						var constant = (ConstantExpression) e;
						if (constant.Value is int)
						{
							return Expression.Constant((int) constant.Value + 1);
						}
					}

					return e;
				});

				return result;
			}
		}

		[Test]
		public void Test([DataSources(false)] string context)
		{
			using var db = new PostProcessorDataConnection(context);
			for (int i = 0; i < 3; i++)
			{
				var newId = db.GetTable<Parent>().Where(p => p.ParentID == 1).Select(p => p.ParentID).First();
				Assert.That(newId, Is.EqualTo(2));
			}
		}
	}
}
