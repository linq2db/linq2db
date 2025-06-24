using System;
using System.Linq;
using System.Runtime.InteropServices;

using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;

// Define a model
public class Person
{
	public int    Id   { get; set; }
	public string Name { get; set; } = null!;
}

// Define a database context using SQLite in-memory database
public class MyDatabase : DataConnection
{
	public ITable<Person> People => this.GetTable<Person>();
}

class Program
{
	static void Main()
	{
		Console.WriteLine($".NET version          : {Environment.Version}");
		Console.WriteLine($".NET Framework version: {RuntimeInformation.FrameworkDescription}");
		Console.WriteLine();

		DataConnection.DefaultSettings = LinqToDBSection.Instance;

		using var db = new MyDatabase();

		// Create table on startup
		//
		db.CreateTable<Person>();

		// Create a new person
		//
		db.Insert(new Person { Id = 1, Name = "John Doe" });

		// Retrieve and print all people
		//
		var people = db.People.ToList();

		foreach (var person in people)
		{
			Console.WriteLine($"ID: {person.Id}, Name: {person.Name}");
		}
	}
}
