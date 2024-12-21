using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2468Tests : TestBase
	{
		public enum StatusEnum
		{
			Unknown = 0,
			Open = 1,
			InProgress = 2,
			Done = 3,
		}

		public enum ColorEnum
		{
			Blue = 0,
			Red = 10,
			Green = 20,
		}

		public enum CMYKEnum
		{
			Cyan = 0,
			Magenta = 10,
			Yellow = 20,
			Black = 40,
		}

		[Table]
		public class InventoryResourceDTO
		{
			[Column]
			public Guid Id { get; set; }

			[Column]
			public StatusEnum Status { get; set; }

			[Column]
			public ColorEnum Color { get; set; }

			[Column]
			public CMYKEnum CMYKColor { get; set; }
		}

		private static class MyExtensions
		{
			[ExpressionMethod(nameof(StatusEnumToStringImpl))]
			public static string StatusEnumToString(StatusEnum v)
			{
				return v.ToString();
			}

			private static Expression<Func<StatusEnum, string>> StatusEnumToStringImpl()
			{
				return v => v == StatusEnum.Done ? "Done" :
					v == StatusEnum.Open ? "Open" :
					v == StatusEnum.InProgress ? "InProgress" : "Unknown";
			}
		}

		private static void MapEnumToString<T>() where T : struct
		{
			MapEnumToString(typeof(T));
		}

		private static void MapEnumToString(Type type)
		{
			var param  = Expression.Parameter(type, "enum");
			var values = Enum.GetValues(type);
			var list   = (System.Collections.IList)values;
			Expression retE = Expression.Constant(null, typeof(string));

			for (var i = values.Length - 1; i >= 0; i--)
			{
				var v  = list[i]!;
				var eq = Expression.Equal(Expression.Constant(v, type), param);
				retE   = Expression.Condition(eq, Expression.Constant(v.ToString()), retE);
			}

			var lambda = Expression.Lambda(retE, param);

			var toStringMethod = type.GetMethods().First(m => m.Name == "ToString" && m.GetParameters().Length == 0);

			Expressions.MapMember(type, toStringMethod, lambda);
		}

		static Issue2468Tests()
		{
			MapEnumToString<ColorEnum>();
			MapEnumToString<CMYKEnum>();
			Expressions.MapMember((StatusEnum v) => v.ToString(), v => MyExtensions.StatusEnumToString(v));
		}

		[Test]
		public void Issue2468Test(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)]
			string context)
		{

			using (var db = (DataConnection)GetDataContext(context))
			using (var itb = db.CreateLocalTable<InventoryResourceDTO>())
			{
				var dto1 = new InventoryResourceDTO
				{
					Color = ColorEnum.Blue, Id = TestData.Guid1, CMYKColor = CMYKEnum.Cyan, Status = StatusEnum.Open
				};

				db.Insert(dto1);

				var list = itb
					.Where(x =>
						x.Color.ToString().Contains("Bl")
						&& x.CMYKColor.ToString().Contains("Cya")
						&& x.Status.ToString().Contains("en")
					)
					.ToList();

				Assert.That(list, Has.Count.EqualTo(1));
			}
		}
	}
}
