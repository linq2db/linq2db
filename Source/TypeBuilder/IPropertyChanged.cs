using System.Reflection;

namespace LinqToDB.TypeBuilder
{
	[PropertyChanged]
	public interface IPropertyChanged
	{
		void OnPropertyChanged(PropertyInfo propertyInfo);
	}
}
