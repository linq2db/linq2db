using System;

using LinqToDB.Mapping;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	partial class Sql
	{
		[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
		public class EnumAttribute : MappingAttribute
		{
			public override string GetObjectID() => "..";
		}
	}
}
