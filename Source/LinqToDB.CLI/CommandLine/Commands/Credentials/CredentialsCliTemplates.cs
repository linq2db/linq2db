using System;
using System.IO;
using System.Text;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// The scripts written by <c>credentials cli init --store keyring|gpg</c>: POSIX sh credentials CLIs over a secret
	/// store's command-line tool. They contain no secrets and are the user's to review and edit.
	/// </summary>
	internal static class CredentialsCliTemplates
	{
		public static string? Get(string store)
		{
			var resourceName = store switch
			{
				"keyring" => "LinqToDB.CLI.CredentialsCli.credentials-keyring.sh",
				"gpg"     => "LinqToDB.CLI.CredentialsCli.credentials-gpg.sh",
				_         => null,
			};

			if (resourceName == null)
				return null;

			using var stream = typeof(CredentialsCliTemplates).Assembly.GetManifestResourceStream(resourceName)
				?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
			using var reader = new StreamReader(stream, Encoding.UTF8);

			// sh needs LF line endings whatever the checkout of the template used.
			return reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
		}

		/// <summary>The tool a generated script runs.</summary>
		public static string GetTool(string store)
		{
			return string.Equals(store, "gpg", StringComparison.Ordinal) ? "pass" : "secret-tool";
		}

		/// <summary>How to install the tool a generated script runs.</summary>
		public static string GetToolPackage(string store)
		{
			return string.Equals(store, "gpg", StringComparison.Ordinal)
				? "package 'pass' on Linux, 'brew install pass' on macOS, plus an initialized password store: pass init <gpg-id>"
				: "package 'libsecret-tools' on Debian/Ubuntu, 'libsecret' on Fedora/Arch";
		}
	}
}
