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
			[Column(DataType = DataType.Char)]     public string CharValue;
			[Column(DataType = DataType.VarChar)]  public string VarCharValue;
			[Column(DataType = DataType.NChar)]    public string NCharValue;
			[Column(DataType = DataType.NVarChar)] public string NVarCharValue;
		}

		[Test]
		public void Test1([IncludeDataSources(
			ProviderName.SqlCe, ProviderName.SqlServer2014, ProviderName.SqlServer2012,
			ProviderName.SqlServer2008, ProviderName.SqlServer2005)]
			string context)
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

				var str = q.ToString();

				Console.WriteLine(str);

				Assert.False(str.Contains("N'CharValue'"));
				Assert.False(str.Contains("N'VarCharValue'"));

				Assert.True(str.Contains("N'NCharValue'"));
				Assert.True(str.Contains("N'NVarCharValue'"));
			}

		}

		[Test]
		public void Test2([IncludeDataSources(
			ProviderName.SqlCe, ProviderName.SqlServer2014, ProviderName.SqlServer2012,
			ProviderName.SqlServer2008, ProviderName.SqlServer2005)]
			string context)
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

				var str = q.ToString();

				Console.WriteLine(str);

				Assert.False(str.Contains("N'CharValue'"));
				Assert.False(str.Contains("N'VarCharValue'"));

				Assert.True(str.Contains("N'NCharValue'"));
				Assert.True(str.Contains("N'NVarCharValue'"));
			}
		}
	}
}
