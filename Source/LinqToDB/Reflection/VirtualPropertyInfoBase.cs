using System.Reflection;

namespace LinqToDB.Reflection
{
	public abstract class VirtualPropertyInfoBase : PropertyInfo
	{
		public override int MetadataToken => -1;

		public override Module Module => typeof(object).Module;
	}
}
