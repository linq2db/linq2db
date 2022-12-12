using System;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	using Mapping;

	partial class Sql
	{
		[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
		public class EnumAttribute : MappingAttribute
		{
			public override string GetObjectID() => "..";
		}
	}
}
