using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;

using Tests.Model;

namespace Tests
{
	public partial class TestBase
	{
		internal const int MaxPersonID = 4;

		protected List<LinqDataTypes> GetTypes(string context)
		{
			return DataCache<LinqDataTypes>.Get(context);
		}

		#region Reference data

		// One snapshot per process, captured by the default provider's CreateDatabase before its readiness
		// latch opens; readers on other lanes wait on that latch rather than read the default DB mid-write.
		static volatile ReferenceData? _referenceData;
		static readonly Lock           _referenceDataSync = new();

		/// <summary>
		/// Captures the reference-data snapshot when <paramref name="context"/> is the default configuration.
		/// Called by the default provider's <c>CreateDatabase</c> once its scripts have run.
		/// </summary>
		protected static void CaptureReferenceData(string context)
		{
			if (string.Equals(context, TestConfiguration.DefaultProvider, StringComparison.OrdinalIgnoreCase))
				_referenceData = ReferenceData.Load(context);
		}

		static ReferenceData Data
		{
			get
			{
				var data = _referenceData;

				if (data != null)
					return data;

				if (ParallelExecutionEnabled && TestConfiguration.DefaultProvider is { } provider)
				{
					CustomTestContext.EnsureDatabaseReady(provider);

					data = _referenceData;

					if (data != null)
						return data;
				}

				// no capture in this run: serial run, CreateDatabase filtered out, or a host without it
				lock (_referenceDataSync)
					return _referenceData ??= ReferenceData.Load(TestConfiguration.DefaultProvider);
			}
		}

		sealed class ReferenceData
		{
			public readonly List<LinqDataTypes>          Types;
			public readonly List<LinqDataTypes2>         Types2;
			public readonly List<Person>                 Person;
			public readonly List<Patient>                Patient;
			public readonly List<Doctor>                 Doctor;
			public readonly List<Parent>                 Parent;
			public readonly List<Parent1>                Parent1;
			public readonly List<Parent4>                Parent4;
			public readonly List<Parent5>                Parent5;
			public readonly List<Child>                  Child;
			public readonly List<GrandChild>             GrandChild;
			public readonly List<GrandChild1>            GrandChild1;
			public readonly List<InheritanceParentBase>  InheritanceParent;
			public readonly List<InheritanceChildBase>   InheritanceChild;
			public readonly List<ParentInheritanceBase>  ParentInheritance;
			public readonly List<ParentInheritanceValue> ParentInheritanceValue;
			public readonly List<ParentInheritance1>     ParentInheritance1;
			public readonly List<ParentInheritanceBase4> ParentInheritance4;

			public static ReferenceData Load(string? configuration)
			{
				using (new DisableLogging())
				using (new DisableBaseline("Default Database"))
				using (var db = configuration != null ? new TestDataConnection(configuration) : new TestDataConnection())
					return new ReferenceData(db);
			}

