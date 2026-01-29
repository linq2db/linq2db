using System;

using LinqToDB.Mapping;

// ReSharper disable CheckNamespace

namespace LinqToDB
{
	partial class Sql
	{
		// TODO: write xml doc what it does (I have no idea)
		[AttributeUsage(AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
		public class EnumAttribute : MappingAttribute
		{
			public override string GetObjectID() => "..";
		}
	}
}
