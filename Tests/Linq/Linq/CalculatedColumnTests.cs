using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	public class CalculatedColumnTests : TestBase
	{
		[Table(Name="Person")]
		public class PersonCalculated
		{
			[Column, PrimaryKey,  Identity] public int    PersonID   { get; set; } // INTEGER
			[Column, NotNull              ] public string FirstName  => throw new NotImplementedException();
			[Column, NotNull              ] public string LastName   => throw new NotImplementedException();
			[Column,    Nullable          ] public string MiddleName { get; set; } // VARCHAR(50)
			[Column, NotNull              ] public char   Gender     { get; set; } // CHARACTER(1)

			[ExpressionMethod(nameof(GetFullNameExpr), IsColumn = true)]
			public string FullName { get; set; }

			public static Expression<Func<PersonCalculated, string>> GetFullNameExpr()
			{
				return p => p.FirstName + ", " + p.LastName;
			}
		}

		[Test, DataContextSource]
		public void Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				//var zz = db.Person.ToArray();
				var q = db.GetTable<PersonCalculated>().Where(i => i.FirstName != "");

				var l = q.ToList();
				var str = q.ToString();
			}
		}
		
	}
}
