using System.Reflection;

namespace LinqToDB.TypeBuilder
{
	public interface ISetParent
	{
		void SetParent([Parent]object parent, [PropertyInfo]PropertyInfo propertyInfo);
	}
}
