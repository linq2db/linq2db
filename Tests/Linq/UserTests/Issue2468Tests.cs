using NUnit.Framework;
using LinqToDB.Data;
using System;
using LinqToDB.Mapping;
using LinqToDB;
using System.Linq;
using LinqToDB.Linq;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using System.Reflection;
using Org.BouncyCastle.Asn1.X509.Qualified;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2468Tests : TestBase
	{
		public enum MyEnum
		{
			Blue = 0,
			Red = 10,
			Green = 20,
			Yellow = 40,

		}

		[Table]
		public class InventoryResourceDTO
		{
			[Column]
			public Guid Id { get; set; }

			[Column]
			public MyEnum Status { get; set; }
		}

		static class MyExtensions
		{
			[ExpressionMethod(nameof(MyEnumToStringImpl))]
			public static string MyEnumToString(MyEnum v)
			{
				return v.ToString();
			}

			static Expression<Func<MyEnum, string>> MyEnumToStringImpl()
			   => v => v == MyEnum.Blue ? "Blue" : v == MyEnum.Red ? "Red" : v == MyEnum.Green ? "Green" : "Yellow";
		}

		public static void MapEnumToString<T>() where T : struct
		{
			var type=typeof(T);
			MapEnumToString(type);
		}
		public static void MapEnumToString(Type type)
		{			
			var underlyingType=Enum.GetUnderlyingType(type);

			var par = Expression.Parameter(typeof(object), "enum");
			var casted = Expression.Convert(par, underlyingType);
			var values = Enum.GetValues(type);
			Expression retE = Expression.Constant(null, typeof(string));
			foreach (var v in values)
			{
				var eq = Expression.Equal(Expression.Constant(Convert.ChangeType(v,underlyingType)), casted);
				retE = Expression.Condition(eq, Expression.Constant(v.ToString()), retE);
			}
			var lambda = Expression.Lambda<Func<object,string>>(retE, par);

			Expressions.MapMember((MyEnum v) => v.ToString(), lambda);
		}


		[Test]
		public void Issue2468Test(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			//MapEnumToString<MyEnum>();

			Expressions.MapMember((MyEnum v) => v.ToString(), v => MyExtensions.MyEnumToString(v));

			using (var db = (DataConnection)GetDataContext(context))
			{
				using (var itb = db.CreateLocalTable<InventoryResourceDTO>())
				{
					var dto1 = new InventoryResourceDTO
					{
						Status = MyEnum.Blue,
						Id = Guid.NewGuid()
					};
					db.Insert(dto1);

					var lst = db.GetTable<InventoryResourceDTO>().Where(x=>x.Status.ToString().Contains("e")).ToList();
				}
			}
		}
	}
}
