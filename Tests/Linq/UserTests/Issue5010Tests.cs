using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5010Tests : TestBase
	{
		[Table]
		public class LargeData
		{
			[Column] public int Id { get; set; }
			[Column(DataType = DataType.Binary)] public byte[]? Bytes1 { get; set; }
			[Column(DataType = DataType.VarBinary)] public byte[]? Bytes2 { get; set; }
			[Column(DataType = DataType.Blob)] public byte[]? Bytes3 { get; set; }
		}

		[Test]
		public void BulkCopyError([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<LargeData>())
			{
				var obj = new List<LargeData> () {
					new LargeData()
					{
						Id = 1,
						Bytes1 = Enumerable.Repeat<byte>(55, 100000).ToArray(),
						Bytes2 = Enumerable.Repeat<byte>(55, 100000).ToArray(),
						Bytes3 = Enumerable.Repeat<byte>(55, 100000).ToArray()
					}
				};

				var res = ((DataConnection)db).BulkCopy(
					new BulkCopyOptions()
					{
						BulkCopyType = BulkCopyType.ProviderSpecific,
						TableName = "LargeData"
					},
					obj
					);
			}
		}
	}
}
