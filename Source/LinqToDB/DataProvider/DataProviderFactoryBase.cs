using System.Collections.Generic;
using System.Linq;

using LinqToDB.Configuration;

namespace LinqToDB.DataProvider
{
	public abstract class DataProviderFactoryBase : IDataProviderFactory
	{
		protected const string VERSION       = "version";
		protected const string ASSEMBLY_NAME = "assemblyName";

		public abstract IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes);

		protected string? GetVersion(IEnumerable<NamedValue> attributes)      => GetAttribute(attributes, VERSION);
		protected string? GetAssemblyName(IEnumerable<NamedValue> attributes) => GetAttribute(attributes, ASSEMBLY_NAME);

		protected string? GetAttribute(IEnumerable<NamedValue> attributes, string attributeName) => attributes.FirstOrDefault(_ => _.Name == attributeName)?.Value;
	}
}
