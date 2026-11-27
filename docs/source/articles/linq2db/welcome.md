# LINQ To DB
**LINQ to DB** is the fastest LINQ database access library offering a superficial, light, quick, and type-safe layer between your POCO objects and your database.

Architecturally it is one step above micro-ORMs like Dapper, Massive, or PetaPoco, in that you work with LINQ expressions, not magic strings while maintaining a thin abstraction layer between your code and the database. Your queries are checked by the C# compiler and allow for easy refactoring.

However, it is not as heavy as **LINQ to SQL** or **Entity Framework**. There is no change-tracking, so you have to manage that by yourself, but on the positive side, you get more control and faster access to your data.

In other words, **LINQ to DB** is type-safe SQL.

# The model
With **LINQ to DB**, data access is performed using a model. A model is made up of entity classes and a context object that represents a session with the database. The context object allows querying and saving data. For more information, see Creating a Model.

**LINQ to DB** supports the following model development approaches:
- Generate a model from an existing database.
- Hand code a model to match the database.

**C#**
```csharp
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Intro;

public class BloggingContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            @"Server=(localdb)\mssqllocaldb;Database=Blogging;Trusted_Connection=True");
    }
}

public class Blog
{
    public int BlogId { get; set; }
    public string Url { get; set; }
    public int Rating { get; set; }
    public List<Post> Posts { get; set; }
}

public class Post
{
    public int PostId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }

    public int BlogId { get; set; }
    public Blog Blog { get; set; }
}
```
# Querying
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
# Saving data
Data is created, deleted, and modified in the database using instances of your entity classes. See Saving Data to learn more.
```csharp
using (var db = new BloggingContext())
{
    var blog = new Blog { Url = "http://sample.com" };
    db.Blogs.Add(blog);
    db.SaveChanges();
}
```