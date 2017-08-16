using System;
using System.Collections.Generic;

namespace LinqToDB.Configuration
{
	/// <summary>
	/// Data provider configuration provider.
	/// </summary>
	public interface IDataProviderSettings
	{
		/// <summary>
		/// Gets an assembly qualified type name of this data provider.
		/// </summary>
		string TypeName { get; }

		/// <summary>
		/// Gets a name of this data provider configuration.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets a value indicating whether the provider is default.
		/// </summary>
		bool Default { get; }

		/// <summary>
		/// Extra provider-specific parameters.
		/// <para>
		/// Sybase:
		/// <list><item>assemblyName - Sybase provider assembly name.</item></list>
		/// </para>
		/// <para>
		/// SAP HANA:
		/// <list><item>assemblyName - SAP HANA provider assembly name.</item></list>
		/// </para>
		/// <para>
		/// Oracle:
		/// <list><item>assemblyName - Oracle provider assembly name.</item></list>
		/// </para>
		/// <para>
		/// SQL Server:
		/// <list><item>version - T-SQL support level, recognized values:
		/// <c>"2000"</c>, <c>"2005"</c>, <c>"2012"</c>, <c>"2014"</c>. Default: <c>"2008"</c>.</item></list>
		/// </para>
		/// <para>
		/// DB2:
		/// <list><item>version - DB2 platform, recognized values:
		/// <c>"zOS"</c> or <c>"z/OS"</c> - DB2 for z/OS. Default platform - DB2 LUW.</item></list>
		/// </para>
		/// </summary>
		IEnumerable<NamedValue> Attributes { get; }
	}
}