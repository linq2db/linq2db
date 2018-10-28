using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1313Tests : TestBase
	{
		/// <summary>
		/// Base class for testing, defines fields that will be replaced in derived class to show original problem.
		/// </summary>
		public class I1313_Base
		{
			/// <summary>
			/// Marker, unaltered in derived class.
			/// </summary>
			public bool MarkerProperty { get; set; }

			/// <summary>
			/// Field to change to property in derived class.
			/// </summary>
			public int ChangeToProp;

			/// <summary>
			/// Field to change type of in derived class.
			/// </summary>
			public int ChangeFieldType;

			/// <summary>
			/// Property to change to field in derived class.
			/// </summary>
			public int ChangeToField { get; set; }

			/// <summary>
			/// Property to change type of in derived class.
			/// </summary>
			public int ChangePropType { get; set; }
		}

		/// <summary>
		/// Derived class, replaces each data member (field + property) in base class above using the 'new' keyword.
		/// </summary>
		public class I1313_Derived : I1313_Base
		{
			/// <summary>
			/// Field changed to property.
			/// </summary>
			public new int ChangeToProp { get; set; }

			/// <summary>
			/// Field changed to long from int.
			/// </summary>
			public new long ChangeFieldType;

			/// <summary>
			/// Property changed to field.
			/// </summary>
			public new int ChangeToField;

			/// <summary>
			/// Property changed to long from int.
			/// </summary>
			public new long ChangePropType { get; set; }
		}

		public class ValueItem
		{
			public int Value { get; set; }
		}

		[Test]
		public void TestMemberReplacement()
		{
			var type = typeof(I1313_Derived);

			var members = type.GetPublicInstanceValueMembers();

			// Test returned array
			Assert.IsNotNull(members);
			Assert.IsTrue(members.Length == 5, "Expected 5 returned members, found {0}.", members.Length);

			// Check for duplicate names
			string[] dupNames = 
				(
					from m in members
					group 1 by m.Name into grp
					where grp.Count() > 1
					select grp.Key
				).ToArray();
			Assert.IsFalse(dupNames.Any(), "Found duplicate entries for: {0}.", string.Join(", ", dupNames));

			// Check that returned members are all from the derived class except the marker property
			var baseMembers =
				(
					from m in members
					where m.Name != nameof(I1313_Base.MarkerProperty) && m.DeclaringType != typeof(I1313_Derived)
					select m.Name
				).ToArray();
			Assert.IsFalse(baseMembers.Any(), "Found incorrect base class member(s): {0}.", string.Join(", ", baseMembers));
		}

		[Test, DataContextSource(false, ProviderName.SQLiteMS)]
		public void TestQuery(string context)
		{
			using (var db = new DataConnection(context))
			{
				using (var table = db.CreateLocalTable<ValueItem>())
				{
					table.Insert(() => new ValueItem { Value = 123 });

					IQueryable<I1313_Derived> query = null;
					Assert.DoesNotThrow(() =>
						query =
							from row in table
							select new I1313_Derived
							{
								MarkerProperty = false,
								ChangeToProp = row.Value,
								ChangeFieldType = 2,
								ChangeToField = 3,
								ChangePropType = 4,
							}
					);

					// Ensure the expected records are returned
					var records = query.ToArray();
					Assert.IsNotNull(records);
					Assert.AreEqual(1, records.Length);

					// Check the returned values are as expected
					var record = records.First();
					Assert.IsTrue(record.ChangeToProp == 123 && record.ChangeFieldType == 2 && record.ChangeToField == 3 && record.ChangePropType == 4, "Unexpected values in record.");

					// Check the replaced base class fields have not been set
					I1313_Base base_record = record;
					Assert.AreSame(record, base_record);
					Assert.IsTrue(base_record.ChangeToProp == 0 && base_record.ChangeFieldType == 0 && base_record.ChangeToField == 0 && base_record.ChangePropType == 0, "Data leakage to base class members.");
				}
			}
		}
	}
}
