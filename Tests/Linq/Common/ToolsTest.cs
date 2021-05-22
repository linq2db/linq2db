using System.IO;
using System.Runtime.InteropServices;
using LinqToDB.Common;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class ToolsTest
	{
		[Test]
		public void GetPathFromUriTest()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Assert.AreEqual(@"C:\Test\Space( )(h#)(p%20){[a&],t@,p%,+}.,\Release", @"file:///C:/Test/Space( )(h#)(p%20){[a&],t@,p%,+}.,/Release".GetPathFromUri());
				Assert.AreEqual(@"C:\Test\Space( )(h#)(p%20){[a&],t@,p%,+}.,\Release", @"file://C:/Test/Space( )(h#)(p%20){[a&],t@,p%,+}.,/Release".GetPathFromUri());
			}
			else
			{
				Assert.AreEqual(@"/Test/Space( )(h#)(p%20){[a&],t@,p%,+}.,/Release", @"file:////Test/Space( )(h#)(p%20){[a&],t@,p%,+}.,/Release".GetPathFromUri());
				Assert.AreEqual(@"/Test/Space( )(h#)(p%20){[a&],t@,p%,+}.,/Release", @"file:///Test/Space( )(h#)(p%20){[a&],t@,p%,+}.,/Release".GetPathFromUri());
			}
		}

		[Test]
		public void AssemblyPathTest()
		{
			var asm = typeof(ToolsTest).Assembly;

			var path = asm.GetPath();
			var file = asm.GetFileName();

			Assert.IsNotEmpty(path);
			Assert.IsNotEmpty(file);

			Assert.That(File     .Exists(file));
			Assert.That(Directory.Exists(path));
		}
	}
}
