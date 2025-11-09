using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue192Tests : TestBase
	{
		[Table(Name = "TypeConvertTable")]
		public class TypeConvertTable
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(Length = 50), NotNull]
			public string Name   { get; set; } = null!;

			[Column(DataType = DataType.Char)]
			public bool BoolValue { get; set; }

			[Column(DataType = DataType.VarChar, Length = 50)]
			public Guid? GuidValue { get; set; }

			public override string ToString()
			{
				return string.Format("{0} {1} {2}", Name, BoolValue, GuidValue);
			}

			public override bool Equals(object? obj)
			{
				if (obj is not TypeConvertTable e)
					return false;

				return
					Name      == e.Name &&
					BoolValue == e.BoolValue &&
					GuidValue == e.GuidValue;
			}

			public override int GetHashCode()
			{
				return 0;
			}

		}

		[Table(Name = "TypeConvertTable")]
		public class TypeConvertTableRaw
		{
			[Column(Length = 50), NotNull]
			public string Name   { get; set; } = null!;

			[Column(DataType = DataType.Char), NotNull]
			public char BoolValue { get; set; }

			[Column(DataType = DataType.VarChar, Length = 50), Nullable]
			public string? GuidValue { get; set; }

		}

		[Test]
		public void Test([DataSources] string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<bool,   DataParameter>(_ => DataParameter.Char(null, _ ? 'Y' : 'N'));
			ms.SetConvertExpression<char,   bool>(_ => _ == 'Y');
			if (context.IsAnyOf(TestProvName.AllClickHouse))
				ms.SetConvertExpression<string, bool>(_ => _.Trim('\0') == "Y");
			else
				ms.SetConvertExpression<string, bool>(_ => _.Trim() == "Y");

			ms.SetConvertExpression<Guid?,  DataParameter>(_ => DataParameter.VarChar(null, _.ToString()));
			ms.SetConvertExpression<string, Guid?>(_ => Guid.Parse(_));

			using var db = GetDataContext(context, ms);
			using var tbl = db.CreateLocalTable<TypeConvertTable>();
				var notVerified = new TypeConvertTable
				{
					Id        = 1,
					Name      = "NotVerified",
					BoolValue = false,
					GuidValue = TestData.Guid1
				};

				var verified = new TypeConvertTable
				{
					Id        = 2,
					Name      = "Verified",
					BoolValue = true,
					GuidValue = TestData.Guid2
				};

				db.Insert(notVerified, tbl.TableName);
			db.Insert(verified, tbl.TableName);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.GetTable<TypeConvertTableRaw>().TableName(tbl.TableName).Count(_ => _.BoolValue == 'N'), Is.EqualTo(1));
					Assert.That(db.GetTable<TypeConvertTableRaw>().TableName(tbl.TableName).Count(_ => _.BoolValue == 'Y'), Is.EqualTo(1));
					Assert.That(db.GetTable<TypeConvertTableRaw>().TableName(tbl.TableName).Count(_ => _.GuidValue == verified.GuidValue.ToString()), Is.EqualTo(1));

					Assert.That(tbl.First(_ => _.BoolValue == false), Is.EqualTo(notVerified));
					Assert.That(tbl.First(_ => _.BoolValue == true), Is.EqualTo(verified));

					Assert.That(tbl.First(_ => _.BoolValue != true), Is.EqualTo(notVerified));
					Assert.That(tbl.First(_ => _.BoolValue != false), Is.EqualTo(verified));

					Assert.That(tbl.First(_ => !_.BoolValue), Is.EqualTo(notVerified));
					Assert.That(tbl.First(_ => _.BoolValue), Is.EqualTo(verified));

					Assert.That(tbl.First(_ => _.BoolValue.Equals(false)), Is.EqualTo(notVerified));
					Assert.That(tbl.First(_ => _.BoolValue.Equals(true)), Is.EqualTo(verified));

					Assert.That(tbl.First(_ => !_.BoolValue.Equals(true)), Is.EqualTo(notVerified));
					Assert.That(tbl.First(_ => !_.BoolValue.Equals(false)), Is.EqualTo(verified));

					Assert.That(tbl.First(_ => _.GuidValue == notVerified.GuidValue), Is.EqualTo(notVerified));
					Assert.That(tbl.First(_ => _.GuidValue == verified.GuidValue), Is.EqualTo(verified));
				}
			}
		}
	}