			ReferenceData(TestDataConnection db)
			{
				db.Child.Delete(c => c.ParentID >= 1000);

				Types             = db.Types            .ToList();
				Types2            = db.Types2           .ToList();
				Person            = db.Person           .ToList();
				Patient           = db.Patient          .ToList();
				Doctor            = db.Doctor           .ToList();
				Parent            = db.Parent           .ToList();
				Child             = db.Child            .ToList();
				GrandChild        = db.GrandChild       .ToList();
				GrandChild1       = db.GrandChild1      .ToList();
				InheritanceParent = db.InheritanceParent.ToList();
				InheritanceChild  = db.InheritanceChild .LoadWith(_ => _.Parent).ToList();

				foreach (var p in Person)
					p.Patient = Patient.SingleOrDefault(ps => p.ID == ps.PersonID);

				foreach (var p in Patient)
					p.Person = Person.Single(ps => ps.ID == p.PersonID);

				Parent1 = Parent.Select(p => new Parent1 { ParentID = p.ParentID, Value1 = p.Value1 }).ToList();

				foreach (var p in Parent)
				{
					p.ParentTest    = p;
					p.Children      = Child     .Where(c => c.ParentID == p.ParentID).ToList();
					p.GrandChildren = GrandChild.Where(c => c.ParentID == p.ParentID).ToList();
					p.Types         = Types.FirstOrDefault(t => t.ID == p.ParentID);
				}

				foreach (var ch in Child)
				{
					ch.Parent        = Parent .Single(p => p.ParentID == ch.ParentID);
					ch.Parent1       = Parent1.Single(p => p.ParentID == ch.ParentID);
					ch.ParentID2     = new Parent3 { ParentID2 = ch.Parent.ParentID, Value1 = ch.Parent.Value1 };
					ch.GrandChildren = GrandChild.Where(c => c.ParentID == ch.ParentID && c.ChildID == ch.ChildID).ToList();
				}

				foreach (var ch in GrandChild)
					ch.Child = Child.Single(c => c.ParentID == ch.ParentID && c.ChildID == ch.ChildID);

				foreach (var ch in GrandChild1)
				{
					ch.Parent = Parent1.Single(p => p.ParentID == ch.ParentID);
					ch.Child  = Child  .Single(c => c.ParentID == ch.ParentID && c.ChildID == ch.ChildID);
				}

				Parent4 = Parent.Select(p => new Parent4 { ParentID = p.ParentID, Value1 = ConvertTo<TypeValue>.From(p.Value1) }).ToList();
				Parent5 = Parent.Select(p => new Parent5 { ParentID = p.ParentID, Value1 = p.Value1 }).ToList();

				foreach (var p in Parent5)
					p.Children = Parent5.Where(c => c.Value1 == p.ParentID).ToList();

				ParentInheritance = Parent.Select(p =>
						p.Value1 == null ? new ParentInheritanceNull { ParentID = p.ParentID } :
						p.Value1.Value == 1 ? new ParentInheritance1 { ParentID = p.ParentID, Value1 = p.Value1.Value } :
						 (ParentInheritanceBase)new ParentInheritanceValue { ParentID = p.ParentID, Value1 = p.Value1.Value }
					).ToList();

				ParentInheritanceValue = ParentInheritance.OfType<ParentInheritanceValue>().ToList();
				ParentInheritance1     = ParentInheritance.OfType<ParentInheritance1>().ToList();

				ParentInheritance4 = Parent
					.Where(p => p.Value1.HasValue && (p.Value1.Value == 1 || p.Value1.Value == 2))
					.Select(p => p.Value1 == 1 ?
						(ParentInheritanceBase4)new ParentInheritance14 { ParentID = p.ParentID } :
												new ParentInheritance24 { ParentID = p.ParentID }
					).ToList();
			}
		}

		protected IEnumerable<LinqDataTypes>          Types                  => Data.Types;
		protected List<LinqDataTypes2>                Types2                 => Data.Types2;
		protected IEnumerable<Person>                 Person                 => Data.Person;
		protected List<Patient>                       Patient                => Data.Patient;
		protected List<Doctor>                        Doctor                 => Data.Doctor;
		protected IEnumerable<Parent>                 Parent                 => Data.Parent;
		protected IEnumerable<Parent1>                Parent1                => Data.Parent1;
		protected List<Parent4>                       Parent4                => Data.Parent4;
		protected List<Parent5>                       Parent5                => Data.Parent5;
		protected IEnumerable<Child>                  Child                  => Data.Child;
		protected IEnumerable<GrandChild>             GrandChild             => Data.GrandChild;
		protected IEnumerable<GrandChild1>            GrandChild1            => Data.GrandChild1;
		protected List<InheritanceParentBase>         InheritanceParent      => Data.InheritanceParent;
		protected List<InheritanceChildBase>          InheritanceChild       => Data.InheritanceChild;
		protected IEnumerable<ParentInheritanceBase>  ParentInheritance      => Data.ParentInheritance;
		protected List<ParentInheritanceValue>        ParentInheritanceValue => Data.ParentInheritanceValue;
		protected List<ParentInheritance1>            ParentInheritance1     => Data.ParentInheritance1;
		protected List<ParentInheritanceBase4>        ParentInheritance4     => Data.ParentInheritance4;
		#endregion

		#region Northwind

		protected TestBaseNorthwind GetNorthwindAsList(string context)
		{
			return new TestBaseNorthwind(context);
		}

		protected class TestBaseNorthwind
		{
			private string _context;

			public TestBaseNorthwind(string context)
			{
				_context = context;
			}

			private List<Northwind.Category>? _category;
			public List<Northwind.Category> Category
			{
				get
				{
					if (_category == null)
						using (new DisableLogging())
						using (new DisableBaseline("Default Database"))
						using (var db = new NorthwindDB(_context))
							_category = db.Category.ToList();
					return _category;
				}
			}

			private List<Northwind.Customer>? _customer;
			public List<Northwind.Customer> Customer
			{
				get
				{
					if (_customer == null)
					{
						using (new DisableLogging())
						using (new DisableBaseline("Default Database"))
						using (var db = new NorthwindDB(_context))
							_customer = db.Customer.ToList();

						foreach (var c in _customer)
							c.Orders = (from o in Order where o.CustomerID == c.CustomerID select o).ToList();
					}

					return _customer;
				}
			}

