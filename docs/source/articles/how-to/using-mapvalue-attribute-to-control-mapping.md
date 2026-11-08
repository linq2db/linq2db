---
uid: mapvalue
---

# Using MapValueAttribute to control mapping with Linq To DB

One of the primary functions of [Linq To DB](https://github.com/linq2db) is mapping between a database and classes/properties in your data model. Linq2db does a great job here straight out of the box, but often it is desirable to tune this process.  The most frequent example of where you may need this is with enumerations.

Let's say you have a table called Issue. Each issue has a status, which can be one of the predefined values: Open, InProgress, Resolved, Closed. Let's assume we use a CHAR(1) field to keep a status value ('O' = Open, 'R' = Resolved, etc.).

If we [scaf](https://www.nuget.org/packages/linq2db.cli)[fold](https://www.nuget.org/packages/linq2db.t4models) our data model without any tuning, we may get something like this:

```cs
[Table(Schema="dbo", Name="Issue")]
public partial class Issue
{
    [PrimaryKey, Identity   ] public int    ID          { get; set; } // int
    [Column,     NotNull    ] public string Subject     { get; set; } // varchar(8000)
    [Column,        Nullable] public string Description { get; set; } // varchar(max)
    [Column,     NotNull    ] public char   Status      { get; set; } // char(1)
}
```

As you understand, it's not convenient to use char values when working with Status in source code. It would be nice to have the IssueStatus enumeration:

```cs
public enum IssueStatus
{
    Open,
    InProgress,
    Resolved,
    Closed
}
```

In order to replace the Status type with an enumeration and teach linq2db how to do the mapping, we need to complete two simple steps:

1. Use the `MapValue` attribute to explain to linq2db how to map between the enumeration and char values in the database table<
1. Change the `Status` property type from char to `IssueStatus`.

The first step is accomplished like this:

```cs
using LinqToDB.Mapping;
 
public enum IssueStatus
{
    [MapValue('O')] Open,
    [MapValue('I')] InProgress,
    [MapValue('R')] Resolved,
    [MapValue('C')] Closed
}
```

If you write your data model classes manually, change the `Status` property type from char to `IssueStatus`.

If you generate your data model with the help of a T4 template, add the following between loading server metadata and a call to `GenerateModel()`:

```cs
Tables["Issue"].Columns["Status"].Type = "IssueStatus";
```

Our data model class will look like this now:

```cs
[Table(Schema="dbo", Name="Issue")]
public partial class Issue
{
    [PrimaryKey, Identity   ] public int         ID          { get; set; } // int
    [Column,     NotNull    ] public string      Subject     { get; set; } // varchar(8000)
    [Column,        Nullable] public string      Description { get; set; } // varchar(max)
    [Column,     NotNull    ] public IssueStatus Status      { get; set; } // char(1)
}
```

Let's get all open issues:

```cs
using (var db = new DataModel())
{
  var openIssues = db.Issues.Where(i => i.Status == IssueStatus.Open).ToList();
}
```

This will generate the following query (for SQL Server):

```sql
SELECT
    [t1].[ID],
    [t1].[Subject],
    [t1].[Description],
    [t1].[Status]
FROM
    [dbo].[Issue] [t1]
WHERE
    [t1].[Status] = N'O'
```

Note that if you used the `int` datatype for the `Status` column instead of char, then you could declare your enumeration like this:

```cs
public enum IssueStatus
{
    Open       = 1,
    InProgress = 2,
    Resolved   = 3,
    Closed     = 4
}
```

and linq2db would do the mapping for you without the need for using the `MapValue` attribute (although integer values are less obvious than character codes when browsing data).

Sometimes we may need to map multiple values in a database table to the same value in the datamodel class. Just add multiple `MapValue` attributes (we'll use another enum for this example):

```cs
public enum Gender
{
    [MapValue(null)]
    Undefined,

    [MapValue("M", IsDefault = true)]
    [MapValue("Male")]
    Male,

    [MapValue("F", IsDefault = true)]
    [MapValue("Female")]
    Female
}

using (var db = new DataModel())
{
    db.People.Insert(() => new Person 
    {
        FirstName = "Herbert",
        LastName  = "Wells",
        Gender    = Gender.Male
    });
}
```

Generated SQL (for SQL Server):

```sql
INSERT INTO [dbo].[Person]
(
    [FirstName],
    [LastName],
    [Gender]
)
VALUES
(
    N'Herbert',
    N'Wells',
    N'M'
)
```

As you can see, `Gender.Male` has been mapped to 'M' (because it is marked with the IsDefault property set to true).

There may be a situation when you need to get values specified by the `MapValue` attribute. There are different ways to accomplish this. You can write an extension method and use reflection inside, you can use a very powerful `ConvertTo<T>` class from linq2db, or you can use `MappingSchema.Default.EnumToValue()` method (also from linq2db), etc. `ConvertTo<T>` can be used like this:

```cs
string gender = ConvertTo<string>.From(Gender.Male); // will return "M"
```

Often developers create a separate table to store possible values and use a foreign key to provide database integrity. If you need to be able to update the range of values without redeployment of source code, or if you see that table as a separate entity (with its own attributes), or you want to be able to understand the values without looking at the source code (e.g., that `Status = 1` means `Open`, in case you used an integer as underlying datatype), which may be useful for DBAs and BAs, creating a separate table may be the right approach. But if you don't need these features, then just setting CHECK constraints may be a simple and good solution. In our example, if we use SQL Server, we can set the following constraint:

```sql
ALTER TABLE 
    dbo.Issue
ADD CONSTRAINT 
    CK_IssueStatus CHECK (Status IN ('O', 'I', 'R', 'C'))
```
