using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue421 : TestBase
	{
		public class BlobClass
		{
			[Column, PrimaryKey]
			public int Id;

			[Column(Length = 100)]
			[Column(DataType = DataType.Blob, Configuration = ProviderName.DB2)]
			[Column(DataType = DataType.Blob, Configuration = ProviderName.Firebird)]
			[Column(DataType = DataType.Blob, Configuration = ProviderName.Oracle)]
			[Column(DataType = DataType.Blob, Configuration = ProviderName.OracleManaged)]
			[Column(DataType = DataType.Blob, Configuration = ProviderName.OracleNative)]
			[Column(DataType = DataType.Blob, Configuration = ProviderName.PostgreSQL, DbType = "bytea")]
			[Column(                          Configuration = ProviderName.Informix,   DbType = "byte")]
			public byte[] BlobValue;
		}

		[Test, DataContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			using (new LocalTable<BlobClass>(db))
			{

				db.Into(db.GetTable<BlobClass>())
					.Value(p => p.Id,        1)
					.Value(p => p.BlobValue, new byte[] { 1, 2, 3 })
					.Insert();

				var v = db.GetTable<BlobClass>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 1, 2, 3 }, v.BlobValue);
			}
		}


		[Test, DataContextSource]
		public void Test2(string context)
		{
			using (var db = GetDataContext(context))
			using (new LocalTable<BlobClass>(db))
			{
				db.InlineParameters = true;

				db.Into(db.GetTable<BlobClass>())
					.Value(p => p.Id,        1)
					.Value(p => p.BlobValue, new byte[] { 1, 2, 3 })
					.Insert();

				var v = db.GetTable<BlobClass>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 1, 2, 3 }, v.BlobValue);
			}
		}
	}
}
