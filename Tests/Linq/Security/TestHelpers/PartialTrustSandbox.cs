using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Tests.Security.TestHelpers
{
	class PartialTrustSandbox : IDisposable
	{
		static readonly PartialTrustSandbox _default = new PartialTrustSandbox("Default Partial Trust Sandbox");
		AppDomain _domain;

		protected PartialTrustSandbox(string domainName, string configurationFile = null)
		{
			var securityConfig = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "CONFIG", "web_mediumtrust.config");
			var permissionXml  = File.ReadAllText(securityConfig).Replace("$AppDir$", Environment.CurrentDirectory);

//#pragma warning disable 0618
			var grantSet = SecurityManager.LoadPolicyLevelFromString(permissionXml, PolicyLevelType.AppDomain).
					GetNamedPermissionSet("ASP.Net");
//#pragma warning restore 0618

			var info = new AppDomainSetup
			{
				ApplicationBase   = Environment.CurrentDirectory,
				ConfigurationFile = configurationFile ?? AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
				PartialTrustVisibleAssemblies = new[]
				{
					// Add conditional APTCA assemblies that you need to access in partial trust here.
					// Do NOT add System.Web here since at least one test relies on it not being treated as conditionally APTCA.
					"System.ComponentModel.DataAnnotations, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9"
				},
			};

			_domain = AppDomain.CreateDomain(domainName, null, info, grantSet, null);
		}

		~PartialTrustSandbox()
		{
			Dispose(false);
		}

		public static PartialTrustSandbox Default
		{
			get { return _default; }
		}

		public T CreateInstance<T>()
		{
			return (T)CreateInstance(typeof(T));
		}

		public object CreateInstance(Type type)
		{
			HandleDisposed();
			return _domain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && _domain != null)
			{
				AppDomain.Unload(_domain);
				_domain = null;
			}
		}

		void HandleDisposed()
		{
			if (_domain == null)
				throw new ObjectDisposedException(null);
		}
	}
}
