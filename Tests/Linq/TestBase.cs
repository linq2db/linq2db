using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.ServiceModel;
using LinqToDB.SqlProvider;

using NUnit.Framework;

namespace Tests
{
	using Model;

	public class TestBase
	{
		static TestBase()
		{
			//Configuration.Linq.GenerateExpressionTest = true;

			var providerListFile =
				File.Exists(@"..\..\UserDataProviders.txt") ?
					@"..\..\UserDataProviders.txt" :
					@"..\..\DefaultDataProviders.txt";

			UserProviders.AddRange(
				File.ReadAllLines(providerListFile)
					.Select(s => s.Trim())
					.Where (s => s.Length > 0 && !s.StartsWith("--")));

			DbManager.TurnTraceSwitchOn();

			PostgreSQLSqlProvider.QuoteIdentifiers = true;

			var path = Path.GetDirectoryName(typeof(DbManager).Assembly.CodeBase.Replace("file:///", ""));

			foreach (var info in Providers)
			{
				try
				{
					Type type;

					if (info.Assembly == null)
					{
						type = typeof(DbManager).Assembly.GetType(info.Type, true);
					}
					else
					{
						var assembly = Assembly.LoadFile(Path.Combine(path, info.Assembly + ".dll"));

						type = assembly.GetType(info.Type, true);
					}

					DbManager.AddDataProvider(type);

					info.Loaded = true;
				}
				catch (Exception)
				{
					info.Loaded = false;
				}
			}

			LinqService.TypeResolver = str =>
			{
				switch (str)
				{
					case "Tests.Model.Gender" : return typeof(Gender);
					case "Tests.Model.Person" : return typeof(Person);
					default                   : return null;
				}
			};
		}

		const  int StartIP = 22654;
		static int _lastIP = StartIP;

		static int GetIP(string config)
		{
			int ip;

			if (_ips.TryGetValue(config, out ip))
				return ip;

			_lastIP++;

			var host = new ServiceHost(new LinqService(config) { AllowUpdates = true }, new Uri("net.tcp://localhost:" + _lastIP));

			host.Description.Behaviors.Add(new ServiceMetadataBehavior());
			host.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
			host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexTcpBinding(), "mex");
			host.AddServiceEndpoint(
				typeof(ILinqService),
				new NetTcpBinding(SecurityMode.None)
				{
					MaxReceivedMessageSize = 10000000,
					MaxBufferPoolSize      = 10000000,
					MaxBufferSize          = 10000000,
					CloseTimeout           = new TimeSpan(00, 01, 00),
					OpenTimeout            = new TimeSpan(00, 01, 00),
					ReceiveTimeout         = new TimeSpan(00, 10, 00),
					SendTimeout            = new TimeSpan(00, 10, 00),
				},
				"LinqOverWCF");

			host.Open();

			_ips.Add(config, _lastIP);

			return _lastIP;
		}

		public class ProviderInfo
		{
			public ProviderInfo(string name, string assembly, string type)
			{
				Name     = name;
				Assembly = assembly;
				Type     = type;
			}

			public readonly string Name;
			public readonly string Assembly;
			public readonly string Type;
			public          bool   Loaded;
			public          int    IP;
			public          bool   Skip;
		}

		public static readonly List<string>       UserProviders = new List<string>();
		public static readonly List<ProviderInfo> Providers = new List<ProviderInfo>
		{
			new ProviderInfo(ProviderName.SqlServer2008, null,                 "LinqToDB.DataProvider.Sql2008DataProvider"),
			new ProviderInfo(ProviderName.SqlCe,         "linq2db.SqlCe",      "LinqToDB.DataProvider.SqlCeDataProviderOld"),
			new ProviderInfo(ProviderName.SQLite,        "linq2db.SQLite",     "LinqToDB.DataProvider.SQLiteDataProviderOld"),
			new ProviderInfo(ProviderName.Access,        null,                 "LinqToDB.DataProvider.AccessDataProviderOld"),
			new ProviderInfo(ProviderName.SqlServer2005, null,                 "LinqToDB.DataProvider.SqlDataProvider"),
			new ProviderInfo(ProviderName.DB2,           "linq2db.DB2",        "LinqToDB.DataProvider.DB2DataProvider"),
			new ProviderInfo(ProviderName.Informix,      "linq2db.Informix",   "LinqToDB.DataProvider.InformixDataProvider"),
			new ProviderInfo(ProviderName.Firebird,      "linq2db.Firebird",   "LinqToDB.DataProvider.FirebirdDataProvider"),
			new ProviderInfo(ProviderName.Oracle,        "linq2db.Oracle",     "LinqToDB.DataProvider.OracleDataProviderOld"),
			new ProviderInfo(ProviderName.PostgreSQL,    "linq2db.PostgreSQL", "LinqToDB.DataProvider.PostgreSQLDataProvider"),
			new ProviderInfo(ProviderName.MySql,         "linq2db.MySql",      "LinqToDB.DataProvider.MySqlDataProvider"),
			new ProviderInfo(ProviderName.Sybase,        "linq2db.Sybase",     "LinqToDB.DataProvider.SybaseDataProviderOld"),
		};

