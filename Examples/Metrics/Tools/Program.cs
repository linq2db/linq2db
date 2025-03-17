using System;
using System.Linq;
using System.Text;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using LinqToDB.Metrics;
using LinqToDB.Tools.Activity;

#pragma warning disable CA1812

namespace ToolsExample
{
	static class Program
	{
		static readonly DataOptions _options = new DataOptions().UseSQLite("Data Source=Northwind.MS.sqlite", SQLiteProvider.Microsoft);

		static void Main()
		{
			// Setup call hierarchy metrics.
			//
			var hierarchyBuilder = new StringBuilder();

			ActivityService.AddFactory(activityID => new ActivityHierarchy(activityID, s => hierarchyBuilder.AppendLine(s)));

			// Setup statistics.
			//
			Configuration.TraceMaterializationActivity = true;
			ActivityService.AddFactory(ActivityStatistics.Factory);

			{
				using var db = new DataConnection(_options);

				db.CreateTable<Customer>(tableOptions:TableOptions.CheckExistence);

				var count = db.GetTable<Customer>().Count();

				Console.WriteLine($"Count is {count}");
			}

			// Display hierarchy metrics.
			//
			Console.WriteLine("LinqToDB call hierarchy:");
			Console.WriteLine();
			Console.WriteLine(hierarchyBuilder);

			// Display statistics.
			//
			Console.WriteLine("LinqToDB statistics:");
			Console.WriteLine();
			Console.WriteLine(ActivityStatistics.GetReport());
		}

		[Table(Name="Customers")]
		public sealed class Customer
		{
			[PrimaryKey]      public string CustomerID  = null!;
			[Column, NotNull] public string CompanyName = null!;
		}
	}
}
