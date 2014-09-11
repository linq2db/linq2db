using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.SqlQuery;

namespace Tests.Model
{
	public class TestDataConnection : DataConnection, ITestDataContext
	{
		public TestDataConnection(string configString)
			: base(configString)
		{
		}

		public TestDataConnection()
			: base(ProviderName.SQLite)
		{
		}

		public ITable<Person>                 Person                 { get { return GetTable<Person>();                 } }
		public ITable<Patient>                Patient                { get { return GetTable<Patient>();                } }
		public ITable<Doctor>                 Doctor                 { get { return GetTable<Doctor>();                 } }
		public ITable<Parent>                 Parent                 { get { return GetTable<Parent>();                 } }
		public ITable<Parent1>                Parent1                { get { return GetTable<Parent1>();                } }
		public ITable<IParent>                Parent2                { get { return GetTable<IParent>();                } }
		public ITable<Parent4>                Parent4                { get { return GetTable<Parent4>();                } }
		public ITable<Parent5>                Parent5                { get { return GetTable<Parent5>();                } }
		public ITable<ParentInheritanceBase>  ParentInheritance      { get { return GetTable<ParentInheritanceBase>();  } }
		public ITable<ParentInheritanceBase2> ParentInheritance2     { get { return GetTable<ParentInheritanceBase2>(); } }
		public ITable<ParentInheritanceBase3> ParentInheritance3     { get { return GetTable<ParentInheritanceBase3>(); } }
		public ITable<ParentInheritanceBase4> ParentInheritance4     { get { return GetTable<ParentInheritanceBase4>(); } }
		public ITable<ParentInheritance1>     ParentInheritance1     { get { return GetTable<ParentInheritance1>();     } }
		public ITable<ParentInheritanceValue> ParentInheritanceValue { get { return GetTable<ParentInheritanceValue>(); } }
		public ITable<Child>                  Child                  { get { return GetTable<Child>();                  } }
		public ITable<GrandChild>             GrandChild             { get { return GetTable<GrandChild>();             } }
		public ITable<GrandChild1>            GrandChild1            { get { return GetTable<GrandChild1>();            } }
		public ITable<LinqDataTypes>          Types                  { get { return GetTable<LinqDataTypes>();          } }
		public ITable<LinqDataTypes2>         Types2                 { get { return GetTable<LinqDataTypes2>();         } }
		public ITable<TestIdentity>           TestIdentity           { get { return GetTable<TestIdentity>();           } }

		[Sql.TableFunction(Name="GetParentByID")]
		public ITable<Parent> GetParentByID(int? id)
		{
			return GetTable<Parent>(this, (MethodInfo)MethodBase.GetCurrentMethod(), id);
		}

		public string GetSqlText(SelectQuery query)
		{
			var provider  = ((IDataContext)this).CreateSqlProvider();
			var optimizer = ((IDataContext)this).GetSqlOptimizer  ();

			//provider.SqlQuery = sql;

			query = optimizer.Finalize(query);

			var cc = provider.CommandCount(query);
			var sb = new StringBuilder();

			var commands = new string[cc];

			for (var i = 0; i < cc; i++)
			{
				sb.Length = 0;

				provider.BuildSql(i, query, sb);
				commands[i] = sb.ToString();
			}

			return string.Join("\n\n", commands);
		}

		[ExpressionMethod("Expression9")]
		static public IQueryable<Parent> GetParent9(ITestDataContext db, Child ch)
		{
			throw new InvalidOperationException();
		}

		[ExpressionMethod("Expression9")]
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
	}
}
