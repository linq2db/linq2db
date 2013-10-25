using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

using LinqToDB;

[assembly: AssemblyTitle           (LinqToDBConstants.ProductName)]
[assembly: AssemblyDescription     (LinqToDBConstants.ProductDescription)]
[assembly: AssemblyProduct         (LinqToDBConstants.ProductName)]
[assembly: AssemblyCopyright       (LinqToDBConstants.Copyright)]
[assembly: AssemblyCulture         ("")]
[assembly: ComVisible              (false)]
[assembly: Guid                    ("080146c6-967e-4bbf-afdf-a9e0fa01d9c2")]
[assembly: AssemblyVersion         (LinqToDBConstants.FullVersionString)]
[assembly: AssemblyFileVersion     (LinqToDBConstants.FullVersionString)]
[assembly: CLSCompliant            (true)]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: AllowPartiallyTrustedCallers]

[assembly: InternalsVisibleTo("linq2db.Tests, PublicKey=" +
	"00240000048000009400000006020000002400005253413100040000010001006f967cbdfdadb7" +
	"4f775f28dc0e73e0514d26e50450c495cb300bd4f3cd9ab4ed3d1eeebaa7de18aa0d51b5a46fee" +
	"ae4f146083d82687998a288447791f8109bd2478d0fca90575eef33867b5307e1d67cd49b30b19" +
	"b573805e66b805984541709994fb81e703c299eed75a7103cbc6a89b190ee5641d5465151d0c51" +
	"5e5897d5")]
