using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tests
{
  class Program
  {
    static void Main()
    {
      var path = typeof(Tests.Create.CreateData).Assembly.Location;
    
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
