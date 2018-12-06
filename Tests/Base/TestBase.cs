﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using System.ServiceModel;
using System.ServiceModel.Description;
#endif

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Mapping;

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using LinqToDB.ServiceModel;
#endif

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

//[assembly: Parallelizable]

namespace Tests
{
	using Model;
	using Tools;

	public class TestBase
	{
		static TestBase()
		{
			Console.WriteLine("Tests started in {0}...",
#if NETSTANDARD1_6
				System.IO.Directory.GetCurrentDirectory()
#else
				Environment.CurrentDirectory
#endif
				);

			Console.WriteLine("CLR Version: {0}...",
#if NETSTANDARD1_6
				System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
#else
				Environment.Version
#endif
				);

			var traceCount = 0;

			DataConnection.WriteTraceLine = (s1,s2) =>
			{
				if (traceCount < 10000)
				{
					Console.WriteLine("{0}: {1}", s2, s1);
					Debug.WriteLine(s1, s2);
				}

				if (traceCount++ > 10000)
					DataConnection.TurnTraceSwitchOn(TraceLevel.Error);
			};

//			Configuration.RetryPolicy.Factory = db => new Retry();

//			Configuration.AvoidSpecificDataProviderAPI = true;
			Configuration.Linq.TraceMapperExpression   = false;
//			Configuration.Linq.GenerateExpressionTest  = true;
			var assemblyPath = typeof(TestBase).AssemblyEx().GetPath();

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
			try
			{
				SqlServerTypes.Utilities.LoadNativeAssemblies(assemblyPath);
			}
			catch // this can fail during tests discovering with NUnitTestAdapter
			{ }
#endif

#if NETSTANDARD1_6
			System.IO.Directory.SetCurrentDirectory(assemblyPath);
#else
			Environment.CurrentDirectory = assemblyPath;
#endif

			var dataProvidersJsonFile     = GetFilePath(assemblyPath, @"DataProviders.json");
			var userDataProvidersJsonFile = GetFilePath(assemblyPath, @"UserDataProviders.json");

			var dataProvidersJson     = File.ReadAllText(dataProvidersJsonFile);
			var userDataProvidersJson =
				File.Exists(userDataProvidersJsonFile) ? File.ReadAllText(userDataProvidersJsonFile) : null;

#if NETSTANDARD1_6
			var configName = "CORE1";
#elif NETSTANDARD2_0
			var configName = "CORE2";
#else
			var configName = "NET45";
#endif

#if APPVEYOR
#warning "AppVeyor configuration detected."

			Console.WriteLine("AppVeyor configuration detected.");
			configName += ".AppVeyor";
#endif
#if TRAVIS
#warning "Travis configuration detected."

			Console.WriteLine("Travis configuration detected.");
			configName += ".Travis";
#endif
			var testSettings = SettingsReader.Deserialize(configName, dataProvidersJson, userDataProvidersJson);
			var databasePath = Path.GetFullPath(Path.Combine("Database"));
			var dataPath     = Path.Combine(databasePath, "Data");

			if (Directory.Exists(dataPath))
				Directory.Delete(dataPath, true);

			Directory.CreateDirectory(dataPath);

			foreach (var file in Directory.GetFiles(databasePath, "*.*"))
			{
				var destination = Path.Combine(dataPath, Path.GetFileName(file));
				Console.WriteLine("{0} => {1}", file, destination);
				File.Copy(file, destination, true);
			}

			UserProviders  = new HashSet<string>(testSettings.Providers ?? Array<string>.Empty, StringComparer.OrdinalIgnoreCase);
			SkipCategories = new HashSet<string>(testSettings.Skip      ?? Array<string>.Empty, StringComparer.OrdinalIgnoreCase);

			var logLevel   = testSettings.TraceLevel;
			var traceLevel = TraceLevel.Info;

			if (!string.IsNullOrEmpty(logLevel))
				if (!Enum.TryParse(logLevel, true, out traceLevel))
					traceLevel = TraceLevel.Info;

			if (!string.IsNullOrEmpty(testSettings.NoLinqService))
				BaseDataContextSourceAttribute.NoLinqService = ConvertTo<bool>.From(testSettings.NoLinqService);

			DataConnection.TurnTraceSwitchOn(traceLevel);

			Console.WriteLine("Connection strings:");

#if NETSTANDARD1_6 || NETSTANDARD2_0
			DataConnection.DefaultSettings            = TxtSettings.Instance;
			TxtSettings.Instance.DefaultConfiguration = "SQLiteMs";

			foreach (var provider in testSettings.Connections/*.Where(c => UserProviders.Contains(c.Key))*/)
			{
				if (string.IsNullOrWhiteSpace(provider.Value.ConnectionString))
					throw new InvalidOperationException("ConnectionString should be provided");

				Console.WriteLine($"\tName=\"{provider.Key}\", Provider=\"{provider.Value.Provider}\", ConnectionString=\"{provider.Value.ConnectionString}\"");

				TxtSettings.Instance.AddConnectionString(
					provider.Key, provider.Value.Provider ?? provider.Key, provider.Value.ConnectionString);
			}
#else
			foreach (var provider in testSettings.Connections)
			{
				Console.WriteLine($"\tName=\"{provider.Key}\", Provider=\"{provider.Value.Provider}\", ConnectionString=\"{provider.Value.ConnectionString}\"");

				DataConnection.AddOrSetConfiguration(
					provider.Key,
					provider.Value.ConnectionString,
					provider.Value.Provider ?? "");
			}
#endif

			Console.WriteLine("Providers:");

			foreach (var userProvider in UserProviders)
				Console.WriteLine($"\t{userProvider}");

			var defaultConfiguration = testSettings.DefaultConfiguration;

			if (!string.IsNullOrEmpty(defaultConfiguration))
			{
				DataConnection.DefaultConfiguration       = defaultConfiguration;
#if NETSTANDARD1_6 || NETSTANDARD2_0
				TxtSettings.Instance.DefaultConfiguration = defaultConfiguration;
#endif
			}

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
			LinqService.TypeResolver = str =>
			{
				switch (str)
				{
					case "Tests.Model.Gender" : return typeof(Gender);
					case "Tests.Model.Person" : return typeof(Person);
					default                   : return null;
				}
			};
#endif
		}