			private List<Northwind.Employee>? _employee;
			public List<Northwind.Employee> Employee
			{
				get
				{
					if (_employee == null)
					{
						using (new DisableLogging())
						using (new DisableBaseline("Default Database"))
						using (var db = new NorthwindDB(_context))
						{
							_employee = db.Employee.ToList();

							foreach (var employee in _employee)
							{
								employee.Employees = (from e in _employee where e.ReportsTo == employee.EmployeeID select e).ToList();
								employee.ReportsToEmployee = (from e in _employee where e.EmployeeID == employee.ReportsTo select e).SingleOrDefault();
							}
						}
					}

					return _employee;
				}
			}

			private List<Northwind.EmployeeTerritory>? _employeeTerritory;
			public List<Northwind.EmployeeTerritory> EmployeeTerritory
			{
				get
				{
					if (_employeeTerritory == null)
						using (new DisableLogging())
						using (new DisableBaseline("Default Database"))
						using (var db = new NorthwindDB(_context))
							_employeeTerritory = db.EmployeeTerritory.ToList();
					return _employeeTerritory;
				}
			}

			private List<Northwind.OrderDetail>? _orderDetail;
			public List<Northwind.OrderDetail> OrderDetail
			{
				get
				{
					if (_orderDetail == null)
						using (new DisableLogging())
						using (new DisableBaseline("Default Database"))
						using (var db = new NorthwindDB(_context))
							_orderDetail = db.OrderDetail.ToList();
					return _orderDetail;
				}
			}

			private List<Northwind.Order>? _order;
			public List<Northwind.Order> Order
			{
				get
				{
					if (_order == null)
					{
						using (new DisableLogging())
						using (new DisableBaseline("Default Database"))
						using (var db = new NorthwindDB(_context))
							_order = db.Order.ToList();

						foreach (var o in _order)
						{
							o.Customer = Customer.Single(c => o.CustomerID == c.CustomerID);
							o.Employee = Employee.Single(e => o.EmployeeID == e.EmployeeID);
						}
					}

					return _order;
				}
			}

			private IEnumerable<Northwind.Product>? _product;
			public IEnumerable<Northwind.Product> Product
			{
				get
				{
					if (_product == null)
						using (new DisableLogging())
						using (new DisableBaseline("Default Database"))
						using (var db = new NorthwindDB(_context))
							_product = db.Product.ToList();

					foreach (var product in _product)
						yield return product;
				}
			}

			private List<Northwind.ActiveProduct>? _activeProduct;
			public List<Northwind.ActiveProduct> ActiveProduct => _activeProduct ??= Product.OfType<Northwind.ActiveProduct>().ToList();

			public IEnumerable<Northwind.DiscontinuedProduct> DiscontinuedProduct => Product.OfType<Northwind.DiscontinuedProduct>();

			private List<Northwind.Region>? _region;
			public List<Northwind.Region> Region
			{
				get
				{
					if (_region == null)
						using (new DisableLogging())
						using (new DisableBaseline("Default Database"))
						using (var db = new NorthwindDB(_context))
							_region = db.Region.ToList();
					return _region;
				}
			}

			private List<Northwind.Shipper>? _shipper;
			public List<Northwind.Shipper> Shipper
			{
				get
				{
					if (_shipper == null)
						using (new DisableLogging())
						using (new DisableBaseline("Default Database"))
						using (var db = new NorthwindDB(_context))
							_shipper = db.Shipper.ToList();
					return _shipper;
				}
			}

			private List<Northwind.Supplier>? _supplier;
			public List<Northwind.Supplier> Supplier
			{
				get
				{
					if (_supplier == null)
						using (new DisableLogging())
						using (new DisableBaseline("Default Database"))
						using (var db = new NorthwindDB(_context))
							_supplier = db.Supplier.ToList();
					return _supplier;
				}
			}

			private List<Northwind.Territory>? _territory;
			public List<Northwind.Territory> Territory
			{
				get
				{
					if (_territory == null)
						using (new DisableLogging())
						using (new DisableBaseline("Default Database"))
						using (var db = new NorthwindDB(_context))
							_territory = db.Territory.ToList();
					return _territory;
				}
			}
		}

		#endregion

		static class DataCache<T>
			where T : class
		{
			static readonly Lock                       _lock = new();
			static readonly Dictionary<string,List<T>> _dic  = new();
			public static List<T> Get(string context)
			{
				lock (_lock)
				{
					context = context.StripRemote();

					if (!_dic.TryGetValue(context, out var list))
					{
						using (new DisableLogging())
						using (new DisableBaseline("Test Cache"))
						using (var db = new DataConnection(context))
						{
							list = db.GetTable<T>().ToList();
							_dic.Add(context, list);
						}
					}

					return list;
				}
			}

			public static void Clear()
			{
				_dic.Clear();
			}
		}
	}
}
