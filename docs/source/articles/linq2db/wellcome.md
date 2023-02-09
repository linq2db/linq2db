## Welcome
### LINQ To DB
**LINQ to DB** is the fastest LINQ database access library offering a superficial, light, quick, and type-safe layer between your POCO objects and your database.

Architecturally it is one step above micro-ORMs like Dapper, Massive, or PetaPoco, in that you work with LINQ expressions, not magic strings while maintaining a thin abstraction layer between your code and the database. Your **queries are checked by the C# compiler** and allow for easy refactoring.

However, **it is not as heavy as LINQ to SQL or Entity Framework**. There is no change-tracking, so you have to manage that by yourself, but on the positive side, you get more control and faster access to your data.

In other words, **LINQ to DB** is type-safe SQL.
### The model
With **LINQ to DB**, data access is performed using a model. A model is made up of entity classes and a context object that represents a session with the database. The context object allows querying and saving data. For more information, see Creating a Model.

**LINQ to DB** supports the following model development approaches:
- Generate a model from an existing database.
- Generate objects from the database and query them.
- Insert the objects instead of copying them.

### Querying
Instances of your entity classes are retrieved from the database using Language Integrated Query (LINQ). For more information, see Querying Data.
**C#**
```csharp
  using (var db = new BloggingContext())
  {
         var blogs = db.Blogs
               .Where(b => b.Rating > 3)
               .OrderBy(b => b.Url)
               .ToList();
        }`
```
### Saving data
Data is created, deleted, and modified in the database using instances of your entity classes. See Saving Data to learn more.