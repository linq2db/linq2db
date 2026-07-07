LINQ to DB is a database LINQ provider that allows you to work with your database using an application object model.

In addition to basic LINQ query operations, LINQ to DB also introduces DML and DDL extensions such as Insert, Delete, Update, CreateTable, DropTable, etc. methods.

## Why LINQ to DB

Comparing to other database LINQ providers, LINQ to DB is not a typical ORM that supports entity services such as automatic object storage, caching, change tracking, object identity, etc. LINQ to DB explicitly translates your LINQ queries into SQL and maps the result to your application object model. Instead of hiding your database from you, LINQ to DB concentrates on high performance, generating clear, readable and human friendly SQL, and close to SQL programming model including DDL and DML operations. In fact, you can consider LINQ to DB as type-safe SQL embedded in C#, VB.NET, and any other .NET languages. LINQ to DB is a straightforward LINQ into SQL translator that avoids any magic, implicity, and unexpected surprises.

## Get started

The following example demonstrates how to start using LINQ to DB.

* Create a project.
* Add <a href='http://www.nuget.org/packages/linq2db/'>LINQ to DB NuGet</a> into your solution.
* Use the following code to test the library:

```c#
using System;
using System.Linq;

using LinqToDB.DataProvider.SqlServer;

namespace ConsoleApplication1
{
    public class Categories
    {
        public int    CategoryID;
        public string CategoryName;
        public string Description;
        public byte[] Picture;
    }

    class Program
    {
        const string ConnectionString =
            "Data Source=.;Database=Northwind;Integrated Security=SSPI";

        static void Main()
        {
            using (var db = SqlServerTools.CreateDataConnection(ConnectionString))
            {
                var q =
                    from c in db.GetTable<Categories>()
                    select c;

                foreach (var category in q)
                {
                    Console.WriteLine("ID : {0}, Name : {1}",
                        category.CategoryID,
                        category.CategoryName);
                }
            }
        }
    }
}
```

More advanced example (using app.config configuration and entity mapping attributes):

### App.config

```xml
<?xml version="1.0"?>
<configuration>
    <connectionStrings>
        <add name="Northwind" 
            connectionString="Data Source=.;Database=Northwind;Integrated Security=SSPI"
            providerName="System.Data.SqlClient" />
    </connectionStrings>
</configuration>
```

### Program.cs

```c#
using System;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.Mapping;

namespace ConsoleApplication1
{
    [Table("Categories")]
    public class Category
    {
        [PrimaryKey, Identity] public int    CategoryID;
        [Column, NotNull]      public string CategoryName;
        [Column,    Nullable]  public string Description;
        [Column,    Nullable]  public byte[] Picture;
    }

    class Program
    {
        static void Main()
        {
            using (var db = new DataConnection())
            {
                var q =
                    from c in db.GetTable<Category>()
                    select c;

                foreach (var category in q)
                {
                    Console.WriteLine("ID : {0}, Name : {1}",
                        category.CategoryID,
                        category.CategoryName);
                }
            }
        }
    }
}
```