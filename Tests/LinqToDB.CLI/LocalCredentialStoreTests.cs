using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// The built-in local store on real files: round trip, file modes and refusals, missing files, tampering, the lock, and
	/// concurrent writers.
	/// </summary>
	[TestFixture]
	public sealed class LocalCredentialStoreTests
	{
		const UnixFileMode Owner600 = UnixFileMode.UserRead | UnixFileMode.UserWrite;
		const UnixFileMode Owner700 = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

		string _root      = null!;
		string _directory = null!;

		[SetUp]
		public void SetUp()
		{
			_root      = CredentialsCliTestSupport.CreateTempDirectory();
			_directory = Path.Combine(_root, "credentials");
		}

		[TearDown]
		public void TearDown()
		{
			CredentialsCliTestSupport.DeleteDirectory(_root);
		}

		LocalCredentialStore CreateStore(TimeSpan? lockTimeout = null)
		{
			// The Windows rule (inside the user profile) is satisfied by treating the test root as the profile.
			return new LocalCredentialStore(_directory, _root) { LockTimeout = lockTimeout ?? TimeSpan.FromSeconds(5) };
		}

		string KeyPath  => Path.Combine(_directory, "credentials.key");
		string DataPath => Path.Combine(_directory, "credentials.dat");
		string LockPath => Path.Combine(_directory, "credentials.lock");

		[Test]
		public void RoundTrip()
		{
			var store = CreateStore();

			store.TryStore("Project-A/Prod", " DOMAIN\\reader ", "  p'a\"s=é ✓ ", out var error).ShouldBeTrue(error);
			store.KeyCreated.ShouldBeTrue();
			store.TryStore("b", "other", string.Empty, out error).ShouldBeTrue(error);
			store.KeyCreated.ShouldBeFalse();

			var reader = CreateStore();

			reader.TryRead("LINQ2DB/project-a/prod", out var user, out var password, out error).ShouldBeTrue(error);
			user.    ShouldBe(" DOMAIN\\reader ");
			password.ShouldBe("  p'a\"s=é ✓ ");

			reader.TryList(out var records, out var diagnostics, out error).ShouldBeTrue(error);
			records.    ShouldBe([new CredentialProfile("b", "other"), new CredentialProfile("project-a/prod", " DOMAIN\\reader ")]);
			diagnostics.ShouldBeEmpty();

			reader.TryGetCount(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(2);

			reader.TryRemove("PROJECT-A/prod", out var removed, out error).ShouldBeTrue(error);
			removed.ShouldBeTrue();
			reader.TryRemove("project-a/prod", out removed, out error).ShouldBeTrue(error);
			removed.ShouldBeFalse();

			reader.TryRead("linq2db/project-a/prod", out _, out _, out error).ShouldBeFalse();
			error.ShouldBe($"Credential target 'linq2db/project-a/prod' was not found in the local store ({_directory}).");

			reader.TryClear(out var cleared, out error).ShouldBeTrue(error);
			cleared.ShouldBe(1);

			// Clear keeps the key.
			File.Exists(KeyPath).ShouldBeTrue();
			reader.TryGetCount(out count, out error).ShouldBeTrue(error);
			count.ShouldBe(0);
		}

		[Test]
		public void FilesAreNotReadableAsText()
		{
			CreateStore().TryStore("a", "visible-user", "visible-password", out var error).ShouldBeTrue(error);

			var data = File.ReadAllBytes(DataPath);

			System.Text.Encoding.UTF8.GetString(data).ShouldNotContain("visible");
			data.Take(7).ShouldBe("L2DBLCS"u8.ToArray());

			// On Windows the key file holds the DPAPI-protected key, not the raw 32 bytes.
			if (OperatingSystem.IsWindows())
				File.ReadAllBytes(KeyPath).Length.ShouldBeGreaterThan(32);
			else
				File.ReadAllBytes(KeyPath).Length.ShouldBe(32);
		}

		[Test]
		public void FilesAreOwnerOnly()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Unix file modes.");

			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			File.GetUnixFileMode(_directory).ShouldBe(Owner700);
			File.GetUnixFileMode(KeyPath).   ShouldBe(Owner600);
			File.GetUnixFileMode(DataPath).  ShouldBe(Owner600);
			File.GetUnixFileMode(LockPath).  ShouldBe(Owner600);
		}

		[Test]
		public void ReadsWithoutDirectoryCreateNothing()
		{
			var store = CreateStore();

			store.TryRead("linq2db/a", out _, out _, out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("was not found in the local store");

			store.TryList(out var records, out _, out error).ShouldBeTrue(error);
			records.ShouldBeEmpty();

			store.TryGetCount(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(0);

			store.TryRemove("a", out var removed, out error).ShouldBeTrue(error);
			removed.ShouldBeFalse();

			store.TryClear(out count, out error).ShouldBeTrue(error);
			count.ShouldBe(0);

			Directory.Exists(_directory).ShouldBeFalse();
		}

		[Test]
		public void ReadsInAnEmptyDirectoryCreateNothing()
		{
			Directory.CreateDirectory(_directory);

			if (!OperatingSystem.IsWindows())
				File.SetUnixFileMode(_directory, Owner700);

			CreateStore().TryList(out var records, out _, out var error).ShouldBeTrue(error);
			records.ShouldBeEmpty();

			CreateStore().TryRemove("a", out var removed, out error).ShouldBeTrue(error);
			removed.ShouldBeFalse();

			Directory.EnumerateFileSystemEntries(_directory).ShouldBeEmpty();
		}

		[Test]
		public void DataWithoutKeyIsAnErrorEverywhereAndNoKeyIsCreated()
		{
			var store = CreateStore();

			store.TryStore("a", "u", "p", out var error).ShouldBeTrue(error);
			File.Delete(KeyPath);

			const string message = "credentials.key' is missing; the stored entries cannot be decrypted";

			store.TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain(message);

			store.TryList(out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain(message);

			store.TryGetCount(out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain(message);

			store.TryRemove("a", out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain(message);

			store.TryClear(out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain(message);

			store.TryStore("b", "u", "p", out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain(message);

			store.TryInitialize(out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain(message);

			File.Exists(KeyPath).ShouldBeFalse();
		}

		[Test]
		public void InitializeCreatesDirectoryAndKeyAndNeverReplacesTheKey()
		{
			var store = CreateStore();

			store.TryInitialize(out var error).ShouldBeTrue(error);
			store.KeyCreated.ShouldBeTrue();
			File.Exists(DataPath).ShouldBeFalse();

			var key = File.ReadAllBytes(KeyPath);

			store.TryInitialize(out error).ShouldBeTrue(error);
			store.KeyCreated.ShouldBeFalse();
			File.ReadAllBytes(KeyPath).ShouldBe(key);
		}

		[Test]
		public void TamperedCiphertextIsAnError()
		{
			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			var data = File.ReadAllBytes(DataPath);
			data[^1] ^= 0x01;
			File.WriteAllBytes(DataPath, data);

			CreateStore().TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("cannot be decrypted");
			error.ShouldNotBeNull().ShouldNotContain("not found");
		}

		[Test]
		public void WrongMagicIsAnError()
		{
			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			var data = File.ReadAllBytes(DataPath);
			data[0] = (byte)'X';
			File.WriteAllBytes(DataPath, data);

			CreateStore().TryList(out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("is not a linq2db local store file");
		}

		[TestCase(7,  "is not a linq2db local store file, or it is truncated", TestName = "MagicOnlyDataFileIsAnError")]
		[TestCase(20, "is truncated: it is shorter than its header",          TestName = "TruncatedDataFileIsAnError")]
		[TestCase(3,  "is not a linq2db local store file, or it is truncated", TestName = "ShortDataFileIsAnError")]
		public void TruncatedDataFileIsAnError(int length, string message)
		{
			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			File.WriteAllBytes(DataPath, File.ReadAllBytes(DataPath).Take(length).ToArray());

			foreach (var store in new[] { CreateStore(), CreateStore() })
			{
				store.TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
				error.ShouldNotBeNull().ShouldContain(message);

				store.TryStore("b", "u", "p", out error).ShouldBeFalse();
				error.ShouldNotBeNull().ShouldContain(message);
			}
		}

		[Test]
		public void UnknownVersionIsAnError()
		{
			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			var data = File.ReadAllBytes(DataPath);
			data[7] = 2;
			File.WriteAllBytes(DataPath, data);

			CreateStore().TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("has format version 2");
		}

		[Test]
		public void WrongKeyLengthIsAnError()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("The Windows key file is DPAPI-protected.");

			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);
			File.WriteAllBytes(KeyPath, new byte[16]);

			CreateStore().TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("is not a valid key (16 bytes instead of 32)");
		}

		[Test]
		public void ForeignWindowsKeyIsReported()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("DPAPI.");

			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);
			File.WriteAllBytes(KeyPath, new byte[64]);

			CreateStore().TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("the key belongs to another Windows user or machine");
		}

		[TestCase("credentials.dat")]
		[TestCase("credentials.key")]
		public void FileReadableByOthersIsRefused(string file)
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Unix file modes.");

			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			var path = Path.Combine(_directory, file);
			File.SetUnixFileMode(path, Owner600 | UnixFileMode.GroupRead | UnixFileMode.OtherRead);

			CreateStore().TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			error.ShouldBe($"'{path}' is accessible by other users. Run: chmod 600 '{path}'");
		}

		[TestCase("credentials.dat")]
		[TestCase("credentials.key")]
		public void SymlinkedFileIsRefused(string file)
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX symbolic links.");

			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			var path   = Path.Combine(_directory, file);
			var target = Path.Combine(_root, file + ".elsewhere");

			File.Move(path, target);
			File.CreateSymbolicLink(path, target);

			CreateStore().TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("is a symbolic link");
		}

		[Test]
		public void WorldWritableDirectoryIsRefused()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Unix file modes.");

			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);
			File.SetUnixFileMode(_directory, (UnixFileMode)0b111_111_111);

			try
			{
				CreateStore().TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
				error.ShouldNotBeNull().ShouldContain("chmod 700");

				CreateStore().TryStore("b", "u", "p", out error).ShouldBeFalse();
				error.ShouldNotBeNull().ShouldContain("chmod 700");
			}
			finally
			{
				File.SetUnixFileMode(_directory, Owner700);
			}
		}

		[Test]
		public void DirectoryUnderASharedWritableParentIsRefused()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Unix file modes.");

			// Another user could rename the credentials directory away and put their own in its place.
			var shared = Path.Combine(_root, "shared");
			Directory.CreateDirectory(shared);
			File.SetUnixFileMode(shared, (UnixFileMode)0b111_111_111);

			try
			{
				var store = new LocalCredentialStore(Path.Combine(shared, "credentials"), _root);

				store.TryStore("a", "u", "p", out var error).ShouldBeFalse();
				error.ShouldNotBeNull().ShouldContain($"is inside '{shared}', which other users can write to without the sticky bit");

				// With the sticky bit others cannot rename or delete what they do not own.
				File.SetUnixFileMode(shared, (UnixFileMode)0b111_111_111 | UnixFileMode.StickyBit);

				store.TryStore("a", "u", "p", out error).ShouldBeTrue(error);
			}
			finally
			{
				File.SetUnixFileMode(shared, Owner700);
			}
		}

		[Test]
		public void SymlinkedDirectoryIsRefused()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX symbolic links.");

			var target = Directory.CreateDirectory(Path.Combine(_root, "real"));
			File.SetUnixFileMode(target.FullName, Owner700);
			Directory.CreateSymbolicLink(_directory, target.FullName);

			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("is a symbolic link");
		}

		[Test]
		public void LockTimeoutNamesTheLockFile()
		{
			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			using (new FileStream(LockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			{
				CreateStore(TimeSpan.FromMilliseconds(300)).TryRead("linq2db/a", out _, out _, out error).ShouldBeFalse();
			}

			error.ShouldNotBeNull().ShouldContain($"held '{LockPath}' for more than");
		}

		[Test]
		public void StaleTemporaryFilesAreDeleted()
		{
			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			var stale = Path.Combine(_directory, "credentials.dat.0123456789abcdef.tmp");
			var fresh = Path.Combine(_directory, "credentials.key.fedcba9876543210.tmp");

			File.WriteAllText(stale, "x");
			File.WriteAllText(fresh, "x");
			File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddMinutes(-5));

			CreateStore().TryStore("b", "u", "p", out error).ShouldBeTrue(error);

			File.Exists(stale).ShouldBeFalse();
			File.Exists(fresh).ShouldBeTrue();
		}

		[Test]
		public void ReaderWithoutLockFileDoesNotBlockAWriter()
		{
			// A store whose lock file is gone (deleted by hand): readers run without the lock, and must still let a writer
			// replace the data file (on Windows a reader's handle without delete sharing would make the replace fail).
			CreateStore().TryStore("seed", "u", "p", out var error).ShouldBeTrue(error);
			File.Delete(LockPath);

			var errors = new System.Collections.Concurrent.ConcurrentQueue<string>();
			var reader = CreateStore(TimeSpan.FromSeconds(30));
			var done   = false;

			var readers = Task.Run(() =>
			{
				while (!Volatile.Read(ref done))
				{
					if (!reader.TryGetCount(out _, out var readError))
						errors.Enqueue(readError!);
				}
			});

			var writer = CreateStore(TimeSpan.FromSeconds(30));

			for (var i = 0; i < 25; i++)
			{
				if (!writer.TryStore($"w/{i}", "u", "p", out var storeError))
					errors.Enqueue(storeError!);
			}

			Volatile.Write(ref done, true);
			readers.Wait(TimeSpan.FromSeconds(60)).ShouldBeTrue("the reader did not finish");

			errors.ShouldBeEmpty();
			CreateStore().TryGetCount(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(26);
		}

		[Test]
		public void ConcurrentWritersLoseNoUpdate()
		{
			// Two store instances on two threads: the lock (flock on Unix, a sharing violation on Windows) serializes them
			// within one process as it does across processes.
			CreateStore().TryInitialize(out var error).ShouldBeTrue(error);

			const int perWriter = 25;

			var errors  = new System.Collections.Concurrent.ConcurrentQueue<string>();
			var writers = Enumerable.Range(0, 2).Select(writer => Task.Run(() =>
			{
				var store = CreateStore(TimeSpan.FromSeconds(30));

				for (var i = 0; i < perWriter; i++)
				{
					if (!store.TryStore($"w{writer}/{i}", "u", $"p{writer}-{i}", out var storeError))
						errors.Enqueue(storeError!);
				}
			})).ToArray();

			// A reader running alongside the writers (on Windows a reader holding the data file open would make a writer's
			// replace fail).
			var reader = Task.Run(() =>
			{
				var store = CreateStore(TimeSpan.FromSeconds(30));

				while (!writers.All(static writer => writer.IsCompleted))
				{
					if (!store.TryGetCount(out _, out var readError))
						errors.Enqueue(readError!);
				}
			});

			Task.WaitAll([.. writers, reader], TimeSpan.FromSeconds(120)).ShouldBeTrue("the writers did not finish");

			errors.ShouldBeEmpty();

			var check = CreateStore();

			check.TryGetCount(out var count, out error).ShouldBeTrue(error);
			count.ShouldBe(2 * perWriter);

			check.TryRead("linq2db/w1/24", out _, out var password, out error).ShouldBeTrue(error);
			password.ShouldBe("p1-24");

			Directory.EnumerateFiles(_directory, "*.tmp").ShouldBeEmpty();
		}
	}
}
