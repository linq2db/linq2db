using System.Linq;

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

				db.GetTable<BlobClass>()
					.Where(_ => _.Id == 1)
					.Set(_ => _.BlobValue, new byte[] {3, 2, 1})
					.Update();

				v = db.GetTable<BlobClass>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 3, 2, 1 }, v.BlobValue);
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

				db.GetTable<BlobClass>()
					.Where(_ => _.Id == 1)
					.Set(_ => _.BlobValue, new byte[] {3, 2, 1})
					.Update();

				v = db.GetTable<BlobClass>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 3, 2, 1 }, v.BlobValue);
			}
		}

		[Test, DataContextSource]
		public void Test3(string context)
		{
			using (var db = GetDataContext(context))
			using (new LocalTable<BlobClass>(db))
			{

				var e = new BlobClass() {Id = 1, BlobValue = new byte[] {1, 2, 3}};

				db.Insert(e);

				var v = db.GetTable<BlobClass>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 1, 2, 3 }, v.BlobValue);

				e.BlobValue = new byte[] {3, 2, 1};

				v = db.GetTable<BlobClass>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 3, 2, 1 }, v.BlobValue);
			}
		}


		[Test, DataContextSource]
		public void Test4(string context)
		{
			using (var db = GetDataContext(context))
			using (new LocalTable<BlobClass>(db))
			{
				db.InlineParameters = true;

				var e = new BlobClass() { Id = 1, BlobValue = new byte[] { 1, 2, 3 } };

				db.Insert(e);

				var v = db.GetTable<BlobClass>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 1, 2, 3 }, v.BlobValue);

				e.BlobValue = new byte[] { 3, 2, 1 };

				v = db.GetTable<BlobClass>().First(_ => _.Id == 1);

				AreEqual(new byte[] { 3, 2, 1 }, v.BlobValue);
			}
		}
	}
}
