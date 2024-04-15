using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4480Tests : TestBase
	{
		public readonly record struct TableId
		{
			public int Value { get; }

			public TableId(int value) => Value = value;

			public static explicit operator TableId(int value) => new(value);
			public static explicit operator int(TableId value) => value.Value;

			private static TableId Deserialize(int value) => new(value);

			public class LinqToDbValueConverter : global::LinqToDB.Common.ValueConverter<TableId, int>
			{
				public LinqToDbValueConverter()
					: base(
						  v => v.Value,
						  p => Deserialize(p),
						  handlesNulls: false)
				{ }
			}
		}

		[Table]
		public class Table
		{
			[Column(DataType = DataType.Int32, CanBeNull = true)]
			[ValueConverter(ConverterType = typeof(TableId.LinqToDbValueConverter))]
			public TableId? Id { get; set; }
		}

		[Test]
		public void Test_Update([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Table>())
			{
				db.GetTable<Table>()
					.Update(
						x => new() { Id = (TableId)3, }
					);
			}
		}
	}
}
