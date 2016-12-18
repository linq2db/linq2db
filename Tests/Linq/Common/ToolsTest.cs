using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Common;
using LinqToDB.Extensions;
using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class ToolsTest
	{
		[Test, Category("WindowsOnly")]
		public void GetPathFromUriTest()
		{
			Assert.AreEqual(@"C:\Test\Space( )(h#)(p%20){[a&],t@,p%,+}.,\Release", @"file:///C:/Test/Space( )(h#)(p%20){[a&],t@,p%,+}.,/Release".GetPathFromUri());
			Assert.AreEqual(@"C:\Test\Space( )(h#)(p%20){[a&],t@,p%,+}.,\Release",  @"file://C:/Test/Space( )(h#)(p%20){[a&],t@,p%,+}.,/Release".GetPathFromUri());
		}

		[Test]
		public void AssemblyPathTest()
		{
			var asm = typeof(ToolsTest).AssemblyEx();

			var path = asm.GetPath();
			var file = asm.GetFileName();

			Assert.IsNotEmpty(path);
			Assert.IsNotEmpty(file);

			Assert.That(File     .Exists(file));
			Assert.That(Directory.Exists(path));
		}
	}
}
