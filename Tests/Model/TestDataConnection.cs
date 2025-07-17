using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Tools.DataProvider.SqlServer.Schemas;

namespace Tests.Model
{
	public class TestDataConnection : DataConnection, ITestDataContext, ISystemSchemaData
	{
		//static int _counter;

		public TestDataConnection(DataOptions options) : base(options)
		{
		}

		public TestDataConnection(Func<DataOptions,DataOptions> optionsSetter) : base(optionsSetter(new DataOptions()))
		{
		}

		public TestDataConnection(string configString)
			: base(configString)
		{
//			if (configString == ProviderName.SqlServer2008 && ++_counter > 1000)
//				OnClosing += TestDataConnection_OnClosing;
		}

		public TestDataConnection()
		{
		}

//		static object _sync = new ();

//		[Table("AllTypes")]
//		class AllTypes
//		{
//			[Column("ID")] public int ID;
//		}

		void TestDataConnection_OnClosing(object sender, EventArgs e)
		{
//			lock (_sync)
//			using (var db = new DataConnection(ProviderName.SqlServer2008))
//			{
//				var n = db.GetTable<AllTypes>().Count();
//				if (n == 0)
//				{
//				}
//			}
		}

		public ITable<Person>                 Person                 => this.GetTable<Person>();
		public ITable<ComplexPerson>          ComplexPerson          => this.GetTable<ComplexPerson>();
		public ITable<Patient>                Patient                => this.GetTable<Patient>();
		public ITable<Doctor>                 Doctor                 => this.GetTable<Doctor>();
		public ITable<Parent>                 Parent                 => this.GetTable<Parent>();
		public ITable<Parent1>                Parent1                => this.GetTable<Parent1>();
		public ITable<IParent>                Parent2                => this.GetTable<IParent>();
		public ITable<Parent4>                Parent4                => this.GetTable<Parent4>();
		public ITable<Parent5>                Parent5                => this.GetTable<Parent5>();
		public ITable<ParentInheritanceBase>  ParentInheritance      => this.GetTable<ParentInheritanceBase>();
		public ITable<ParentInheritanceBase2> ParentInheritance2     => this.GetTable<ParentInheritanceBase2>();
		public ITable<ParentInheritanceBase3> ParentInheritance3     => this.GetTable<ParentInheritanceBase3>();
		public ITable<ParentInheritanceBase4> ParentInheritance4     => this.GetTable<ParentInheritanceBase4>();
		public ITable<ParentInheritance1>     ParentInheritance1     => this.GetTable<ParentInheritance1>();
		public ITable<ParentInheritanceValue> ParentInheritanceValue => this.GetTable<ParentInheritanceValue>();
		public ITable<Child>                  Child                  => this.GetTable<Child>();
		public ITable<GrandChild>             GrandChild             => this.GetTable<GrandChild>();
		public ITable<GrandChild1>            GrandChild1            => this.GetTable<GrandChild1>();
		public ITable<LinqDataTypes>          Types                  => this.GetTable<LinqDataTypes>();
		public ITable<LinqDataTypes2>         Types2                 => this.GetTable<LinqDataTypes2>();
		public ITable<TestIdentity>           TestIdentity           => this.GetTable<TestIdentity>();
		public ITable<InheritanceParentBase>  InheritanceParent      => this.GetTable<InheritanceParentBase>();
		public ITable<InheritanceChildBase>   InheritanceChild       => this.GetTable<InheritanceChildBase>();

		[Sql.TableFunction(Name="GetParentByID")]
		public ITable<Parent> GetParentByID(int? id)
		{
			var methodInfo = (typeof(TestDataConnection)).GetMethod("GetParentByID", new [] {typeof(int?)})!;

			return this.GetTable<Parent>(this, methodInfo, id);
		}

		[ExpressionMethod(nameof(Expression9))]
		public static IQueryable<Parent> GetParent9(ITestDataContext db, Child ch)
		{
			throw new InvalidOperationException();
		}

		[ExpressionMethod(nameof(Expression9))]
		public IQueryable<Parent> GetParent10(Child ch)
		{
			throw new InvalidOperationException();
		}

		static Expression<Func<ITestDataContext,Child,IQueryable<Parent>>> Expression9()
		{
			return (db, ch) =>
				from p in db.Parent
				where p.ParentID == (int)Math.Floor(ch.ChildID / 10.0)
				select p;
		}

		SystemSchemaModel? _system;
		public SystemSchemaModel System => _system ??= new SystemSchemaModel(this);
	}
}