		[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
		public class DataContextsAttribute : ValuesAttribute
		{
			public DataContextsAttribute(params string[] except)
			{
				Except = except;
			}

			const bool IncludeLinqService = true;

			public string[] Except             { get; set; }
			public string[] Include            { get; set; }
			public bool     ExcludeLinqService { get; set; }

			public override IEnumerable GetData(ParameterInfo parameter)
			{
				if (Include != null)
				{
					var list = Include.Intersect(
						IncludeLinqService ? 
							UserProviders.Concat(UserProviders.Select(p => p + ".LinqService")) :
							UserProviders).
						ToArray();

					return list;
				}

				var providers = new List<string>();

				foreach (var info in Providers)
				{
					if (info.Skip && Include == null)
						continue;

					if (Except != null && Except.Contains(info.Name))
						continue;

					if (!UserProviders.Contains(info.Name))
						continue;

					providers.Add(info.Name);

					if (IncludeLinqService && !ExcludeLinqService)
					{
						providers.Add(info.Name + ".LinqService");
					}
				}

				return providers.ToArray();
			}
		}

		public class IncludeDataContextsAttribute : DataContextsAttribute
		{
			public IncludeDataContextsAttribute(params string[] include)
			{
				Include = include;
			}
		}

		static readonly Dictionary<string,int> _ips = new Dictionary<string,int>();

		protected ITestDataContext GetDataContext(string configuration)
		{
			if (configuration.EndsWith(".LinqService"))
			{
				var str = configuration.Substring(0, configuration.Length - ".LinqService".Length);
				var ip  = GetIP(str);
				var dx  = new TestServiceModelDataContext(ip);

				Debug.WriteLine(((IDataContext)dx).ContextID, "Provider ");

				return dx;
			}

			Debug.WriteLine(configuration, "Provider ");

			return new TestDbManager(configuration);
		}

		protected void TestOnePerson(int id, string firstName, IQueryable<Person> persons)
		{
			var list = persons.ToList();

			Assert.AreEqual(1, list.Count);

			var person = list[0];

			Assert.AreEqual(id, person.ID);
			Assert.AreEqual(firstName, person.FirstName);
		}

		protected void TestOneJohn(IQueryable<Person> persons)
		{
			TestOnePerson(1, "John", persons);
		}

		protected void TestPerson(int id, string firstName, IQueryable<Person> persons)
		{
			var person = persons.ToList().Where(p => p.ID == id).First();

			Assert.AreEqual(id, person.ID);
			Assert.AreEqual(firstName, person.FirstName);
		}

		protected void TestJohn(IQueryable<Person> persons)
		{
			TestPerson(1, "John", persons);
		}

		private List<LinqDataTypes> _types;
		protected IEnumerable<LinqDataTypes>  Types
		{
			get
			{
				if (_types == null)
					using (var db = new TestDbManager())
						_types = db.Types.ToList();

				return _types;
			}
		}

		private   List<LinqDataTypes2> _types2;
		protected List<LinqDataTypes2>  Types2
		{
			get
			{
				if (_types2 == null)
					using (var db = new TestDbManager())
						_types2 = db.Types2.ToList();

				return _types2;
			}
		}

		private          List<Person> _person;
		protected IEnumerable<Person>  Person
		{
			get
			{
				if (_person == null)
				{
					using (var db = new TestDbManager())
						_person = db.Person.ToList();

					foreach (var p in _person)
						p.Patient = Patient.SingleOrDefault(ps => p.ID == ps.PersonID);
				}

				return _person;
			}
		}

		private   List<Patient> _patient;
		protected List<Patient>  Patient
		{
			get
			{
				if (_patient == null)
				{
					using (var db = new TestDbManager())
						_patient = db.Patient.ToList();

					foreach (var p in _patient)
						p.Person = Person.Single(ps => ps.ID == p.PersonID);
				}

				return _patient;
			}
		}

		#region Parent/Child Model

		private          List<Parent> _parent;
		protected IEnumerable<Parent>  Parent
		{
			get
			{
				if (_parent == null)
					using (var db = new TestDbManager())
					{
						_parent = db.Parent.ToList();
						db.Close();

						foreach (var p in _parent)
						{
							p.Children      = Child.     Where(c => c.ParentID == p.ParentID).ToList();
							p.GrandChildren = GrandChild.Where(c => c.ParentID == p.ParentID).ToList();
							p.Types         = Types.FirstOrDefault(t => t.ID == p.ParentID);
						}
					}

				return _parent;
			}
		}

		private          List<Parent1> _parent1;
		protected IEnumerable<Parent1>  Parent1
		{
			get
			{
				if (_parent1 == null)
					_parent1 = Parent.Select(p => new Parent1 { ParentID = p.ParentID, Value1 = p.Value1 }).ToList();

				return _parent1;
			}
		}

		private   List<Parent4> _parent4;
		protected List<Parent4>  Parent4
		{
			get
			{
				return _parent4 ?? (_parent4 = Parent.Select(p => new Parent4 { ParentID = p.ParentID, Value1 = Map.DefaultSchema.MapValueToEnum<TypeValue>(p.Value1) }).ToList());
			}
		}

		private   List<Parent5> _parent5;
		protected List<Parent5>  Parent5
		{
			get
			{
				if (_parent5 == null)
				{
					_parent5 = Parent.Select(p => new Parent5 { ParentID = p.ParentID, Value1 = p.Value1}).ToList();

					foreach (var p in _parent5)
						p.Children = _parent5.Where(c => c.Value1 == p.ParentID).ToList();
				}

				return _parent5;
			}
		}

		private          List<ParentInheritanceBase> _parentInheritance;
		protected IEnumerable<ParentInheritanceBase>  ParentInheritance
		{
			get
			{
				if (_parentInheritance == null)
					_parentInheritance = Parent.Select(p =>
						p.Value1       == null ? new ParentInheritanceNull  { ParentID = p.ParentID } :
						p.Value1.Value == 1    ? new ParentInheritance1     { ParentID = p.ParentID, Value1 = p.Value1.Value } :
						 (ParentInheritanceBase) new ParentInheritanceValue { ParentID = p.ParentID, Value1 = p.Value1.Value }
					).ToList();

				return _parentInheritance;
			}
		}

		private   List<ParentInheritanceValue> _parentInheritanceValue;
		protected List<ParentInheritanceValue>  ParentInheritanceValue
		{
			get
			{
				return _parentInheritanceValue ?? (_parentInheritanceValue =
					ParentInheritance.Where(p => p is ParentInheritanceValue).Cast<ParentInheritanceValue>().ToList());
			}
		}

		private   List<ParentInheritance1> _parentInheritance1;
		protected List<ParentInheritance1>  ParentInheritance1
		{
			get
			{
				return _parentInheritance1 ?? (_parentInheritance1 =
					ParentInheritance.Where(p => p is ParentInheritance1).Cast<ParentInheritance1>().ToList());
			}
		}

		private   List<ParentInheritanceBase4> _parentInheritance4;
		protected List<ParentInheritanceBase4>  ParentInheritance4
		{
			get
			{
				return _parentInheritance4 ?? (_parentInheritance4 = Parent
					.Where(p => p.Value1.HasValue && (new[] { 1, 2 }.Contains(p.Value1.Value)))
					.Select(p => p.Value1 == 1 ?
						(ParentInheritanceBase4)new ParentInheritance14 { ParentID = p.ParentID } :
						(ParentInheritanceBase4)new ParentInheritance24 { ParentID = p.ParentID }
				).ToList());
			}
		}

		private          List<Child> _child;
		protected IEnumerable<Child>  Child
		{
			get
			{
				if (_child == null)
					using (var db = new TestDbManager())
					{
						_child = db.Child.ToList();
						db.Clone();

						foreach (var ch in _child)
						{
							ch.Parent        = Parent. Single(p => p.ParentID == ch.ParentID);
							ch.Parent1       = Parent1.Single(p => p.ParentID == ch.ParentID);
							ch.ParentID2     = new Parent3 { ParentID2 = ch.Parent.ParentID, Value1 = ch.Parent.Value1 };
							ch.GrandChildren = GrandChild.Where(c => c.ParentID == ch.ParentID && c.ChildID == ch.ChildID).ToList();
						}
					}

				return _child;
			}
		}

		private          List<GrandChild> _grandChild;
		protected IEnumerable<GrandChild>  GrandChild
		{
			get
			{
				if (_grandChild == null)
					using (var db = new TestDbManager())
					{
						_grandChild = db.GrandChild.ToList();
						db.Close();

						foreach (var ch in _grandChild)
							ch.Child = Child.Single(c => c.ParentID == ch.ParentID && c.ChildID == ch.ChildID);
					}

				return _grandChild;
			}
		}

		private          List<GrandChild1> _grandChild1;
		protected IEnumerable<GrandChild1>  GrandChild1
		{
			get
			{
				if (_grandChild1 == null)
					using (var db = new TestDbManager())
					{
						_grandChild1 = db.GrandChild1.ToList();

						foreach (var ch in _grandChild1)
						{
							ch.Parent = Parent1.Single(p => p.ParentID == ch.ParentID);
							ch.Child  = Child.  Single(c => c.ParentID == ch.ParentID && c.ChildID == ch.ChildID);
						}
					}

				return _grandChild1;
			}
		}

		#endregion

		#region Northwind

		private List<Northwind.Category> _category;
		public  List<Northwind.Category>  Category
		{
			get
			{
				if (_category == null)
					using (var db = new NorthwindDB())
						_category = db.Category.ToList();
				return _category;
			}
		}

		private List<Northwind.Customer> _customer;
		public  List<Northwind.Customer>  Customer
		{
			get
			{
				if (_customer == null)
				{
					using (var db = new NorthwindDB())
						_customer = db.Customer.ToList();

					foreach (var c in _customer)
						c.Orders = (from o in Order where o.CustomerID == c.CustomerID select o).ToList();
				}

				return _customer;
			}
		}

		private List<Northwind.Employee> _employee;
		public  List<Northwind.Employee>  Employee
		{
			get
			{
				if (_employee == null)
				{
					using (var db = new NorthwindDB())
					{
						_employee = db.Employee.ToList();

						foreach (var employee in _employee)
						{
							employee.Employees         = (from e in _employee where e.ReportsTo  == employee.EmployeeID select e).ToList();
							employee.ReportsToEmployee = (from e in _employee where e.EmployeeID == employee.ReportsTo  select e).SingleOrDefault();
						}
					}
				}

				return _employee;
			}
		}

		private List<Northwind.EmployeeTerritory> _employeeTerritory;
		public  List<Northwind.EmployeeTerritory>  EmployeeTerritory
		{
			get
			{
				if (_employeeTerritory == null)
					using (var db = new NorthwindDB())
						_employeeTerritory = db.EmployeeTerritory.ToList();
				return _employeeTerritory;
			}
		}

		private List<Northwind.OrderDetail> _orderDetail;
		public  List<Northwind.OrderDetail>  OrderDetail
		{
			get
			{
				if (_orderDetail == null)
					using (var db = new NorthwindDB())
						_orderDetail = db.OrderDetail.ToList();
				return _orderDetail;
			}
		}

		private List<Northwind.Order> _order;
		public  List<Northwind.Order>  Order
		{
			get
			{
				if (_order == null)
				{
					using (var db = new NorthwindDB())
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

		private IEnumerable<Northwind.Product> _product;
		public  IEnumerable<Northwind.Product>  Product
		{
			get
			{
				if (_product == null)
					using (var db = new NorthwindDB())
						_product = db.Product.ToList();

				foreach (var product in _product)
					yield return product;
			}
		}

		private List<Northwind.ActiveProduct> _activeProduct;
		public  List<Northwind.ActiveProduct>  ActiveProduct
		{
			get { return _activeProduct ?? (_activeProduct = Product.OfType<Northwind.ActiveProduct>().ToList()); }
		}

		public  IEnumerable<Northwind.DiscontinuedProduct>  DiscontinuedProduct
		{
			get { return Product.OfType<Northwind.DiscontinuedProduct>(); }
		}

		private List<Northwind.Region> _region;
		public  List<Northwind.Region>  Region
		{
			get
			{
				if (_region == null)
					using (var db = new NorthwindDB())
						_region = db.Region.ToList();
				return _region;
			}
		}

		private List<Northwind.Shipper> _shipper;
		public  List<Northwind.Shipper>  Shipper
		{
			get
			{
				if (_shipper == null)
					using (var db = new NorthwindDB())
						_shipper = db.Shipper.ToList();
				return _shipper;
			}
		}

		private List<Northwind.Supplier> _supplier;
		public  List<Northwind.Supplier>  Supplier
		{
			get
			{
				if (_supplier == null)
					using (var db = new NorthwindDB())
						_supplier = db.Supplier.ToList();
				return _supplier;
			}
		}

		private List<Northwind.Territory> _territory;
		public  List<Northwind.Territory>  Territory
		{
			get
			{
				if (_territory == null)
					using (var db = new NorthwindDB())
						_territory = db.Territory.ToList();
				return _territory;
			}
		}

		#endregion

		protected void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> result)
		{
			var resultList   = result.  ToList();
			var expectedList = expected.ToList();

			Assert.AreNotEqual(0, expectedList.Count);
			Assert.AreEqual(expectedList.Count, resultList.Count, "Expected and result lists are different. Lenght: ");

			var exceptExpectedList = resultList.  Except(expectedList).ToList();
			var exceptResultList   = expectedList.Except(resultList).  ToList();

			var exceptExpected = exceptExpectedList.Count;
			var exceptResult   = exceptResultList.  Count;

			if (exceptResult != 0 || exceptExpected != 0)
				for (var i = 0; i < resultList.Count; i++)
					Debug.WriteLine("{0} {1} --- {2}", Equals(expectedList[i], resultList[i]) ? " " : "-", expectedList[i], resultList[i]);

			Assert.AreEqual(0, exceptExpected);
			Assert.AreEqual(0, exceptResult);
		}

		protected void AreEqual<T>(IEnumerable<IEnumerable<T>> expected, IEnumerable<IEnumerable<T>> result)
		{
			var resultList   = result.  ToList();
			var expectedList = expected.ToList();

			Assert.AreNotEqual(0, expectedList.Count);
			Assert.AreEqual(expectedList.Count, resultList.Count, "Expected and result lists are different. Lenght: ");

			for (var i = 0; i < resultList.Count; i++)
			{
				var elist = expectedList[i].ToList();
				var rlist = resultList  [i].ToList();

				if (elist.Count > 0 || rlist.Count > 0)
					AreEqual(elist, rlist);
			}
		}

		protected void AreSame<T>(IEnumerable<T> expected, IEnumerable<T> result)
		{
			var resultList   = result.  ToList();
			var expectedList = expected.ToList();

			Assert.AreNotEqual(0, expectedList.Count);
			Assert.AreEqual(expectedList.Count, resultList.Count);

			var b = expectedList.SequenceEqual(resultList);

			if (!b)
				for (var i = 0; i < resultList.Count; i++)
					Debug.WriteLine("{0} {1} --- {2}", Equals(expectedList[i], resultList[i]) ? " " : "-", expectedList[i], resultList[i]);

			Assert.IsTrue(b);
		}

		protected void CompareSql(string result, string expected)
		{
			var ss = expected.Trim('\r', '\n').Split('\n');

			while (ss.All(_ => _.Length > 0 && _[0] == '\t'))
				for (var i = 0; i < ss.Length; i++)
					ss[i] = ss[i].Substring(1);

			Assert.AreEqual(string.Join("\n", ss), result.Trim('\r', '\n'));
		}
	}
}
