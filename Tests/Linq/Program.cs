using System;

namespace Tests
{
	class Program
	{
		static void Main()
		{
			SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);

			var path = typeof(Program).Assembly.Location;

			Console.WriteLine(path);

			var res = NUnit.ConsoleRunner.Runner.Main(new []{ path });

			if (res != 0)
			{
				Console.BackgroundColor = ConsoleColor.Red;
				Console.WriteLine("Error!");
				Console.ReadLine();
			}
		}
	}
}
