using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class MultipleInheritanceLevel : TestBase
	{
		[Table("Vehicle", Schema = "DoubleInheritance")]
		[InheritanceMapping(Code = VehicleType.Bicycle, Type = typeof(Bicycle))]
		[InheritanceMapping(Code = VehicleType.Automobile, Type = typeof(Automobile))]
		public abstract class Vehicle
		{
			[Identity]
			public int Id { get; set; }

			[Column(IsDiscriminator = true)]
			public virtual VehicleType Type { get; set; }
		}

		public class Bicycle : Vehicle
		{
			[Column]
			public override VehicleType Type => VehicleType.Bicycle;

			[Column]
			public BicycleType BicycleType { get; set; }
		}

		[InheritanceMapping(Code = AutomobileType.Lorry, Type = typeof(Lorry))]
		[InheritanceMapping(Code = AutomobileType.Car, Type = typeof(Car))]
		public abstract class Automobile : Vehicle
		{
			[Column]
			public override VehicleType Type => VehicleType.Automobile;

			[Column(IsDiscriminator = true)]
			public virtual AutomobileType AutomobileType { get; set; }
		}

		public class Lorry : Automobile
		{
			[Column]
			public override AutomobileType AutomobileType => AutomobileType.Lorry;

			[Column]
			public double CargoCapacity { get; set; }
		}

		public class Car : Automobile
		{
			[Column]
			public override AutomobileType AutomobileType => AutomobileType.Car;

			[Column]
			public int MaxOccupancy { get; set; }
		}

		public enum VehicleType
		{
			Bicycle,
			Automobile,
		}

		public enum AutomobileType
		{
			Lorry,
			Car,
		}

		public enum BicycleType
		{
			Standard,
			Recumbent,
			Cargo,
			Tandem,
		}

		/*
		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<Vehicle>())
			{
				var list = new List<Vehicle> { new Bicycle(), new Lorry() };
				table.BulkCopy(list);

				var array = table.ToArray();
			}
		}
	*/
	}
}
