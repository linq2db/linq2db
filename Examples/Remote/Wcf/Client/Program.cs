using DataModels;
using LinqToDB;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Client
{
	public class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("---------------------------------------------------------------------");
			var identity = await InsertNewCategoryAsync();
			Console.WriteLine("---------------------------------------------------------------------");
			await UpdateNewCategoryAsync(identity);
			Console.WriteLine("---------------------------------------------------------------------");
			ReadScalar();
			Console.WriteLine("---------------------------------------------------------------------");
			ReadCollection();
			Console.WriteLine("---------------------------------------------------------------------");

			Console.ReadLine();
		}

		private static async Task UpdateNewCategoryAsync(decimal categoryId)
		{
			using (var db = new NorthwindDB())
			{
				await db.Categories
					.Where(c => c.CategoryID == categoryId)
					.Set(c => c.CategoryName, c => c.CategoryName + "!")
					.UpdateAsync();

				Console.WriteLine("Category Updated, Identity = " + categoryId);
			}
		}

		private static async Task<decimal> InsertNewCategoryAsync()
		{
			using (var db = new NorthwindDB())
			{
				var cat = new Category
				{
					CategoryName = "MyCat " + DateTime.Now.ToString("HH:mm:ss"),
					Description = "MyCat Description",
					Picture = null
				};

				var identity = (decimal)await db.InsertWithIdentityAsync(cat);

				Console.WriteLine("Category added: " + cat.CategoryName + ", Identity = " + identity);

				return identity;
			}
		}

		private static void ReadScalar()
		{
			using (var db = new NorthwindDB())
			{
				var q =
					from c in db.Categories
					orderby c.CategoryID descending
					select new
					{
						Id = c.CategoryID,
						Name = c.CategoryName
					};

				var row = q.First();
				Console.WriteLine(row.Id + " : " + row.Name);
			}
		}

		private static void ReadCollection()
		{
			using (var db = new NorthwindDB())
			{
				var q =
					from c in db.Categories
					from p in db.Products.Where(p => p.CategoryID == c.CategoryID).DefaultIfEmpty()
					orderby c.CategoryID descending
					select new
					{
						ProductName = p.ProductName,
						CategoryName = c.CategoryName
					};

				foreach (var c in q)
					Console.WriteLine(c.CategoryName + " : " + (c.ProductName ?? "null"));
			}
		}

	}
}