		protected static string GetFilePath(string basePath, string findFileName)
		{
			var fileName = Path.GetFullPath(Path.Combine(basePath, findFileName));

			while (!File.Exists(fileName))
			{
				Console.WriteLine($"File not found: {fileName}");

				basePath = Path.GetDirectoryName(basePath);

				if (basePath == null)
					return null;

				fileName = Path.GetFullPath(Path.Combine(basePath, findFileName));
			}

			Console.WriteLine($"Base path found: {fileName}");

			return fileName;
		}

#if !NETSTANDARD1_6 && !NETSTANDARD2_0 && !MONO
		const int IP = 22654;
		static bool _isHostOpen;
#endif

		static void OpenHost()
		{
#if !NETSTANDARD1_6 && !NETSTANDARD2_0 && !MONO
			if (_isHostOpen)
				return;

			_isHostOpen = true;

			var host = new ServiceHost(new LinqService { AllowUpdates = true }, new Uri("net.tcp://localhost:" + IP));

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
#endif
		}

		public class UserProviderInfo
		{
			public string Name;
			public string ConnectionString1;
			public string ProviderName;
		}

		public static readonly HashSet<string> UserProviders;
		public static readonly HashSet<string> SkipCategories;

		public static readonly List<string> Providers = new List<string>
		{
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
			ProviderName.Access,
			ProviderName.DB2,
			ProviderName.Informix,
			TestProvName.MariaDB,
			ProviderName.Sybase,
			ProviderName.SapHana,
			ProviderName.OracleNative,
			ProviderName.OracleManaged,
			ProviderName.SqlCe,
			ProviderName.SQLiteClassic,
#endif
#if !NETSTANDARD1_6
			ProviderName.SybaseManaged,
#endif
			ProviderName.Firebird,
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2012,
			ProviderName.SqlServer2014,
			ProviderName.SqlServer2000,
			ProviderName.SqlServer2005,
			ProviderName.PostgreSQL,
			ProviderName.PostgreSQL92,
			ProviderName.PostgreSQL93,
			ProviderName.PostgreSQL95,
			TestProvName.PostgreSQL10,
			TestProvName.PostgreSQL11,
			TestProvName.PostgreSQLLatest,
			ProviderName.MySql,
			TestProvName.SqlAzure,
			TestProvName.MySql57,
			ProviderName.SQLiteMS,
			TestProvName.Firebird3
		};

