using System;
using LinqToDB.CLI;

namespace LinqToDB.Tools
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			try
			{
				return new LinqToDBCLIController().Execute(args);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Unhandled exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
				return StatusCodes.INTERNAL_ERROR;
			}
		}

		// TODO: move path to options. same for sqlce provider
		//private static void RegisterSapHanaFactory()
		//{
		//	try
		//	{
		//		// woo-hoo, hardcoded pathes! default install location on x64 system
		//		var srcPath = @"c:\Program Files (x86)\sap\hdbclient\dotnetcore\v2.1\Sap.Data.Hana.Core.v2.1.dll";
		//		var targetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, Path.GetFileName(srcPath));
		//		if (File.Exists(srcPath))
		//		{
		//			// original path contains spaces which breaks broken native dlls discovery logic in SAP provider
		//			// if you run tests from path with spaces - it will not help you
		//			File.Copy(srcPath, targetPath, true);
		//			var sapHanaAssembly = Assembly.LoadFrom(targetPath);
		//			DbProviderFactories.RegisterFactory("Sap.Data.Hana", sapHanaAssembly.GetType("Sap.Data.Hana.HanaFactory")!);
		//		}
		//	}
		//	catch { }
		//}
	}
}

