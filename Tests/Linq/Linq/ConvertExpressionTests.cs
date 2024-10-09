using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class ConvertExpressionTests : TestBase
	{
		[Test]
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void Select1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let children = p.Children.Where(c => c.ParentID > 1)
					select children.Sum(c => c.ChildID),
					from p in db.Parent
					let children = p.Children.Where(c => c.ParentID > 1)
					select children.Sum(c => c.ChildID));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void Select2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					select children2.Sum(c => c.ChildID),
					from p in db.Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					select children2.Sum(c => c.ChildID));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void Select3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					Parent
						.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
						.Select(t => new { children2 = t.children1.Where(c => c.ParentID < 10) })
						.Select(t => t.children2.Sum(c => c.ChildID)),
					db.Parent
						.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
						.Select(t => new { children2 = t.children1.Where(c => c.ParentID < 10) })
						.Select(t => t.children2.Sum(c => c.ChildID)));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void Select4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					Parent
						.Select(p => p.Children. Where(c => c.ParentID > 1))
						.Select(t => t.Where(c => c.ParentID < 10))
						.Select(t => t.Sum(c => c.ChildID)),
					db.Parent
						.Select(p => p.Children. Where(c => c.ParentID > 1))
						.Select(t => t.Where(c => c.ParentID < 10))
						.Select(t => t.Sum(c => c.ChildID)));
		}

		[Test]
		public void Where1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					where children1.Any()
					select children2.Sum(c => c.ChildID),
					from p in db.Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					where children1.Any()
					select children2.Sum(c => c.ChildID));
		}

		[Test]
		public void Where2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					where children1.Any()
					let children2 = children1.Where(c => c.ParentID < 10)
					select children2.Sum(c => c.ChildID),
					from p in db.Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					where children1.Any()
					let children2 = children1.Where(c => c.ParentID < 10)
					select children2.Sum(c => c.ChildID));
		}

		[Test]
		public void Where3([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					where children2.Any()
					select children2.Sum(c => c.ChildID),
					from p in db.Parent
					let children1 = p.Children.Where(c => c.ParentID > 1)
					let children2 = children1.Where(c => c.ParentID < 10)
					where children2.Any()
					select children2.Sum(c => c.ChildID));
		}

		//[Test]
		//public void Where4([DataSources] string context)
		//{
		//	using (var db = GetDataContext(context))
		//		AreEqual(
		//			   Parent
		//				.Select(p => new { p, children1 = p.Children. Where(c => c.ParentID > 1)  })
		//				.Where (t => t.children1.Any()),
		//			db.Parent
		//				.Select(p => new { p, children1 = p.Children. Where(c => c.ParentID > 1)  })
		//				.Where (t => t.children1.Any()));
		//}

		//[Test]
		//public void Where5([DataSources] string context)
		//{
		//	using (var db = GetDataContext(context))
		//		AreEqual(
		//			   Parent
		//				.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
		//				.Where (t => t.children1.Any()),
		//			db.Parent
		//				.Select(p => new { children1 = p.Children. Where(c => c.ParentID > 1)  })
		//				.Where (t => t.children1.Any()));
		//}

		//[Test]
		//public void Where6([DataSources] string context)
		//{
		//	using (var db = GetDataContext(context))
		//		AreEqual(
		//			   Parent
		//				.Select(p => p.Children. Where(c => c.ParentID > 1))
		//				.Where (t => t.Any()),
		//			db.Parent
		//				.Select(p => p.Children. Where(c => c.ParentID > 1))
		//				.Where (t => t.Any()));
		//}

		[Test]
		public void Any1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Parent
						.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
						.Any(p => p.children1.Any()), Is.EqualTo(Parent
						.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
						.Any(p => p.children1.Any())));
		}

		[Test]
		public void Any2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Parent
						.Select(p => p.Children.Where(c => c.ParentID > 1))
						.Any(p => p.Any()), Is.EqualTo(Parent
						.Select(p => p.Children.Where(c => c.ParentID > 1))
						.Any(p => p.Any())));
		}

		[Test]
		public void Any3([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					db.Parent
						.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
						.Where(p => p.children1.Any())
						.Any(), Is.EqualTo(Parent
						.Select(p => new { p, children1 = p.Children.Where(c => c.ParentID > 1) })
						.Where(p => p.children1.Any())
						.Any()));
		}

		//[Test]
		//public void Any4([DataSources] string context)
		//{
		//	using (var db = GetDataContext(context))
		//		Assert.That(
		//			db.Parent
		//				.Select(p => new { children1 = p.Children.Where(c => c.ParentID > 1) })
		//				.Where(p => p.children1.Any())
		//				.Any(), Is.EqualTo(Parent
		//				.Select(p => new { children1 = p.Children.Where(c => c.ParentID > 1) })
		//				.Where(p => p.children1.Any())
		//				.Any()));
		//}

		[Test]
		public void LetTest1([DataSources(ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select ch.FirstOrDefault()!.ParentID
					,
					from p in db.Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select ch.FirstOrDefault()!.ParentID);
			}
		}

		[Test]
		public void LetTest2([DataSources(ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select p
					,
					from p in db.Parent
					let ch = p.Children
					where ch.FirstOrDefault() != null
					select p);
			}
		}

		[Test]
		public void LetTest3([DataSources(TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch = Child
					select ch.FirstOrDefault()!.ParentID
					,
					from p in db.Parent
					let ch = db.Child
					select ch.FirstOrDefault()!.ParentID);
			}
		}

		[Test]
		public void LetTest4([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0),
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0),
						First2 = ch2.FirstOrDefault()
					});
			}
		}

		[Test]
		public void LetTest41([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0),
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID).OrderBy(c => c.ChildID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0),
						First2 = ch2.FirstOrDefault()
					});
			}
		}

		[Test]
		public void LetTest5([DataSources(TestProvName.AllOracle11, TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0)!.ParentID,
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0)!.ParentID,
						First2 = ch2.FirstOrDefault()
					});
			}
		}

		[Test]
		public void LetTest6([DataSources(TestProvName.AllOracle11, TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllClickHouse)] string context)
		{
			//LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true;

			using (var db = GetDataContext(context))
			{
				var expected = (from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						p.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0)!.ParentID,
						First2 = ch2.FirstOrDefault()
					})
					.Where(t => t.ParentID > 0)
					.ToArray();

				var actual = (
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						p.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0)!.ParentID,
						First2 = ch2.FirstOrDefault()
					}
				)
					.Where(t => t.ParentID > 0)
					.ToArray();

				// Access has different order in result set
				if (!context.IsAnyOf(TestProvName.AllAccess))
					AreEqual(expected, actual);
			}
		}

		[Test]
		public void LetTest61([DataSources(TestProvName.AllOracle11, TestProvName.AllInformix, TestProvName.AllSybase, TestProvName.AllClickHouse)] string context)
		{
			//LinqToDB.Common.Configuration.Linq.GenerateExpressionTest = true;

			using (var db = GetDataContext(context))
			{
				var expected = (from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						p.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0)!.ParentID,
						First2 = ch2.FirstOrDefault()
					})
					.Where(t => t.ParentID > 0)
					.ToArray();

				var actual = (
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID).OrderBy(c => c.ChildID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					select new
					{
						p.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First1 = ch2.FirstOrDefault(c => c.ParentID > 0)!.ParentID,
						First2 = ch2.FirstOrDefault()
					}
				)
					.Where(t => t.ParentID > 0)
					.ToArray();

				// Access has different order in result set
				if (!context.IsAnyOf(TestProvName.AllAccess))
					AreEqual(expected, actual);
			}
		}

		// PostgreSQL92 Uses 3 queries and we join results in wrong order. See LetTest71 with explicit sort
		[Test]
		public void LetTest7([DataSources(TestProvName.AllInformix, ProviderName.PostgreSQL92, TestProvName.AllSybase, TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0)!.ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0).Take(5000)
					,
					(
						from p in db.Parent
						let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0)!.ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0).Take(5000));
		}

		[Test]
		public void LetTest71([DataSources(TestProvName.AllOracle11, TestProvName.AllInformix, TestProvName.AllSybase, ProviderName.Access, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0) == null ? 0 : ch2.FirstOrDefault(c => c.ParentID > 0)!.ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0).Take(5000)
					,
					(
						from p in db.Parent
						let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
						let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
						select new
						{
							p.ParentID,
							Any    = ch2.Any(),
							Count  = ch2.Count(),
							First1 = ch2.FirstOrDefault(c => c.ParentID > 0)!.ParentID,
							First2 = ch2.FirstOrDefault()
						}
					).Where(t => t.ParentID > 0).Take(5000));
		}

		[Test]
		public void LetTest8([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let ch1 = Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					let ch3	= ch2.OrderBy(c => c.ChildID).FirstOrDefault(c => c.ParentID > 0)
					select new
					{
						First1 = ch3 == null ? 0 : ch3.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
					let ch2 = ch1.OrderBy(c => c.ChildID).Where(c => c.ChildID > -100)
					let ch3	= ch2.OrderBy(c => c.ChildID).FirstOrDefault(c => c.ParentID > 0)
					select new
					{
						First1 = ch3 == null ? 0 : ch3.ParentID,
						Any    = ch2.Any(),
						Count  = ch2.Count(),
						First2 = ch2.FirstOrDefault()
					});
		}

		[Test]
		public void LetTest9([DataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					(
						from p in Parent
						let ch1 = Child.OrderBy(c => c.ChildID).Where(c => c.ParentID == p.ParentID)
						select new
						{
							First = ch1.FirstOrDefault()
						}
					).Take(10)
					,
					(
						from p in db.Parent
						let ch1 = db.Child.OrderBy(c => c.ChildID).Where(c => c.ParentID == p.ParentID)
						select new
						{
							First = ch1.FirstOrDefault()
						}
					).Take(10));
		}

		[Test]
		public void LetTest10([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				Assert.That(
					(
						from p in db.Parent
						let ch1 = db.Child.Where(c => c.ParentID == p.ParentID)
						select new
						{
							First = ch1.FirstOrDefault()
						}
					).Any(), Is.EqualTo((
						from p in Parent
						let ch1 = Child.Where(c => c.ParentID == p.ParentID)
						select new
						{
							First = ch1.FirstOrDefault()
						}
					).Any()
				));
		}

		[Test]
		public void LetTest11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					let ch1 = Child.FirstOrDefault(c => c.ParentID > 0)
					let ch2 = Child.Where(c => c.ChildID > -100)
					select new
					{
						First1 = ch1 == null ? 0 : ch1.ParentID,
						First2 = ch2.FirstOrDefault()
					}
					,
					from p in db.Parent
					let ch1 = db.Child.OrderBy(c => c.ParentID).FirstOrDefault(c => c.ParentID > 0)
					let ch2 = db.Child.OrderBy(c => c.ParentID).Where(c => c.ChildID > -100)
					orderby p.ParentID
					select new
					{
						First1 = ch1 == null ? 0 : ch1.ParentID,
						First2 = ch2.FirstOrDefault()
					});
		}

		#region Removal of compiler-generated conversions
		sealed class ConversionsTestTable
		{
			public sbyte   SByte  { get; set; }
			public byte    Byte   { get; set; }
			public short   Int16  { get; set; }
			public ushort  UInt16 { get; set; }
			public int     Int32  { get; set; }
			public uint    UInt32 { get; set; }
			public long    Int64  { get; set; }
			public ulong   UInt64 { get; set; }

			public sbyte?  SByteN  { get; set; }
			public byte?   ByteN   { get; set; }
			public short?  Int16N  { get; set; }
			public ushort? UInt16N { get; set; }
			public int?    Int32N  { get; set; }
			public uint?   UInt32N { get; set; }
			public long?   Int64N  { get; set; }
			public ulong?  UInt64N { get; set; }
		}

		enum EnumByte   : byte   { TestValue = 4 }
		enum EnumSByte  : sbyte  { TestValue = 4 }
		enum EnumInt16  : short  { TestValue = 4 }
		enum EnumUInt16 : ushort { TestValue = 4 }
		enum EnumInt32           { TestValue = 4 }
		enum EnumUInt32 : uint   { TestValue = 4 }
		enum EnumInt64  : long   { TestValue = 4 }
		enum EnumUInt64 : ulong  { TestValue = 4 }

		[Test]
		public void TestConversionRemovedForEnumOfTypeByte([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<ConversionsTestTable>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumByte.TestValue
							 || x.SByte   == (sbyte )EnumByte.TestValue
							 || x.Int16   == (short )EnumByte.TestValue
							 || x.UInt16  == (ushort)EnumByte.TestValue
							 || x.Int32   == (int   )EnumByte.TestValue
							 || x.UInt32  == (uint  )EnumByte.TestValue
							 || x.Int64   == (long  )EnumByte.TestValue
							 || x.UInt64  == (ulong )EnumByte.TestValue
							 || x.ByteN   == (byte  )EnumByte.TestValue
							 || x.SByteN  == (sbyte )EnumByte.TestValue
							 || x.Int16N  == (short )EnumByte.TestValue
							 || x.UInt16N == (ushort)EnumByte.TestValue
							 || x.Int32N  == (int   )EnumByte.TestValue
							 || x.UInt32N == (uint  )EnumByte.TestValue
							 || x.Int64N  == (long  )EnumByte.TestValue
							 || x.UInt64N == (ulong )EnumByte.TestValue

							 || (byte  )EnumByte.TestValue == x.Byte
							 || (sbyte )EnumByte.TestValue == x.SByte
							 || (short )EnumByte.TestValue == x.Int16
							 || (ushort)EnumByte.TestValue == x.UInt16
							 || (int   )EnumByte.TestValue == x.Int32
							 || (uint  )EnumByte.TestValue == x.UInt32
							 || (long  )EnumByte.TestValue == x.Int64
							 || (ulong )EnumByte.TestValue == x.UInt64
							 || (byte  )EnumByte.TestValue == x.ByteN
							 || (sbyte )EnumByte.TestValue == x.SByteN
							 || (short )EnumByte.TestValue == x.Int16N
							 || (ushort)EnumByte.TestValue == x.UInt16N
							 || (int   )EnumByte.TestValue == x.Int32N
							 || (uint  )EnumByte.TestValue == x.UInt32N
							 || (long  )EnumByte.TestValue == x.Int64N
							 || (ulong )EnumByte.TestValue == x.UInt64N)
					.ToList();

				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Not.Contain("convert"));
			}
		}

		[Test]
		public void TestConversionRemovedForEnumOfTypeSByte([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<ConversionsTestTable>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumSByte.TestValue
							 || x.SByte   == (sbyte )EnumSByte.TestValue
							 || x.Int16   == (short )EnumSByte.TestValue
							 || x.UInt16  == (ushort)EnumSByte.TestValue
							 || x.Int32   == (int   )EnumSByte.TestValue
							 || x.UInt32  == (uint  )EnumSByte.TestValue
							 || x.Int64   == (long  )EnumSByte.TestValue
							 || x.UInt64  == (ulong )EnumSByte.TestValue
							 || x.ByteN   == (byte  )EnumSByte.TestValue
							 || x.SByteN  == (sbyte )EnumSByte.TestValue
							 || x.Int16N  == (short )EnumSByte.TestValue
							 || x.UInt16N == (ushort)EnumSByte.TestValue
							 || x.Int32N  == (int   )EnumSByte.TestValue
							 || x.UInt32N == (uint  )EnumSByte.TestValue
							 || x.Int64N  == (long  )EnumSByte.TestValue
							 || x.UInt64N == (ulong )EnumSByte.TestValue

							 || (byte  )EnumSByte.TestValue == x.Byte
							 || (sbyte )EnumSByte.TestValue == x.SByte
							 || (short )EnumSByte.TestValue == x.Int16
							 || (ushort)EnumSByte.TestValue == x.UInt16
							 || (int   )EnumSByte.TestValue == x.Int32
							 || (uint  )EnumSByte.TestValue == x.UInt32
							 || (long  )EnumSByte.TestValue == x.Int64
							 || (ulong )EnumSByte.TestValue == x.UInt64
							 || (byte  )EnumSByte.TestValue == x.ByteN
							 || (sbyte )EnumSByte.TestValue == x.SByteN
							 || (short )EnumSByte.TestValue == x.Int16N
							 || (ushort)EnumSByte.TestValue == x.UInt16N
							 || (int   )EnumSByte.TestValue == x.Int32N
							 || (uint  )EnumSByte.TestValue == x.UInt32N
							 || (long  )EnumSByte.TestValue == x.Int64N
							 || (ulong )EnumSByte.TestValue == x.UInt64N)
					.ToList();

				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Not.Contain("convert"));
			}
		}

		[Test]
		public void TestConversionRemovedForEnumOfTypeInt16([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<ConversionsTestTable>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumInt16.TestValue
							 || x.SByte   == (sbyte )EnumInt16.TestValue
							 || x.Int16   == (short )EnumInt16.TestValue
							 || x.UInt16  == (ushort)EnumInt16.TestValue
							 || x.Int32   == (int   )EnumInt16.TestValue
							 || x.UInt32  == (uint  )EnumInt16.TestValue
							 || x.Int64   == (long  )EnumInt16.TestValue
							 || x.UInt64  == (ulong )EnumInt16.TestValue
							 || x.ByteN   == (byte  )EnumInt16.TestValue
							 || x.SByteN  == (sbyte )EnumInt16.TestValue
							 || x.Int16N  == (short )EnumInt16.TestValue
							 || x.UInt16N == (ushort)EnumInt16.TestValue
							 || x.Int32N  == (int   )EnumInt16.TestValue
							 || x.UInt32N == (uint  )EnumInt16.TestValue
							 || x.Int64N  == (long  )EnumInt16.TestValue
							 || x.UInt64N == (ulong )EnumInt16.TestValue

							 || (byte  )EnumInt16.TestValue == x.Byte
							 || (sbyte )EnumInt16.TestValue == x.SByte
							 || (short )EnumInt16.TestValue == x.Int16
							 || (ushort)EnumInt16.TestValue == x.UInt16
							 || (int   )EnumInt16.TestValue == x.Int32
							 || (uint  )EnumInt16.TestValue == x.UInt32
							 || (long  )EnumInt16.TestValue == x.Int64
							 || (ulong )EnumInt16.TestValue == x.UInt64
							 || (byte  )EnumInt16.TestValue == x.ByteN
							 || (sbyte )EnumInt16.TestValue == x.SByteN
							 || (short )EnumInt16.TestValue == x.Int16N
							 || (ushort)EnumInt16.TestValue == x.UInt16N
							 || (int   )EnumInt16.TestValue == x.Int32N
							 || (uint  )EnumInt16.TestValue == x.UInt32N
							 || (long  )EnumInt16.TestValue == x.Int64N
							 || (ulong )EnumInt16.TestValue == x.UInt64N)
					.ToList();

				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Not.Contain("convert"));
			}
		}

		[Test]
		public void TestConversionRemovedForEnumOfTypeUInt16([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<ConversionsTestTable>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumUInt16.TestValue
							 || x.SByte   == (sbyte )EnumUInt16.TestValue
							 || x.Int16   == (short )EnumUInt16.TestValue
							 || x.UInt16  == (ushort)EnumUInt16.TestValue
							 || x.Int32   == (int   )EnumUInt16.TestValue
							 || x.UInt32  == (uint  )EnumUInt16.TestValue
							 || x.Int64   == (long  )EnumUInt16.TestValue
							 || x.UInt64  == (ulong )EnumUInt16.TestValue
							 || x.ByteN   == (byte  )EnumUInt16.TestValue
							 || x.SByteN  == (sbyte )EnumUInt16.TestValue
							 || x.Int16N  == (short )EnumUInt16.TestValue
							 || x.UInt16N == (ushort)EnumUInt16.TestValue
							 || x.Int32N  == (int   )EnumUInt16.TestValue
							 || x.UInt32N == (uint  )EnumUInt16.TestValue
							 || x.Int64N  == (long  )EnumUInt16.TestValue
							 || x.UInt64N == (ulong )EnumUInt16.TestValue

							 || (byte  )EnumUInt16.TestValue == x.Byte
							 || (sbyte )EnumUInt16.TestValue == x.SByte
							 || (short )EnumUInt16.TestValue == x.Int16
							 || (ushort)EnumUInt16.TestValue == x.UInt16
							 || (int   )EnumUInt16.TestValue == x.Int32
							 || (uint  )EnumUInt16.TestValue == x.UInt32
							 || (long  )EnumUInt16.TestValue == x.Int64
							 || (ulong )EnumUInt16.TestValue == x.UInt64
							 || (byte  )EnumUInt16.TestValue == x.ByteN
							 || (sbyte )EnumUInt16.TestValue == x.SByteN
							 || (short )EnumUInt16.TestValue == x.Int16N
							 || (ushort)EnumUInt16.TestValue == x.UInt16N
							 || (int   )EnumUInt16.TestValue == x.Int32N
							 || (uint  )EnumUInt16.TestValue == x.UInt32N
							 || (long  )EnumUInt16.TestValue == x.Int64N
							 || (ulong )EnumUInt16.TestValue == x.UInt64N)
					.ToList();

				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Not.Contain("convert"));
			}
		}

		[Test]
		public void TestConversionRemovedForEnumOfTypeInt32([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<ConversionsTestTable>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumInt32.TestValue
							 || x.SByte   == (sbyte )EnumInt32.TestValue
							 || x.Int16   == (short )EnumInt32.TestValue
							 || x.UInt16  == (ushort)EnumInt32.TestValue
							 || x.Int32   == (int   )EnumInt32.TestValue
							 || x.UInt32  == (uint  )EnumInt32.TestValue
							 || x.Int64   == (long  )EnumInt32.TestValue
							 || x.UInt64  == (ulong )EnumInt32.TestValue
							 || x.ByteN   == (byte  )EnumInt32.TestValue
							 || x.SByteN  == (sbyte )EnumInt32.TestValue
							 || x.Int16N  == (short )EnumInt32.TestValue
							 || x.UInt16N == (ushort)EnumInt32.TestValue
							 || x.Int32N  == (int   )EnumInt32.TestValue
							 || x.UInt32N == (uint  )EnumInt32.TestValue
							 || x.Int64N  == (long  )EnumInt32.TestValue
							 || x.UInt64N == (ulong )EnumInt32.TestValue

							 || (byte  )EnumInt32.TestValue == x.Byte
							 || (sbyte )EnumInt32.TestValue == x.SByte
							 || (short )EnumInt32.TestValue == x.Int16
							 || (ushort)EnumInt32.TestValue == x.UInt16
							 || (int   )EnumInt32.TestValue == x.Int32
							 || (uint  )EnumInt32.TestValue == x.UInt32
							 || (long  )EnumInt32.TestValue == x.Int64
							 || (ulong )EnumInt32.TestValue == x.UInt64
							 || (byte  )EnumInt32.TestValue == x.ByteN
							 || (sbyte )EnumInt32.TestValue == x.SByteN
							 || (short )EnumInt32.TestValue == x.Int16N
							 || (ushort)EnumInt32.TestValue == x.UInt16N
							 || (int   )EnumInt32.TestValue == x.Int32N
							 || (uint  )EnumInt32.TestValue == x.UInt32N
							 || (long  )EnumInt32.TestValue == x.Int64N
							 || (ulong )EnumInt32.TestValue == x.UInt64N)
					.ToList();

				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Not.Contain("convert"));
			}
		}

		[Test]
		public void TestConversionRemovedForEnumOfTypeUInt32([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<ConversionsTestTable>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumUInt32.TestValue
							 || x.SByte   == (sbyte )EnumUInt32.TestValue
							 || x.Int16   == (short )EnumUInt32.TestValue
							 || x.UInt16  == (ushort)EnumUInt32.TestValue
							 || x.Int32   == (int   )EnumUInt32.TestValue
							 || x.UInt32  == (uint  )EnumUInt32.TestValue
							 || x.Int64   == (long  )EnumUInt32.TestValue
							 || x.UInt64  == (ulong )EnumUInt32.TestValue
							 || x.ByteN   == (byte  )EnumUInt32.TestValue
							 || x.SByteN  == (sbyte )EnumUInt32.TestValue
							 || x.Int16N  == (short )EnumUInt32.TestValue
							 || x.UInt16N == (ushort)EnumUInt32.TestValue
							 || x.Int32N  == (int   )EnumUInt32.TestValue
							 || x.UInt32N == (uint  )EnumUInt32.TestValue
							 || x.Int64N  == (long  )EnumUInt32.TestValue
							 || x.UInt64N == (ulong )EnumUInt32.TestValue

							 || (byte  )EnumUInt32.TestValue == x.Byte
							 || (sbyte )EnumUInt32.TestValue == x.SByte
							 || (short )EnumUInt32.TestValue == x.Int16
							 || (ushort)EnumUInt32.TestValue == x.UInt16
							 || (int   )EnumUInt32.TestValue == x.Int32
							 || (uint  )EnumUInt32.TestValue == x.UInt32
							 || (long  )EnumUInt32.TestValue == x.Int64
							 || (ulong )EnumUInt32.TestValue == x.UInt64
							 || (byte  )EnumUInt32.TestValue == x.ByteN
							 || (sbyte )EnumUInt32.TestValue == x.SByteN
							 || (short )EnumUInt32.TestValue == x.Int16N
							 || (ushort)EnumUInt32.TestValue == x.UInt16N
							 || (int   )EnumUInt32.TestValue == x.Int32N
							 || (uint  )EnumUInt32.TestValue == x.UInt32N
							 || (long  )EnumUInt32.TestValue == x.Int64N
							 || (ulong )EnumUInt32.TestValue == x.UInt64N)
					.ToList();

				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Not.Contain("convert"));
			}
		}

		[Test]
		public void TestConversionRemovedForEnumOfTypeInt64([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<ConversionsTestTable>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumInt64.TestValue
							 || x.SByte   == (sbyte )EnumInt64.TestValue
							 || x.Int16   == (short )EnumInt64.TestValue
							 || x.UInt16  == (ushort)EnumInt64.TestValue
							 || x.Int32   == (int   )EnumInt64.TestValue
							 || x.UInt32  == (uint  )EnumInt64.TestValue
							 || x.Int64   == (long  )EnumInt64.TestValue
							 || x.UInt64  == (ulong )EnumInt64.TestValue
							 || x.ByteN   == (byte  )EnumInt64.TestValue
							 || x.SByteN  == (sbyte )EnumInt64.TestValue
							 || x.Int16N  == (short )EnumInt64.TestValue
							 || x.UInt16N == (ushort)EnumInt64.TestValue
							 || x.Int32N  == (int   )EnumInt64.TestValue
							 || x.UInt32N == (uint  )EnumInt64.TestValue
							 || x.Int64N  == (long  )EnumInt64.TestValue
							 || x.UInt64N == (ulong )EnumInt64.TestValue

							 || (byte  )EnumInt64.TestValue == x.Byte
							 || (sbyte )EnumInt64.TestValue == x.SByte
							 || (short )EnumInt64.TestValue == x.Int16
							 || (ushort)EnumInt64.TestValue == x.UInt16
							 || (int   )EnumInt64.TestValue == x.Int32
							 || (uint  )EnumInt64.TestValue == x.UInt32
							 || (long  )EnumInt64.TestValue == x.Int64
							 || (ulong )EnumInt64.TestValue == x.UInt64
							 || (byte  )EnumInt64.TestValue == x.ByteN
							 || (sbyte )EnumInt64.TestValue == x.SByteN
							 || (short )EnumInt64.TestValue == x.Int16N
							 || (ushort)EnumInt64.TestValue == x.UInt16N
							 || (int   )EnumInt64.TestValue == x.Int32N
							 || (uint  )EnumInt64.TestValue == x.UInt32N
							 || (long  )EnumInt64.TestValue == x.Int64N
							 || (ulong )EnumInt64.TestValue == x.UInt64N)
					.ToList();

				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Not.Contain("convert"));
			}
		}

		[Test]
		public void TestConversionRemovedForEnumOfTypeUInt64([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = GetDataConnection(context))
			using (var table = db.CreateLocalTable<ConversionsTestTable>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumUInt64.TestValue
							 || x.SByte   == (sbyte )EnumUInt64.TestValue
							 || x.Int16   == (short )EnumUInt64.TestValue
							 || x.UInt16  == (ushort)EnumUInt64.TestValue
							 || x.Int32   == (int   )EnumUInt64.TestValue
							 || x.UInt32  == (uint  )EnumUInt64.TestValue
							 || x.Int64   == (long  )EnumUInt64.TestValue
							 || x.UInt64  == (ulong )EnumUInt64.TestValue
							 || x.ByteN   == (byte  )EnumUInt64.TestValue
							 || x.SByteN  == (sbyte )EnumUInt64.TestValue
							 || x.Int16N  == (short )EnumUInt64.TestValue
							 || x.UInt16N == (ushort)EnumUInt64.TestValue
							 || x.Int32N  == (int   )EnumUInt64.TestValue
							 || x.UInt32N == (uint  )EnumUInt64.TestValue
							 || x.Int64N  == (long  )EnumUInt64.TestValue
							 || x.UInt64N == (ulong )EnumUInt64.TestValue

							 || (byte  )EnumUInt64.TestValue == x.Byte
							 || (sbyte )EnumUInt64.TestValue == x.SByte
							 || (short )EnumUInt64.TestValue == x.Int16
							 || (ushort)EnumUInt64.TestValue == x.UInt16
							 || (int   )EnumUInt64.TestValue == x.Int32
							 || (uint  )EnumUInt64.TestValue == x.UInt32
							 || (long  )EnumUInt64.TestValue == x.Int64
							 || (ulong )EnumUInt64.TestValue == x.UInt64
							 || (byte  )EnumUInt64.TestValue == x.ByteN
							 || (sbyte )EnumUInt64.TestValue == x.SByteN
							 || (short )EnumUInt64.TestValue == x.Int16N
							 || (ushort)EnumUInt64.TestValue == x.UInt16N
							 || (int   )EnumUInt64.TestValue == x.Int32N
							 || (uint  )EnumUInt64.TestValue == x.UInt32N
							 || (long  )EnumUInt64.TestValue == x.Int64N
							 || (ulong )EnumUInt64.TestValue == x.UInt64N)
					.ToList();

				Assert.That(db.LastQuery!.ToLowerInvariant(), Does.Not.Contain("convert"));
			}
		}

		#endregion

		#region issue 3791
		[Table]
		public class Issue3791Table
		{
			[Identity, PrimaryKey] public int     Id      { get; set; }
			[Column              ] public string? OtherId { get; set; }

			[Association(ThisKey = nameof(OtherId), OtherKey = nameof(Issue3791GuidTable.Id))]
			public Issue3791GuidTable? Association { get; set; }
		}
		[Table(IsColumnAttributeRequired = false)]
		public class Issue3791GuidTable
		{
			[PrimaryKey] public Guid Id { get; set; }
		}

		[Test]
		public void Issue3791Test([DataSources] string context)
		{
			var ms = new MappingSchema();
			ms.SetConvertExpression<Guid, string>(a => a.ToString());
			ms.SetConvertExpression<string, Guid>(a => Guid.Parse(a));

			using var db = GetDataContext(context);

			using var table = db.CreateLocalTable<Issue3791Table>();
			using var _     = db.CreateLocalTable<Issue3791GuidTable>();

			table.LoadWith(a => a.Association).ToList();
		}
		#endregion
	}
}
