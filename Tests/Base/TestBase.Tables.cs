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

		private   List<LinqDataTypes>?      _types;
		protected IEnumerable<LinqDataTypes> Types
		{
			get
			{
				if (_types == null)
					using (new DisableLogging())
					using (new DisableBaseline("Default Database"))
					using (var db = new TestDataConnection())
						_types = db.Types.ToList();

				return _types;
			}
		}

		private   List<LinqDataTypes2>? _types2;
		protected List<LinqDataTypes2> Types2
		{
			get
			{
				if (_types2 == null)
					using (new DisableLogging())
					using (new DisableBaseline("Default Database"))
					using (var db = new TestDataConnection())
						_types2 = db.Types2.ToList();

				return _types2;
			}
		}

		#region Person Model
		private   List<Person>?       _person;
		protected IEnumerable<Person> Person
		{
			get
			{
				if (_person == null)
				{
					InitPatientPerson();
				}

				return _person!;
			}
		}

		private   List<Patient>? _patient;
		protected List<Patient> Patient
		{
			get
			{
				if (_patient == null)
				{
					InitPatientPerson();
				}

				return _patient!;
			}
		}

		private   List<Doctor>? _doctor;
		protected List<Doctor> Doctor
		{
			get
			{
				if (_doctor == null)
				{
					using (new DisableLogging())
					using (new DisableBaseline("Default Database"))
					using (var db = new TestDataConnection())
						_doctor = db.Doctor.ToList();
				}

				return _doctor;
			}
		}
		#endregion

		#region Parent/Child Model

		private   List<Parent>?      _parent;
		protected IEnumerable<Parent> Parent
		{
			get
			{
				if (_parent == null)
					using (new DisableLogging())
					using (new DisableBaseline("Default Database"))
					using (var db = new TestDataConnection())
					{
						_parent = db.Parent.ToList();
						db.Close();

						foreach (var p in _parent)
						{
							p.ParentTest = p;
							p.Children = Child.Where(c => c.ParentID == p.ParentID).ToList();
							p.GrandChildren = GrandChild.Where(c => c.ParentID == p.ParentID).ToList();
							p.Types = Types.FirstOrDefault(t => t.ID == p.ParentID);
						}
					}

				return _parent;
			}
		}

		private   List<Parent1>?      _parent1;
		protected IEnumerable<Parent1> Parent1
		{
			get
			{
				_parent1 ??= Parent.Select(p => new Parent1 { ParentID = p.ParentID, Value1 = p.Value1 }).ToList();

				return _parent1;
			}
		}

		private   List<Parent4>? _parent4;
		protected List<Parent4> Parent4 => _parent4 ??= Parent.Select(p => new Parent4 { ParentID = p.ParentID, Value1 = ConvertTo<TypeValue>.From(p.Value1) }).ToList();

		private   List<Parent5>? _parent5;
		protected List<Parent5> Parent5
		{
			get
			{
				if (_parent5 == null)
				{
					_parent5 = Parent.Select(p => new Parent5 { ParentID = p.ParentID, Value1 = p.Value1 }).ToList();

					foreach (var p in _parent5)
						p.Children = _parent5.Where(c => c.Value1 == p.ParentID).ToList();
				}

				return _parent5;
			}
		}

		protected List<Child>?      _child;
		protected IEnumerable<Child> Child
		{
			get
			{
				if (_child == null)
					using (new DisableLogging())
					using (new DisableBaseline("Default Database"))
					using (var db = new TestDataConnection())
					{
						db.Child.Delete(c => c.ParentID >= 1000);
						_child = db.Child.ToList();
						db.Close();

						foreach (var ch in _child)
						{
							ch.Parent = Parent.Single(p => p.ParentID == ch.ParentID);
							ch.Parent1 = Parent1.Single(p => p.ParentID == ch.ParentID);
							ch.ParentID2 = new Parent3 { ParentID2 = ch.Parent.ParentID, Value1 = ch.Parent.Value1 };
							ch.GrandChildren = GrandChild.Where(c => c.ParentID == ch.ParentID && c.ChildID == ch.ChildID).ToList();
						}
					}

				foreach (var item in _child)
					yield return item;
			}
		}

		private   List<GrandChild>?      _grandChild;
		protected IEnumerable<GrandChild> GrandChild
		{
			get
			{
				if (_grandChild == null)
					using (new DisableLogging())
					using (new DisableBaseline("Default Database"))
					using (var db = new TestDataConnection())
					{
						_grandChild = db.GrandChild.ToList();
						db.Close();

						foreach (var ch in _grandChild)
							ch.Child = Child.Single(c => c.ParentID == ch.ParentID && c.ChildID == ch.ChildID);
					}

				return _grandChild;
			}
		}

		private   List<GrandChild1>?      _grandChild1;
		protected IEnumerable<GrandChild1> GrandChild1
		{
			get
			{
				if (_grandChild1 == null)
					using (new DisableLogging())
					using (new DisableBaseline("Default Database"))
					using (var db = new TestDataConnection())
					{
						_grandChild1 = db.GrandChild1.ToList();

						foreach (var ch in _grandChild1)
						{
							ch.Parent = Parent1.Single(p => p.ParentID == ch.ParentID);
							ch.Child = Child.Single(c => c.ParentID == ch.ParentID && c.ChildID == ch.ChildID);
						}
					}

				return _grandChild1;
			}
		}
		#endregion

		#region Inheritance Parent/Child Model

		private   List<InheritanceParentBase>? _inheritanceParent;
		protected List<InheritanceParentBase> InheritanceParent
		{
			get
			{
				if (_inheritanceParent == null)
				{
					using (new DisableLogging())
					using (new DisableBaseline("Default Database"))
					using (var db = new TestDataConnection())
						_inheritanceParent = db.InheritanceParent.ToList();
				}

				return _inheritanceParent;
			}
		}

		private   List<InheritanceChildBase>? _inheritanceChild;
		protected List<InheritanceChildBase> InheritanceChild
		{
			get
			{
				if (_inheritanceChild == null)
				{
					using (new DisableLogging())
					using (new DisableBaseline("Default Database"))
					using (var db = new TestDataConnection())
						_inheritanceChild = db.InheritanceChild.LoadWith(_ => _.Parent).ToList();
				}

				return _inheritanceChild;
			}
		}

		private   List<ParentInheritanceBase>?      _parentInheritance;
		protected IEnumerable<ParentInheritanceBase> ParentInheritance
		{
			get
			{
				_parentInheritance ??= Parent.Select(p =>
						p.Value1 == null ? new ParentInheritanceNull { ParentID = p.ParentID } :
						p.Value1.Value == 1 ? new ParentInheritance1 { ParentID = p.ParentID, Value1 = p.Value1.Value } :
						 (ParentInheritanceBase)new ParentInheritanceValue { ParentID = p.ParentID, Value1 = p.Value1.Value }
					).ToList();

				return _parentInheritance;
			}
		}

		private   List<ParentInheritanceValue>? _parentInheritanceValue;
		protected List<ParentInheritanceValue> ParentInheritanceValue => _parentInheritanceValue ??=
					ParentInheritance.OfType<ParentInheritanceValue>().ToList();

		private   List<ParentInheritance1>? _parentInheritance1;
		protected List<ParentInheritance1> ParentInheritance1 =>
			_parentInheritance1 ??=
				ParentInheritance.OfType<ParentInheritance1>().ToList();

		private   List<ParentInheritanceBase4>? _parentInheritance4;
		protected List<ParentInheritanceBase4> ParentInheritance4 =>
			_parentInheritance4 ??= Parent
					.Where(p => p.Value1.HasValue && (p.Value1.Value == 1 || p.Value1.Value == 2))
					.Select(p => p.Value1 == 1 ?
						(ParentInheritanceBase4)new ParentInheritance14 { ParentID = p.ParentID } :
												new ParentInheritance24 { ParentID = p.ParentID }
				).ToList();
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

		void InitPatientPerson()
		{
			if (_patient == null || _person == null)
			{
				using (new DisableLogging())
				using (new DisableBaseline("Default Database"))
				using (var db = new TestDataConnection())
				{
					var persons  = db.Person.ToList();
					var patients = db.Patient.ToList();

					foreach (var p in persons)
						p.Patient = patients.SingleOrDefault(ps => p.ID == ps.PersonID);

					foreach (var p in patients)
						p.Person = persons.Single(ps => ps.ID == p.PersonID);

					_patient = patients;
					_person = persons;
				}
			}
		}

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
