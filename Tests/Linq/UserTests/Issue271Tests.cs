using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue271Tests : TestBase
	{
		public class Entity
		{
			[Column(DataType = DataType.Char)]     public string? CharValue;
			[Column(DataType = DataType.VarChar)]  public string? VarCharValue;
			[Column(DataType = DataType.NChar)]    public string? NCharValue;
			[Column(DataType = DataType.NVarChar)] public string? NVarCharValue;
		}

		[Test]
		public void Test1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from e in db.GetTable<Entity>()
					where
						e.CharValue     == "CharValue"     &&
						e.VarCharValue  == "VarCharValue"  &&
						e.NCharValue    == "NCharValue"    &&
						e.NVarCharValue == "NVarCharValue"
					select e;

				var str = q.ToString()!;

				TestContext.Out.WriteLine(str);

				Assert.That(str, Does.Not.Contain("N'CharValue'"));
				Assert.That(str, Does.Not.Contain("N'VarCharValue'"));

				Assert.That(str, Does.Contain("N'NCharValue'"));
				Assert.That(str, Does.Contain("N'NVarCharValue'"));
			}

		}

		[Test]
		public void Test2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var @char     = new[] { "CharValue"     };
				var @varChar  = new[] { "VarCharValue"  };
				var @nChar    = new[] { "NCharValue"    };
				var @nVarChar = new[] { "NVarCharValue" };

				var q =
					from e in db.GetTable<Entity>()
					where
						@char    .Contains(e.CharValue    ) &&
						@varChar .Contains(e.VarCharValue ) &&
						@nChar   .Contains(e.NCharValue   ) &&
						@nVarChar.Contains(e.NVarCharValue)
					select e;

				var str = q.ToString()!;

				TestContext.Out.WriteLine(str);

				Assert.That(str, Does.Not.Contain("N'CharValue'"));
				Assert.That(str, Does.Not.Contain("N'VarCharValue'"));

				Assert.That(str, Does.Contain("N'NCharValue'"));
				Assert.That(str, Does.Contain("N'NVarCharValue'"));
			}
		}
	}
}
