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

		[TestCase("credentials.dat", TestName = "DataFileCanBeReplacedWhileBeingRead")]
		[TestCase("credentials.key", TestName = "KeyFileCanBeReplacedWhileBeingRead")]
		public void FileCanBeReplacedWhileBeingRead(string fileName)
		{
			// Windows lets a rename replace a file only when every open handle shares delete access. The shared read keeps
			// its handle open while the writer replaces the file (no timing: the replace runs inside the read), and the
			// reader still gets the contents it opened.
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("Windows sharing rules; Unix renames over open files.");

			var (path, replacement) = CreateFileAndReplacement(fileName);

			var read = LocalCredentialStore.ReadAllBytesShared(path, () => LocalCredentialStore.ReplaceFile(replacement, path, null));

			read.ShouldBe("old"u8.ToArray());
			File.ReadAllBytes(path).ShouldBe("new"u8.ToArray());
		}

		[Test]
		public void FileOpenWithoutShareDeleteCannotBeReplaced()
		{
			// Why the shared read passes FileShare.Delete: a reader that shares only read access blocks the replace for as
			// long as it holds the file, through every retry.
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("Windows sharing rules; Unix renames over open files.");

			var (path, replacement) = CreateFileAndReplacement("credentials.dat");
			var retries             = 0;

			using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				var exception = Should.Throw<Exception>(() => LocalCredentialStore.ReplaceFile(replacement, path, () => retries++));

				// A sharing violation or access denied, depending on the Windows version.
				(exception is IOException or UnauthorizedAccessException).ShouldBeTrue(exception.ToString());
			}

			retries.ShouldBe(4);
			File.ReadAllBytes(path).ShouldBe("old"u8.ToArray());
		}

		(string Path, string Replacement) CreateFileAndReplacement(string fileName)
		{
			Directory.CreateDirectory(_directory);

			var path        = Path.Combine(_directory, fileName);
			var replacement = Path.Combine(_directory, fileName + ".new.tmp");

			File.WriteAllBytes(path, "old"u8.ToArray());
			File.WriteAllBytes(replacement, "new"u8.ToArray());

			return (path, replacement);
		}

		[Test]
		public void KeyReplacedWhileBeingCreatedFailsBeforeAnythingIsStored()
		{
			// Without an effective lock, a concurrent creator can replace the new key after it was moved into place; data
			// encrypted with either key would then not match the key on disk.
			var store = CreateStore();

			store.KeyInstalled = () => File.WriteAllBytes(KeyPath, new byte[32]);

			store.TryStore("a", "user", "secret", out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain($"'{KeyPath}' was replaced by another process while it was being created");
			File.Exists(DataPath).ShouldBeFalse();
		}

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
				error.ShouldNotBeNull().ShouldContain($"is inside '{shared}', which every user can write to without the sticky bit");

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
		public void GroupWritableAncestorIsAccepted()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Unix file modes.");

			// Private user groups with umask 002 leave ~/.config at 0775.
			var config = Path.Combine(_root, ".config");
			Directory.CreateDirectory(config);
			File.SetUnixFileMode(config, (UnixFileMode)0b111_111_101);

			new LocalCredentialStore(Path.Combine(config, "linq2db"), _root).TryStore("a", "u", "p", out var error).ShouldBeTrue(error);
		}

		[Test]
		public void LinkIntoAWorldWritableTreeIsRefused()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX symbolic links.");

			// ~/work -> <shared>/work: the real path is under a directory every user can write to.
			var shared = Path.Combine(_root, "shared");
			var work   = Path.Combine(shared, "work");
			var link   = Path.Combine(_root, "work");

			Directory.CreateDirectory(work);
			File.SetUnixFileMode(work, Owner700);
			File.SetUnixFileMode(shared, (UnixFileMode)0b111_111_111);
			Directory.CreateSymbolicLink(link, work);

			try
			{
				new LocalCredentialStore(Path.Combine(link, "credentials"), _root).TryStore("a", "u", "p", out var error).ShouldBeFalse();
				error.ShouldNotBeNull().ShouldContain($"is inside '{shared}', which every user can write to without the sticky bit");
			}
			finally
			{
				File.SetUnixFileMode(shared, Owner700);
			}
		}

		[Test]
		public void LinkInAWorldWritableDirectoryIsRefused()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX symbolic links.");

			// <shared>/link -> ~/private: the real path is the user's own, but any user can point the link elsewhere later.
			var shared  = Path.Combine(_root, "shared");
			var real    = Path.Combine(_root, "private");
			var link    = Path.Combine(shared, "link");

			Directory.CreateDirectory(shared);
			Directory.CreateDirectory(real);
			File.SetUnixFileMode(real, Owner700);
			Directory.CreateSymbolicLink(link, real);
			File.SetUnixFileMode(shared, (UnixFileMode)0b111_111_111);

			try
			{
				new LocalCredentialStore(Path.Combine(link, "credentials"), _root).TryStore("a", "u", "p", out var error).ShouldBeFalse();
				error.ShouldNotBeNull().ShouldContain($"is inside '{shared}', which every user can write to without the sticky bit");
			}
			finally
			{
				File.SetUnixFileMode(shared, Owner700);
			}
		}

		[Test]
		public void ReadOnlyStoreCanBeRead()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("Unix file modes.");

			// A store mounted read-only (here: files and directory without write permission): reading only locks, so it
			// needs no write access; writing fails at once instead of waiting for the lock.
			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);

			const UnixFileMode ReadOnly = UnixFileMode.UserRead;

			File.SetUnixFileMode(LockPath, ReadOnly);
			File.SetUnixFileMode(DataPath, ReadOnly);
			File.SetUnixFileMode(KeyPath,  ReadOnly);
			File.SetUnixFileMode(_directory, UnixFileMode.UserRead | UnixFileMode.UserExecute);

			try
			{
				CreateStore().TryRead("linq2db/a", out var user, out var password, out error).ShouldBeTrue(error);
				user.    ShouldBe("u");
				password.ShouldBe("p");

				if (Environment.UserName != "root")
				{
					var stopwatch = System.Diagnostics.Stopwatch.StartNew();

					CreateStore(TimeSpan.FromSeconds(3)).TryStore("b", "u", "p", out error).ShouldBeFalse();
					stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(3));
					error.ShouldNotBeNull().ShouldNotContain("another linq2db-cli process");
				}
			}
			finally
			{
				File.SetUnixFileMode(_directory, Owner700);
				File.SetUnixFileMode(LockPath, CredentialsDirectory.FileMode);
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
		public void LockFailureOtherThanContentionIsReportedAtOnce()
		{
			// The lock path cannot be opened for a reason other than another holder (here a dangling symbolic link, standing
			// for a read-only or failing file system): no wait for the lock timeout, and no blame on another process.
			CreateStore().TryStore("a", "u", "p", out var error).ShouldBeTrue(error);
			File.Delete(LockPath);

			try
			{
				File.CreateSymbolicLink(LockPath, Path.Combine(_root, "missing", "credentials.lock"));
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				Assert.Ignore($"Cannot create a symbolic link here: {ex.Message}");
			}

			var timeout   = TimeSpan.FromSeconds(3);
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			CreateStore(timeout).TryStore("b", "u", "p", out error).ShouldBeFalse();

			stopwatch.Stop();

			error.ShouldNotBeNull().ShouldNotContain("another linq2db-cli process");
			stopwatch.Elapsed.ShouldBeLessThan(timeout);
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
			// A store whose lock file is gone (deleted by hand): a reader runs without the lock, so a writer can replace the
			// data file while the reader holds it open. Elsewhere a rename over an open file succeeds at once; on Windows it
			// fails while the reader's handle is open, and the writer retries the rename until the read is done.
			CreateStore().TryStore("seed", "u", "p", out var error).ShouldBeTrue(error);
			File.Delete(LockPath);

			var timeout = TimeSpan.FromSeconds(30);
			var failures = 0;

			// Not disposed: the writer task may still hold them when an assertion fails, and they own no kernel handle.
			var replaceFailed = new ManualResetEventSlim();
			var readerDone    = new ManualResetEventSlim();

			var writer = new LocalCredentialStore(_directory, _root)
			{
				DataFileReplaceFailed = () =>
				{
					// The first failure is the reader's open handle: hold the retry until the reader has closed it, so the
					// next attempt runs against a closed file however the threads are scheduled.
					if (Interlocked.Increment(ref failures) == 1)
					{
						replaceFailed.Set();
						readerDone.Wait(timeout);
					}
				},
			};

			Task<string?>? writing = null;
			var writerDoneWhileOpen = false;

			var reader = new LocalCredentialStore(_directory, _root)
			{
				DataFileOpenedForRead = () =>
				{
					// Once only: the writer runs its own reads through another instance, without this hook.
					if (writing != null)
						return;

					writing = Task.Run(() =>
					{
						return writer.TryStore("written", "u", "p", out var writerError) ? null : writerError ?? "failed";
					});

					if (OperatingSystem.IsWindows())
					{
						// Keep the file open until the writer's rename has failed on it (or the writer gave up), then let the
						// read finish.
						var started = writing;
						SpinWait.SpinUntil(() => replaceFailed.IsSet || started.IsCompleted, timeout);
					}
					else
					{
						writerDoneWhileOpen = writing.Wait(timeout);
					}
				},
			};

			int count;

			try
			{
				reader.TryGetCount(out count, out error).ShouldBeTrue(error);
			}
			finally
			{
				readerDone.Set();
			}

			count.ShouldBe(1);

			var written = writing.ShouldNotBeNull("the reader did not open the data file");

			written.Wait(timeout).ShouldBeTrue("the writer did not finish");
			written.Result.ShouldBeNull();

			if (OperatingSystem.IsWindows())
			{
				replaceFailed.IsSet.ShouldBeTrue("the writer's rename did not run into the open reader");
			}
			else
			{
				writerDoneWhileOpen.ShouldBeTrue("the writer did not finish while the reader held the file open");
				failures.ShouldBe(0);
			}

			CreateStore().TryGetCount(out count, out error).ShouldBeTrue(error);
			count.ShouldBe(2);
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
