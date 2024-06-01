using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Collections.Generic;
using System;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3475Tests : TestBase
	{
		internal sealed class LinqToDBDynamicLinqCustomTypeProvider() : DefaultDynamicLinqCustomTypeProvider(ParsingConfig.Default)
		{
			public override HashSet<Type> GetCustomTypes()
			{
				var types = base.GetCustomTypes();
				types.Add(typeof(Sql));
				return types;
			}
		}

		[Table]
		public class NumberLikeTestTable
		{
			[Column] public int?      IntNProp { get; set; }
		}

		public class NumberLikeTestObj
		{
			public NumberLikeTestTable? Obj { get; set; }
		}

		[Test]
		public void NumberLikeTests([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			ParsingConfig.Default.CustomTypeProvider = new LinqToDBDynamicLinqCustomTypeProvider();

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<NumberLikeTestTable>())
			{
				var query1 = db.GetTable<NumberLikeTestTable>().Select(x => new NumberLikeTestObj() { Obj = x }).Where(x => Sql.Like(x!.Obj!.IntNProp!.ToString(), "1%")).Take(50);
				var query2 = db.GetTable<NumberLikeTestTable>().Select(x => new NumberLikeTestObj() { Obj = x }).Where("((Sql.Like(it.Obj.IntNProp.ToString(),@0)))", new object[]{ "1%" }).Take(50);
				
				var res1 = query1.ToList();
				var res2 = query2.ToList();
			}
		}
	}
}
