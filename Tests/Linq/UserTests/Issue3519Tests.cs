using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3519Tests : TestBase
	{
		[Test]
		public void ConverterUseTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			// Throws an exception in Microsoft.Data.SqlClient
			// EnumValueObject parameter is passed through
			// instead of being converted to byte using the
			// specified ValueConverter on the column
			db.GetTable<Entity>().Merge()
				.Using(new[]
				{
					new { Value = new EnumValueObject(EnumValue.None), Name = "None", },
					new { Value = new EnumValueObject(EnumValue.Dummy1), Name = "Dummy1", },
					new { Value = new EnumValueObject(EnumValue.Dummy2), Name = "Dummy2", },
				})
				.On((dst, src) => dst.Id == src.Value)
				.InsertWhenNotMatched(src => new Entity { Id = src.Value, })
				.DeleteWhenNotMatchedBySource()
				.Merge();
		}

		enum EnumValue : byte { None, Dummy1, Dummy2, }
		record struct EnumValueObject(EnumValue EnumValue)
		{
			public class LinqToDbValueConverter : LinqToDB.Common.ValueConverter<EnumValueObject, byte>
			{
				public LinqToDbValueConverter()
					: base(
						  v => (byte)v.EnumValue,
						  p => new((EnumValue)p),
						  handlesNulls: false)
				{ }
			}
		}

		class Entity
		{
			[PrimaryKey]
			[Column(DataType = DataType.Byte)]
			[ValueConverter(ConverterType = typeof(EnumValueObject.LinqToDbValueConverter))]
			public EnumValueObject Id { get; set; }
		}
	}
}
