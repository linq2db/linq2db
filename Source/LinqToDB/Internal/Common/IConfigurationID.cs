using System.ComponentModel;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// For internal use.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IConfigurationID
	{
		int ConfigurationID { get; }
	}
}
