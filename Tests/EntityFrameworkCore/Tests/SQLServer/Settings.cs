namespace LinqToDB.EntityFrameworkCore.Tests.SqlServer
{
	public static class Settings
	{
		public const string ForMappingConnectionString  = "Server=.;Database=ForMapping;Integrated Security=SSPI;Encrypt=true;TrustServerCertificate=true";
		public const string IssuesConnectionString      = "Server=.;Database=IssuesEFCore;Integrated Security=SSPI;Encrypt=true;TrustServerCertificate=true";
		public const string JsonConvertConnectionString = "Server=.;Database=JsonConvertContext;Integrated Security=SSPI;Encrypt=true;TrustServerCertificate=true";
		public const string NorthwindConnectionString   = "Server=.;Database=NorthwindEFCore;Integrated Security=SSPI;Encrypt=true;TrustServerCertificate=true";
		public const string ConverterConnectionString   = "Server=.;Database=ConverterTests;Integrated Security=SSPI;Encrypt=true;TrustServerCertificate=true";
		public const string InheritanceConnectionString = "Server=.;Database=InheritanceEFCore;Integrated Security=SSPI;Encrypt=true;TrustServerCertificate=true";
	}
}
