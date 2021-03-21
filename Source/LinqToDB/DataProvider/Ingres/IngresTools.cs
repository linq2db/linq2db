using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Ingres
{
	using Configuration;
	using Data;

	[PublicAPI]
	public static class IngresTools
	{
		private static readonly Lazy<IDataProvider> _ingresDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new IngresDataProvider(ProviderName.Ingres);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		public static bool AutoDetectProvider { get; set; } = true;

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			switch (css.ProviderName)
			{
				case IngresProviderAdapter.ClientNamespace					  :
				case ProviderName.Ingres                                      : return _ingresDataProvider.Value;
				case ""                                                       :
				case null                                                     :
					if (css.Name == "Ingres")
						goto case "Ingres";
					break;
			}

			return null;
		}

		public static void ResolveIngres(string path)
		{
			new AssemblyResolver(path, IngresProviderAdapter.AssemblyName);
		}

		public static void ResolveIngres(Assembly assembly)
		{
			new AssemblyResolver(assembly, assembly.FullName!);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_ingresDataProvider.Value, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_ingresDataProvider.Value, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_ingresDataProvider.Value, transaction);
		}

		#endregion
	}
}
