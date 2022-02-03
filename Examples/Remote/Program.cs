using System;
using System.Linq;
using System.Threading.Tasks;
using DataModels;
using LinqToDB;

namespace Client
{
	public class Program
	{
		static async Task Main(string[] args)
		{
			var identity = await InsertNewCategoryAsync();
			await UpdateNewCategoryAsync(identity);
			await ReadScalarAsync();
			ReadCollection();

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

				Console.WriteLine("Category updated: Identity = " + categoryId);
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

				var identity = (decimal) await db.InsertWithIdentityAsync(cat);

				Console.WriteLine("Category added: " + cat.CategoryName + ", Identity = " + identity);

				return identity;
			}
		}

		private static async Task ReadScalarAsync()
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

				var row = await q.FirstAsync();
				Console.WriteLine("Category read: " + row.Id + " : " + row.Name);
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

				Console.WriteLine("All categories:");

				foreach (var c in q)
					Console.WriteLine(c.CategoryName + " : " + (c.ProductName ?? "null"));
			}
		}

	}
}