		protected ITestDataContext GetDataContext(string configuration, MappingSchema ms = null)
		{
			if (configuration.EndsWith(".LinqService"))
			{
#if !NETSTANDARD1_6 && !NETSTANDARD2_0 && !MONO
				OpenHost();

				var str = configuration.Substring(0, configuration.Length - ".LinqService".Length);
				var dx  = new TestServiceModelDataContext(IP) { Configuration = str };

				Debug.WriteLine(((IDataContext)dx).ContextID, "Provider ");

				if (ms != null)
					dx.MappingSchema = new MappingSchema(dx.MappingSchema, ms);

				return dx;
#else
				configuration = configuration.Substring(0, configuration.Length - ".LinqService".Length);
#endif
			}

			Debug.WriteLine(configuration, "Provider ");

			var res = new TestDataConnection(configuration);
			if (ms != null)
				res.AddMappingSchema(ms);
			return res;
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

		protected void TestPerson(int id, string firstName, IQueryable<IPerson> persons)
		{
			var person = persons.ToList().First(p => p.ID == id);

			Assert.AreEqual(id, person.ID);
			Assert.AreEqual(firstName, person.FirstName);
		}

		protected void TestJohn(IQueryable<IPerson> persons)
		{
			TestPerson(1, "John", persons);
		}

		private List<LinqDataTypes> _types;
		protected IEnumerable<LinqDataTypes>  Types
		{
			get
			{
				if (_types == null)
					using (new DisableLogging())
					using (var db = new TestDataConnection())
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
					using (new DisableLogging())
					using (var db = new TestDataConnection())
						_types2 = db.Types2.ToList();

				return _types2;
			}
		}

		protected internal const int MaxPersonID = 4;

		private          List<Person> _person;
		protected IEnumerable<Person>  Person
		{
			get
			{
				if (_person == null)
				{
					using (new DisableLogging())
					using (var db = new TestDataConnection())
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
					using (new DisableLogging())
					using (var db = new TestDataConnection())
						_patient = db.Patient.ToList();

					foreach (var p in _patient)
						p.Person = Person.Single(ps => ps.ID == p.PersonID);
				}

				return _patient;
			}
		}

		private   List<Doctor> _doctor;
		protected List<Doctor>  Doctor
		{
			get
			{
				if (_doctor == null)
				{
					using (new DisableLogging())
					using (var db = new TestDataConnection())
						_doctor = db.Doctor.ToList();
				}

				return _doctor;
			}
		}

		#region Parent/Child Model

		private          List<Parent> _parent;
		protected IEnumerable<Parent>  Parent
		{
			get
			{
				if (_parent == null)
					using (new DisableLogging())
					using (var db = new TestDataConnection())
					{
						db.Parent.Delete(c => c.ParentID >= 1000);
						_parent = db.Parent.ToList();
						db.Close();

						foreach (var p in _parent)
						{
							p.ParentTest    = p;
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
				return _parent4 ?? (_parent4 = Parent.Select(p => new Parent4 { ParentID = p.ParentID, Value1 = ConvertTo<TypeValue>.From(p.Value1) }).ToList());
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

		protected        List<Child> _child;
		protected IEnumerable<Child>  Child
		{
			get
			{
				if (_child == null)
					using (new DisableLogging())
					using (var db = new TestDataConnection())
					{
						db.Child.Delete(c => c.ParentID >= 1000);
						_child = db.Child.ToList();
						db.Close();

						foreach (var ch in _child)
						{
							ch.Parent        = Parent. Single(p => p.ParentID == ch.ParentID);
							ch.Parent1       = Parent1.Single(p => p.ParentID == ch.ParentID);
							ch.ParentID2     = new Parent3 { ParentID2 = ch.Parent.ParentID, Value1 = ch.Parent.Value1 };
							ch.GrandChildren = GrandChild.Where(c => c.ParentID == ch.ParentID && c.ChildID == ch.ChildID).ToList();
						}
					}

				foreach (var item in _child)
					yield return item;
			}
		}

		private          List<GrandChild> _grandChild;
		protected IEnumerable<GrandChild>  GrandChild
		{
			get
			{
				if (_grandChild == null)
					using (new DisableLogging())
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

		private          List<GrandChild1> _grandChild1;
		protected IEnumerable<GrandChild1>  GrandChild1
		{
			get
			{
				if (_grandChild1 == null)
					using (new DisableLogging())
					using (var db = new TestDataConnection())
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

		#region Inheritance Parent/Child Model

		private   List<InheritanceParentBase> _inheritanceParent;
		protected List<InheritanceParentBase>  InheritanceParent
		{
			get
			{
				if (_inheritanceParent == null)
				{
					using (new DisableLogging())
					using (var db = new TestDataConnection())
						_inheritanceParent = db.InheritanceParent.ToList();
				}

				return _inheritanceParent;
			}
		}

		private   List<InheritanceChildBase> _inheritanceChild;
		protected List<InheritanceChildBase>  InheritanceChild
		{
			get
			{
				if (_inheritanceChild == null)
				{
					using (new DisableLogging())
					using (var db = new TestDataConnection())
						_inheritanceChild = db.InheritanceChild.LoadWith(_ => _.Parent).ToList();
				}

				return _inheritanceChild;
			}
		}

		#endregion

		#region Northwind

		public TestBaseNorthwind GetNorthwindAsList(string context)
		{
			return new TestBaseNorthwind(context);
		}

		public class TestBaseNorthwind
		{
			private string _context;

			public TestBaseNorthwind(string context)
			{
				_context = context;
			}

			private List<Northwind.Category> _category;
			public  List<Northwind.Category>  Category
			{
				get
				{
					if (_category == null)
						using (new DisableLogging())
						using (var db = new NorthwindDB(_context))
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
						using (new DisableLogging())
						using (var db = new NorthwindDB(_context))
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
						using (new DisableLogging())
						using (var db = new NorthwindDB(_context))
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
						using (new DisableLogging())
						using (var db = new NorthwindDB(_context))
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
						using (new DisableLogging())
						using (var db = new NorthwindDB(_context))
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
						using (new DisableLogging())
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

			private IEnumerable<Northwind.Product> _product;
			public  IEnumerable<Northwind.Product>  Product
			{
				get
				{
					if (_product == null)
						using (new DisableLogging())
						using (var db = new NorthwindDB(_context))
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
						using (new DisableLogging())
						using (var db = new NorthwindDB(_context))
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
						using (new DisableLogging())
						using (var db = new NorthwindDB(_context))
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
						using (new DisableLogging())
						using (var db = new NorthwindDB(_context))
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
						using (new DisableLogging())
						using (var db = new NorthwindDB(_context))
							_territory = db.Territory.ToList();
					return _territory;
				}
			}
		}

		#endregion

		protected IEnumerable<LinqDataTypes2> AdjustExpectedData(ITestDataContext db, IEnumerable<LinqDataTypes2> data)
		{
			if (db.ProviderNeedsTimeFix(db.ContextID))
			{
				var adjusted = new List<LinqDataTypes2>();
				foreach (var record in data)
				{
					var copy = new LinqDataTypes2()
					{
						ID             = record.ID,
						MoneyValue     = record.MoneyValue,
						DateTimeValue  = record.DateTimeValue,
						DateTimeValue2 = record.DateTimeValue2,
						BoolValue      = record.BoolValue,
						GuidValue      = record.GuidValue,
						SmallIntValue  = record.SmallIntValue,
						IntValue       = record.IntValue,
						BigIntValue    = record.BigIntValue,
						StringValue    = record.StringValue
					};

					if (copy.DateTimeValue != null)
					{
						copy.DateTimeValue = copy.DateTimeValue.Value.AddMilliseconds(-copy.DateTimeValue.Value.Millisecond);
					}

					adjusted.Add(copy);
				}

				return adjusted;
			}

			return data;
		}

		protected void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> result)
		{
			AreEqual(t => t, expected, result, EqualityComparer<T>.Default);
		}

		protected void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> result, Func<IEnumerable<T>, IEnumerable<T>> sort)
		{
			AreEqual(t => t, expected, result, EqualityComparer<T>.Default, sort);
		}

		protected void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> result, IEqualityComparer<T> comparer)
		{
			AreEqual(t => t, expected, result, comparer);
		}

		protected void AreEqual<T>(IEnumerable<T> expected, IEnumerable<T> result, IEqualityComparer<T> comparer, Func<IEnumerable<T>, IEnumerable<T>> sort)
		{
			AreEqual(t => t, expected, result, comparer, sort);
		}

		protected void AreEqual<T>(Func<T,T> fixSelector, IEnumerable<T> expected, IEnumerable<T> result)
		{
			AreEqual(fixSelector, expected, result, EqualityComparer<T>.Default);
		}

		protected void AreEqual<T>(Func<T, T> fixSelector, IEnumerable<T> expected, IEnumerable<T> result, IEqualityComparer<T> comparer)
		{
			AreEqual<T>(fixSelector, expected, result, comparer, null);
		}

		protected void AreEqual<T>(
			Func<T,T> fixSelector,
			IEnumerable<T> expected,
			IEnumerable<T> result,
			IEqualityComparer<T> comparer,
			Func<IEnumerable<T>, IEnumerable<T>> sort)
		{
			var resultList   = result.  Select(fixSelector).ToList();
			var expectedList = expected.Select(fixSelector).ToList();

			if (sort != null)
			{
				resultList   = sort(resultList)  .ToList();
				expectedList = sort(expectedList).ToList();
			}

			Assert.AreNotEqual(0, expectedList.Count, "Expected list cannot be empty.");
			Assert.AreEqual(expectedList.Count, resultList.Count, "Expected and result lists are different. Length: ");

			var exceptExpectedList = resultList.  Except(expectedList, comparer).ToList();
			var exceptResultList   = expectedList.Except(resultList,   comparer).ToList();

			var exceptExpected = exceptExpectedList.Count;
			var exceptResult   = exceptResultList.  Count;
			var message        = new StringBuilder();

			if (exceptResult != 0 || exceptExpected != 0)
				for (var i = 0; i < resultList.Count; i++)
				{
					Debug.  WriteLine   ("{0} {1} --- {2}", comparer.Equals(expectedList[i], resultList[i]) ? " " : "-", expectedList[i], resultList[i]);
					message.AppendFormat("{0} {1} --- {2}", comparer.Equals(expectedList[i], resultList[i]) ? " " : "-", expectedList[i], resultList[i]);
					message.AppendLine  ();
				}

			Assert.AreEqual(0, exceptExpected, $"Expected Was{Environment.NewLine}{message}");
			Assert.AreEqual(0, exceptResult,   $"Expect Result{Environment.NewLine}{message}");
		}

		protected void AreEqual<T>(IEnumerable<IEnumerable<T>> expected, IEnumerable<IEnumerable<T>> result)
		{
			var resultList   = result.  ToList();
			var expectedList = expected.ToList();

			Assert.AreNotEqual(0, expectedList.Count);
			Assert.AreEqual(expectedList.Count, resultList.Count, "Expected and result lists are different. Length: ");

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

		protected List<LinqDataTypes> GetTypes(string context)
		{
			return DataCache<LinqDataTypes>.Get(context);
		}
	}

	static class DataCache<T>
		where T : class
	{
		static readonly Dictionary<string,List<T>> _dic = new Dictionary<string, List<T>>();
		public static List<T> Get(string context)
		{
			lock (_dic)
			{
				context = context.Replace(".LinqService", "");

				if (!_dic.TryGetValue(context, out var list))
				{
					using (new DisableLogging())
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

	public static class Helpers
	{
		public static string ToInvariantString<T>(this T data)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}", data)
				.Replace(',', '.').Trim(' ', '.', '0');
		}
	}

	public class AllowMultipleQuery : IDisposable
	{
		public AllowMultipleQuery()
		{
			Configuration.Linq.AllowMultipleQuery = true;
		}

		public void Dispose()
		{
			Configuration.Linq.AllowMultipleQuery = false;
		}
	}

	public class AvoidSpecificDataProviderAPI : IDisposable
	{
		private readonly bool _oldValue = Configuration.AvoidSpecificDataProviderAPI;

		public AvoidSpecificDataProviderAPI(bool value)
		{
			Configuration.AvoidSpecificDataProviderAPI = value;
		}

		public void Dispose()
		{
			Configuration.AvoidSpecificDataProviderAPI = _oldValue;
		}
	}

	public class GuardGrouping : IDisposable
	{
		public GuardGrouping()
		{
			Configuration.Linq.GuardGrouping = true;
		}

		public void Dispose()
		{
			Configuration.Linq.GuardGrouping = false;
		}
	}

	public class DisableLogging : IDisposable
	{
		private TraceSwitch _traceSwitch;

		public DisableLogging()
		{
			_traceSwitch = DataConnection.TraceSwitch;
			DataConnection.TurnTraceSwitchOn(TraceLevel.Off);
		}

		public void Dispose()
		{
			DataConnection.TraceSwitch = _traceSwitch;
		}
	}

	public class DisableQueryCache : IDisposable
	{
		public DisableQueryCache()
		{
			Configuration.Linq.DisableQueryCache = true;
		}

		public void Dispose()
		{
			Configuration.Linq.DisableQueryCache = false;
		}
	}

	public class WithoutJoinOptimization : IDisposable
	{
		public WithoutJoinOptimization()
		{
			Configuration.Linq.OptimizeJoins = false;
		}

		public void Dispose()
		{
			Configuration.Linq.OptimizeJoins = true;
		}
	}

	public class DeletePerson : IDisposable
	{
		readonly IDataContext _db;

		public DeletePerson(IDataContext db)
		{
			_db = db;
			Delete(_db);
		}

		public void Dispose()
		{
			Delete(_db);
		}

		readonly Func<IDataContext,int> Delete =
			CompiledQuery.Compile<IDataContext, int>(db => db.GetTable<Person>().Delete(_ => _.ID > TestBase.MaxPersonID));

	}

	public class WithoutComparasionNullCheck : IDisposable
	{
		public WithoutComparasionNullCheck()
		{
			Configuration.Linq.CompareNullsAsValues = false;
		}

		public void Dispose()
		{
			Configuration.Linq.CompareNullsAsValues = true;
			Query.ClearCaches();
		}
	}

	public abstract class DataSourcesBaseAttribute : DataAttribute, IParameterDataSource
	{
		public bool     IncludeLinqService { get; }
		public string[] Providers          { get; }

		protected DataSourcesBaseAttribute(bool includeLinqService, string[] providers)
		{
			IncludeLinqService = includeLinqService;
			Providers = providers;
		}

		public IEnumerable GetData(IParameterInfo parameter)
		{
			if (!IncludeLinqService)
				return GetProviders();
			var providers = GetProviders().ToArray();
			return providers.Concat(providers.Select(p => p + ".LinqService"));
		}

		protected abstract IEnumerable<string> GetProviders();
	}

	[AttributeUsage(AttributeTargets.Parameter)]
	public class DataSourcesAttribute : DataSourcesBaseAttribute
	{
		public DataSourcesAttribute(params string[] excludeProviders) : base(true, excludeProviders)
		{
		}

		public DataSourcesAttribute(bool includeLinqService, params string[] excludeProviders) : base(includeLinqService, excludeProviders)
		{
		}

		protected override IEnumerable<string> GetProviders()
		{
			return TestBase.UserProviders.Where(p => !Providers.Contains(p) && TestBase.Providers.Contains(p));
		}
	}

	[AttributeUsage(AttributeTargets.Parameter)]
	public class IncludeDataSourcesAttribute : DataSourcesBaseAttribute
	{
		public IncludeDataSourcesAttribute(params string[] includeProviders) : base(true, includeProviders)
		{
		}

		public IncludeDataSourcesAttribute(bool includeLinqService, params string[] includeProviders) : base(includeLinqService, includeProviders)
		{
		}

		protected override IEnumerable<string> GetProviders()
		{
			return Providers.Where(TestBase.UserProviders.Contains);
		}
	}

	public class SQLiteDataSourcesAttribute : IncludeDataSourcesAttribute
	{
		public SQLiteDataSourcesAttribute(bool includeLinqService = false) : base(includeLinqService, 
			ProviderName.SQLiteClassic, ProviderName.SQLite, ProviderName.SQLiteMS)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class SkipCategoryAttribute : NUnitAttribute, IApplyToTest
	{
		public SkipCategoryAttribute(string category)
		{
			Category = category;
		}

		public SkipCategoryAttribute(string category, string providerName)
		{
			Category     = category;
			ProviderName = providerName;
		}

		public string Category     { get; }
		public string ProviderName { get; }

		public void ApplyToTest(Test test)
		{
			if (test.RunState == RunState.NotRunnable || test.RunState == RunState.Explicit || ProviderName != null)
				return;

			if (TestBase.SkipCategories.Contains(Category))
			{
				test.RunState = RunState.Explicit;
				test.Properties.Set(PropertyNames.Category, Category);
				test.Properties.Set(PropertyNames.SkipReason, $"Skip category '{Category}'");
			}
		}
	}
}
