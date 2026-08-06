#if HANASTUBS
namespace Sap.Data.Hana
{
	// Sap.Data.Hana.Net.v8.0 is x64-only, so it is not referenced in x86 builds (see Tests.csproj).
	// These stubs satisfy compilation of the SAP HANA tests on x86; the tests themselves only run
	// against the SapHanaNative provider, which is never enabled in an x86 test run.
	public struct HanaDecimal
	{
		public HanaDecimal(string _) { }
		public HanaDecimal(int _)    { }
	}
}
#endif
