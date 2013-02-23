using System;
using System.Linq;

using DataModel;

namespace T4Model.LinqToDB
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var dc = new NorthwindDB())
			{
				dc.Customers.ToList();
			}
		}
	}
}
