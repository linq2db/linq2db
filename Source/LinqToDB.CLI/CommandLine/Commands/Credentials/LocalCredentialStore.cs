using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// The built-in <c>local</c> store: linq2db's own encrypted file in the credentials directory, readable only by the
	/// user, with its key in a separate file next to it (on Windows the key is protected with DPAPI).
	/// </summary>
	/// <remarks>
	/// <para>
	/// Files: <c>credentials.key</c> (32 random bytes; DPAPI-wrapped on Windows), <c>credentials.dat</c> (magic and format
	/// version, a 12-byte nonce, a 16-byte tag, then the AES-256-GCM ciphertext of a JSON object
	/// <c>{ "target": { "user": ..., "password": ... } }</c>; the header is the associated data) and
	/// <c>credentials.lock</c>.
	/// </para>
	/// <para>
	/// Every operation holds an OS lock on <c>credentials.lock</c> (<see cref="FileShare.None"/>, released when the process
	/// ends, so never stale): writers read, modify and replace the data under it, and a reader never holds the data file
	/// open while a writer replaces it. Reads never create the directory, the key or the lock file; only storing creates
	/// them. Data without its key is an error everywhere, and no new key is created then: it would make the stored entries
	/// unreadable for good.
	/// </para>
	/// </remarks>
	internal sealed class LocalCredentialStore : ICredentialStore
	{
		public const string KeyFileName  = "credentials.key";
		public const string DataFileName = "credentials.dat";
		public const string LockFileName = "credentials.lock";

		const int KeySize   = 32;
		const int NonceSize = 12;
		const int TagSize   = 16;

		/// <summary>Magic and format version; also the associated data of the encryption.</summary>
		static readonly byte[] _header = [(byte)'L', (byte)'2', (byte)'D', (byte)'B', (byte)'L', (byte)'C', (byte)'S', 1];

		static readonly byte[] _keyEntropy = SHA256.HashData(Encoding.UTF8.GetBytes("linq2db local store key v1"));

		static readonly TimeSpan _staleTemporaryAge = TimeSpan.FromMinutes(1);

		readonly string  _directory;
		readonly string? _userProfile;

		/// <param name="directory">The credentials directory.</param>
		/// <param name="userProfile">The user profile directory (Windows): the credentials directory must be inside it.</param>
		public LocalCredentialStore(string directory, string? userProfile)
		{
			_directory   = directory;
			_userProfile = userProfile;
		}

		/// <summary>How long an operation waits for another process holding the lock.</summary>
		public TimeSpan LockTimeout { get; init; } = TimeSpan.FromSeconds(5);

		/// <summary>The credentials directory.</summary>
		public string Directory => _directory;

		/// <summary>
		/// Test hook: runs while the data file is open for reading, so a test can replace the file under an open reader.
		/// </summary>
		internal Action? DataFileOpenedForRead { get; init; }

		/// <summary>Set when the last operation created the key, which is when the store comes into existence.</summary>
		public bool KeyCreated { get; private set; }

		string KeyPath  => Path.Combine(_directory, KeyFileName);
		string DataPath => Path.Combine(_directory, DataFileName);
		string LockPath => Path.Combine(_directory, LockFileName);

		string Name => $"the local store ({_directory})";

		public bool TryRead(string target, out string? user, out string? password, out string? error)
		{
			user     = null;
			password = null;

			if (!CredentialTargets.TryNormalize(target, out var normalized, out error))
				return false;

			if (!TryLoadForRead(out var entries, out error))
				return false;

			if (!entries.TryGetValue(normalized, out var entry))
			{
				error = $"Credential target '{normalized}' was not found in {Name}.";
				return false;
			}

			user     = entry.User;
			password = entry.Password;
			return true;
		}

		public bool TryList(out IReadOnlyList<CredentialProfile> profiles, out IReadOnlyList<string> diagnostics, out string? error)
		{
			profiles    = [];
			diagnostics = [];

			if (!TryLoadForRead(out var entries, out error))
				return false;

			profiles = entries
				.Where(static entry => entry.Key.StartsWith(CredentialTargets.Prefix, StringComparison.Ordinal))
				.Select(static entry => new CredentialProfile(entry.Key.Substring(CredentialTargets.Prefix.Length), entry.Value.User))
				.OrderBy(static profile => profile.Name, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			return true;
		}

		public bool TryGetCount(out int count, out string? error)
		{
			count = 0;

			if (!TryLoadForRead(out var entries, out error))
				return false;

			count = entries.Keys.Count(static target => target.StartsWith(CredentialTargets.Prefix, StringComparison.Ordinal));
			return true;
		}

		public bool TryStore(string name, string user, string password, out string? error)
		{
			KeyCreated = false;

			if (!CredentialTargets.TryNormalize(CredentialTargets.Prefix + name, out var target, out error)
				|| !CredentialTargets.ValidateValue("user name", user, out error)
				|| !CredentialTargets.ValidateValue("password", password, out error))
			{
				return false;
			}

			return TryModify(createStore: true, entries =>
			{
				entries[target] = (user, password);
				return true;
			}, out error);
		}

		public bool TryRemove(string name, out bool removed, out string? error)
		{
			removed = false;

			if (!CredentialTargets.TryNormalize(CredentialTargets.Prefix + name, out var target, out error))
				return false;

			var result = false;

			if (!TryModify(createStore: false, entries => result = entries.Remove(target), out error))
				return false;

			removed = result;
			return true;
		}

		public bool TryClear(out int removedCount, out string? error)
		{
			var count = 0;

			removedCount = 0;

			if (!TryModify(createStore: false, entries =>
			{
				foreach (var target in entries.Keys.Where(static target => target.StartsWith(CredentialTargets.Prefix, StringComparison.Ordinal)).ToList())
				{
					entries.Remove(target);
					count++;
				}

				return count > 0;
			}, out error))
			{
				return false;
			}

			removedCount = count;
			return true;
		}

		/// <summary>
		/// Creates the credentials directory and the key when they are missing (<c>credentials cli init --store local</c>),
		/// so problems show up at once. Refused when the data exists without its key.
		/// </summary>
		public bool TryInitialize(out string? error)
		{
			KeyCreated = false;

			if (!CheckEnvironment(out error) || !CredentialsDirectory.TryEnsure(_directory, out error))
				return false;

			return Guard(() =>
			{
				using var lockFile = AcquireLock(create: true, out var lockError);

				if (lockError != null)
					return lockError;

				if (!TryGetKey(create: true, out var key, out var keyError))
					return keyError;

				CryptographicOperations.ZeroMemory(key);
				return null;
			}, out error);
		}

		/// <summary>Checks that hold before any file is touched: AES-GCM support and, on Windows, the directory location.</summary>
		bool CheckEnvironment(out string? error)
		{
			if (!AesGcm.IsSupported)
			{
				error = "The local store cannot be used: this platform has no AES-GCM.";
				return false;
			}

			if (OperatingSystem.IsWindows() && !CredentialsDirectory.IsInsideProfile(_directory, _userProfile))
			{
				error = CredentialsDirectory.OutsideProfileMessage(_directory);
				return false;
			}

			error = null;
			return true;
		}

		bool TryLoadForRead(out Dictionary<string, (string User, string Password)> entries, out string? error)
		{
			entries = new(StringComparer.Ordinal);

			if (!CheckEnvironment(out error))
				return false;

			// Reads never create anything: no directory means an empty store.
			if (!System.IO.Directory.Exists(_directory))
				return true;

			if (!OperatingSystem.IsWindows() && !CredentialsDirectory.CheckUnix(_directory, out error))
				return false;

			Dictionary<string, (string User, string Password)>? loaded = null;

			if (!Guard(() =>
			{
				// Without a lock file nothing was ever stored here by this tool; reading must not create one.
				using var lockFile = AcquireLock(create: false, out var lockError);

				if (lockError != null)
					return lockError;

				return Load(out loaded);
			}, out error))
			{
				return false;
			}

			entries = loaded!;
			return true;
		}

		/// <summary>
		/// Reads, modifies and writes the data under the lock. <paramref name="createStore"/> creates the directory, the lock
		/// and the key when they are missing; otherwise a missing store has nothing to modify.
		/// </summary>
		bool TryModify(bool createStore, Func<Dictionary<string, (string User, string Password)>, bool> modify, out string? error)
		{
			if (!CheckEnvironment(out error))
				return false;

			if (createStore)
			{
				if (!CredentialsDirectory.TryEnsure(_directory, out error))
					return false;
			}
			else
			{
				if (!System.IO.Directory.Exists(_directory))
					return true;

				if (!OperatingSystem.IsWindows() && !CredentialsDirectory.CheckUnix(_directory, out error))
					return false;
			}

			return Guard(() =>
			{
				using var lockFile = AcquireLock(create: createStore || File.Exists(DataPath), out var lockError);

				if (lockError != null)
					return lockError;

				DeleteStaleTemporaryFiles();

				var loadError = Load(out var entries);

				if (loadError != null)
					return loadError;

				if (!createStore && !File.Exists(DataPath))
					return null;

				if (!modify(entries!))
					return null;

				if (!TryGetKey(create: createStore, out var key, out var keyError))
					return keyError;

				try
				{
					return Save(entries!, key);
				}
				finally
				{
					CryptographicOperations.ZeroMemory(key);
				}
			}, out error);
		}

		/// <summary>Runs an operation, turning file system failures into store errors.</summary>
		bool Guard(Func<string?> operation, out string? error)
		{
			try
			{
				error = operation();
				return error == null;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				error = $"Cannot access {Name}: {ex.Message}{CredentialsDirectory.AccessHint(ex)}";
				return false;
			}
		}

		/// <summary>
		/// Takes the lock, waiting a bounded time for another process. Returns <see langword="null"/> without an error when
		/// <paramref name="create"/> is <see langword="false"/> and the lock file does not exist. Only a lock held by another
		/// process is waited for; any other failure to open the lock file (a read-only or failing file system, a dangling
		/// link) is reported at once.
		/// </summary>
		FileStream? AcquireLock(bool create, out string? error)
		{
			var options = new FileStreamOptions
			{
				Mode   = create ? FileMode.OpenOrCreate : FileMode.Open,
				// Opening an existing lock file only to lock it needs no write access, so a store on a read-only mount can
				// still be read. The lock stays exclusive either way (FileShare.None; flock(LOCK_EX) on Unix).
				Access = create ? FileAccess.ReadWrite : FileAccess.Read,
				Share  = FileShare.None,
			};

			if (!OperatingSystem.IsWindows() && create)
				options.UnixCreateMode = CredentialsDirectory.FileMode;

			var deadline = DateTime.UtcNow + LockTimeout;

			while (true)
			{
				try
				{
					error = null;
					return new FileStream(LockPath, options);
				}
				catch (FileNotFoundException) when (!create)
				{
					error = null;
					return null;
				}
				catch (IOException ex) when (IsHeldByAnotherProcess(ex) && DateTime.UtcNow < deadline)
				{
					Thread.Sleep(50);
				}
				catch (IOException ex) when (IsHeldByAnotherProcess(ex))
				{
					error = $"Cannot lock {Name}: another linq2db-cli process held '{LockPath}' for more than {LockTimeout.TotalSeconds.ToString("0", CultureInfo.InvariantCulture)} seconds.";
					return null;
				}
				catch (IOException ex)
				{
					error = $"Cannot lock {Name}: '{LockPath}' cannot be opened: {ex.Message}";
					return null;
				}
			}
		}

		/// <summary>
		/// Whether opening the lock file failed because another handle holds it: a sharing or lock violation on Windows,
		/// <c>EWOULDBLOCK</c> from <c>flock</c> on Unix (.NET reports the raw errno as the HResult there).
		/// </summary>
		static bool IsHeldByAnotherProcess(IOException exception)
		{
			const int ErrorSharingViolation = unchecked((int)0x80070020);
			const int ErrorLockViolation    = unchecked((int)0x80070021);
			const int EWouldBlock           = 11;
			const int BsdEWouldBlock        = 35;

			if (exception is FileNotFoundException or DirectoryNotFoundException or PathTooLongException)
				return false;

			if (OperatingSystem.IsWindows())
				return exception.HResult is ErrorSharingViolation or ErrorLockViolation;

			return exception.HResult == (OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD() ? BsdEWouldBlock : EWouldBlock);
		}

		/// <summary>Loads the entries (empty when there is no data file). Returns an error message on failure.</summary>
		string? Load(out Dictionary<string, (string User, string Password)>? entries)
		{
			entries = new(StringComparer.Ordinal);

			if (!File.Exists(DataPath))
				return null;

			if (!File.Exists(KeyPath))
				return MissingKeyMessage();

			var fileError = CheckFile(DataPath);

			if (fileError != null)
				return fileError;

			var data = ReadAllBytesShared(DataPath, DataFileOpenedForRead);

			if (data.Length < _header.Length || !data.AsSpan(0, _header.Length - 1).SequenceEqual(_header.AsSpan(0, _header.Length - 1)))
				return $"'{DataPath}' is not a linq2db local store file, or it is truncated.";

			if (data[_header.Length - 1] != _header[^1])
				return $"'{DataPath}' has format version {data[_header.Length - 1].ToString(CultureInfo.InvariantCulture)}, which this linq2db-cli does not read.";

			if (data.Length < _header.Length + NonceSize + TagSize)
				return $"'{DataPath}' is truncated: it is shorter than its header, nonce and tag.";

			if (!TryGetKey(create: false, out var key, out var keyError))
				return keyError;

			var plaintext = new byte[data.Length - _header.Length - NonceSize - TagSize];

			try
			{
				using (var aes = new AesGcm(key, TagSize))
				{
					aes.Decrypt(
						data.AsSpan(_header.Length, NonceSize),
						data.AsSpan(_header.Length + NonceSize + TagSize),
						data.AsSpan(_header.Length + NonceSize, TagSize),
						plaintext,
						_header);
				}

				entries = Deserialize(plaintext);

				return entries == null ? $"'{DataPath}' does not contain valid credential entries." : null;
			}
			catch (AuthenticationTagMismatchException)
			{
				return $"'{DataPath}' cannot be decrypted with '{KeyPath}': the file was changed or belongs to another key.";
			}
			finally
			{
				CryptographicOperations.ZeroMemory(key);
				CryptographicOperations.ZeroMemory(plaintext);
			}
		}

		string MissingKeyMessage()
		{
			return $"'{KeyPath}' is missing; the stored entries cannot be decrypted. Delete '{DataPath}' and re-enter them.";
		}

		/// <summary>
		/// Reads the key, or creates it when <paramref name="create"/> is set and neither key nor data exists. The key is
		/// written to a temporary file, flushed to disk and moved into place without overwriting, so a partial key file never
		/// exists and a concurrent creator's key wins.
		/// </summary>
		bool TryGetKey(bool create, out byte[] key, out string? error)
		{
			key = [];

			if (!File.Exists(KeyPath))
			{
				if (File.Exists(DataPath))
				{
					error = MissingKeyMessage();
					return false;
				}

				if (!create)
				{
					error = MissingKeyMessage();
					return false;
				}

				var newKey    = RandomNumberGenerator.GetBytes(KeySize);
				var temporary = Path.Combine(_directory, $"{KeyFileName}.{Guid.NewGuid():N}.tmp");

				try
				{
					var stored = newKey;

					if (OperatingSystem.IsWindows())
					{
						if (!Dpapi.TryTransform(newKey, _keyEntropy, protect: true, out var wrapped, out var dpapiError))
						{
							error = $"Cannot protect the key of {Name} with DPAPI: {dpapiError}";
							return false;
						}

						stored = wrapped!;
					}

					WriteNewFile(temporary, stored);

					try
					{
						File.Move(temporary, KeyPath, overwrite: false);
						KeyCreated = true;
					}
					catch (IOException) when (File.Exists(KeyPath))
					{
						// Another process created the key first: use it.
					}
				}
				finally
				{
					CryptographicOperations.ZeroMemory(newKey);

					if (File.Exists(temporary))
						File.Delete(temporary);
				}
			}

			var fileError = CheckFile(KeyPath);

			if (fileError != null)
			{
				error = fileError;
				return false;
			}

			var bytes = ReadAllBytesShared(KeyPath, null);

			if (OperatingSystem.IsWindows())
			{
				if (!Dpapi.TryTransform(bytes, _keyEntropy, protect: false, out var unwrapped, out _))
				{
					error = $"'{KeyPath}' cannot be unprotected: the key belongs to another Windows user or machine. Delete '{KeyPath}' and '{DataPath}' and re-enter the passwords.";
					return false;
				}

				bytes = unwrapped!;
			}

			if (bytes.Length != KeySize)
			{
				CryptographicOperations.ZeroMemory(bytes);
				error = $"'{KeyPath}' is not a valid key ({bytes.Length.ToString(CultureInfo.InvariantCulture)} bytes instead of {KeySize.ToString(CultureInfo.InvariantCulture)}).";
				return false;
			}

			key   = bytes;
			error = null;
			return true;
		}

		/// <summary>Encrypts the entries with a fresh nonce and replaces the data file atomically.</summary>
		string? Save(Dictionary<string, (string User, string Password)> entries, byte[] key)
		{
			var plaintext = Serialize(entries);
			var data      = new byte[_header.Length + NonceSize + TagSize + plaintext.Length];

			try
			{
				_header.CopyTo(data, 0);
				RandomNumberGenerator.Fill(data.AsSpan(_header.Length, NonceSize));

				using var aes = new AesGcm(key, TagSize);

				aes.Encrypt(
					data.AsSpan(_header.Length, NonceSize),
					plaintext,
					data.AsSpan(_header.Length + NonceSize + TagSize),
					data.AsSpan(_header.Length + NonceSize, TagSize),
					_header);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(plaintext);
			}

			var temporary = Path.Combine(_directory, $"{DataFileName}.{Guid.NewGuid():N}.tmp");

			try
			{
				WriteNewFile(temporary, data);
				// The rename replaces the file atomically: a reader sees the old or the new data, never a part.
				ReplaceFile(temporary, DataPath);
				return null;
			}
			finally
			{
				if (File.Exists(temporary))
					File.Delete(temporary);
			}
		}

		/// <summary>
		/// Reads a whole file while letting a writer replace it: a read can run without the lock (when no lock file exists
		/// yet), and on Windows an open handle without delete sharing would make the writer's replacing move fail.
		/// </summary>
		static byte[] ReadAllBytesShared(string path, Action? opened)
		{
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

			opened?.Invoke();

			var result = new byte[stream.Length];
			stream.ReadExactly(result);
			return result;
		}

		/// <summary>
		/// Renames <paramref name="source"/> over <paramref name="destination"/>. On Windows a process outside the lock (an
		/// antivirus scanner, an indexer) can hold the file briefly, so the rename is retried a few times.
		/// </summary>
		static void ReplaceFile(string source, string destination)
		{
			for (var attempt = 1; ; attempt++)
			{
				try
				{
					File.Move(source, destination, overwrite: true);
					return;
				}
				catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException) && OperatingSystem.IsWindows() && attempt < 5)
				{
					Thread.Sleep(50);
				}
			}
		}

		/// <summary>Creates a new owner-only file and flushes its contents to disk before it is renamed into place.</summary>
		static void WriteNewFile(string path, byte[] contents)
		{
			var options = new FileStreamOptions
			{
				Mode   = FileMode.CreateNew,
				Access = FileAccess.Write,
				Share  = FileShare.None,
			};

			if (!OperatingSystem.IsWindows())
				options.UnixCreateMode = CredentialsDirectory.FileMode;

			using var stream = new FileStream(path, options);

			stream.Write(contents, 0, contents.Length);
			stream.Flush(flushToDisk: true);
		}

		/// <summary>Removes temporary files that a crashed run left behind (they hold ciphertext or a wrapped key).</summary>
		void DeleteStaleTemporaryFiles()
		{
			foreach (var file in System.IO.Directory.EnumerateFiles(_directory, "credentials.*.tmp"))
			{
				try
				{
					if (DateTime.UtcNow - File.GetLastWriteTimeUtc(file) > _staleTemporaryAge)
						File.Delete(file);
				}
				catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
				{
				}
			}
		}

		/// <summary>
		/// Unix: refuses a key or data file that is a symbolic link or that other users can access.
		/// </summary>
		static string? CheckFile(string path)
		{
			if (OperatingSystem.IsWindows())
				return null;

			var info = new FileInfo(path);

			if (info.LinkTarget != null)
				return $"'{path}' is a symbolic link; the local store uses only its own files.";

			if ((info.UnixFileMode & CredentialsDirectory.OthersAnything) != 0)
				return $"'{path}' is accessible by other users. Run: chmod 600 '{path}'";

			return null;
		}

		static byte[] Serialize(Dictionary<string, (string User, string Password)> entries)
		{
			var buffer = new ArrayBufferWriter<byte>();

			using (var writer = new Utf8JsonWriter(buffer))
			{
				writer.WriteStartObject();

				foreach (var entry in entries.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
				{
					writer.WriteStartObject(entry.Key);
					writer.WriteString("user",     entry.Value.User);
					writer.WriteString("password", entry.Value.Password);
					writer.WriteEndObject();
				}

				writer.WriteEndObject();
			}

			var result = buffer.WrittenSpan.ToArray();
			// Clear() also zeroes the written bytes.
			buffer.Clear();
			return result;
		}

		static Dictionary<string, (string User, string Password)>? Deserialize(byte[] plaintext)
		{
			try
			{
				using var document = JsonDocument.Parse(plaintext);

				if (document.RootElement.ValueKind != JsonValueKind.Object)
					return null;

				var result = new Dictionary<string, (string User, string Password)>(StringComparer.Ordinal);

				foreach (var property in document.RootElement.EnumerateObject())
				{
					if (property.Value.ValueKind != JsonValueKind.Object
						|| !property.Value.TryGetProperty("user", out var user)
						|| !property.Value.TryGetProperty("password", out var password)
						|| user.ValueKind != JsonValueKind.String
						|| password.ValueKind != JsonValueKind.String)
					{
						return null;
					}

					result[property.Name] = (user.GetString()!, password.GetString()!);
				}

				return result;
			}
			catch (JsonException)
			{
				return null;
			}
		}
	}
}
