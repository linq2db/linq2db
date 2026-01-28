using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Marks target method or property having only server-side translation.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class ServerSideOnlyAttribute : MappingAttribute
	{
		public override string GetObjectID() => Configuration ?? string.Empty;
	}
}
