using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.SqlBuilder;

namespace Tests.Model
{
	public class TestDataConnection : DataConnection, ITestDataContext
	{
		public TestDataConnection(string configString)
			: base(configString)
		{
		}

		public TestDataConnection()
			: base(ProviderName.Access)
		{
		}

		public Table<Person>                 Person                 { get { return GetTable<Person>();                 } }
		public Table<Patient>                Patient                { get { return GetTable<Patient>();                } }
		public Table<Doctor>                 Doctor                 { get { return GetTable<Doctor>();                 } }
		public Table<Parent>                 Parent                 { get { return GetTable<Parent>();                 } }
		public Table<Parent1>                Parent1                { get { return GetTable<Parent1>();                } }
		public Table<IParent>                Parent2                { get { return GetTable<IParent>();                } }
		public Table<Parent4>                Parent4                { get { return GetTable<Parent4>();                } }
		public Table<Parent5>                Parent5                { get { return GetTable<Parent5>();                } }
		public Table<ParentInheritanceBase>  ParentInheritance      { get { return GetTable<ParentInheritanceBase>();  } }
		public Table<ParentInheritanceBase2> ParentInheritance2     { get { return GetTable<ParentInheritanceBase2>(); } }
		public Table<ParentInheritanceBase3> ParentInheritance3     { get { return GetTable<ParentInheritanceBase3>(); } }
		public Table<ParentInheritanceBase4> ParentInheritance4     { get { return GetTable<ParentInheritanceBase4>(); } }
		public Table<ParentInheritance1>     ParentInheritance1     { get { return GetTable<ParentInheritance1>();     } }
		public Table<ParentInheritanceValue> ParentInheritanceValue { get { return GetTable<ParentInheritanceValue>(); } }
		public Table<Child>                  Child                  { get { return GetTable<Child>();                  } }
		public Table<GrandChild>             GrandChild             { get { return GetTable<GrandChild>();             } }
		public Table<GrandChild1>            GrandChild1            { get { return GetTable<GrandChild1>();            } }
		public Table<LinqDataTypes>          Types                  { get { return GetTable<LinqDataTypes>();          } }
		public Table<LinqDataTypes2>         Types2                 { get { return GetTable<LinqDataTypes2>();         } }
		public Table<TestIdentity>           TestIdentity           { get { return GetTable<TestIdentity>();           } }

		[Sql.TableFunction(Name="GetParentByID")]
		public Table<Parent> GetParentByID(int? id)
		{
			return GetTable<Parent>(this, (MethodInfo)MethodBase.GetCurrentMethod(), id);
		}

		public string GetSqlText(SqlQuery sql)
		{
			var provider = ((IDataContext)this).CreateSqlProvider();

			//provider.SqlQuery = sql;

			sql = provider.Finalize(sql);

			var cc = provider.CommandCount(sql);
			var sb = new StringBuilder();

			var commands = new string[cc];

			for (var i = 0; i < cc; i++)
			{
				sb.Length = 0;

				provider.BuildSql(i, sql, sb, 0, 0, false);
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
