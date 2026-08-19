#if !NETFRAMEWORK
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ProtoBuf.Grpc.Server;

using Tests.Model;
using Tests.Model.Remote.Grpc;

namespace Tests.Remote.ServerContainer
{
	internal sealed class GrpcServerContainer : ServerContainerBase<TestGrpcLinqService>
	{
		private static string GetServiceUrl(int port) => $"https://localhost:{port}";

		// MTP runs tests as a bare executable, so the ASP.NET Core HTTPS development certificate
		// that `dotnet test` used to provision is unavailable. Bind Kestrel to a throwaway self-signed
		// cert instead — the gRPC client accepts any server cert (DangerousAcceptAnyServerCertificateValidator).
		private static readonly X509Certificate2 _certificate = CreateServerCertificate();

		private static X509Certificate2 CreateServerCertificate()
		{
			using var rsa = RSA.Create(2048);

			var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

			var san = new SubjectAlternativeNameBuilder();
			san.AddDnsName("localhost");
			request.CertificateExtensions.Add(san.Build());

			using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));

			// Round-trip through PKCS#12 so Kestrel's TLS stack can use the private key (required on Windows).
			var pfx = cert.Export(X509ContentType.Pkcs12);
#if NET9_0_OR_GREATER
			return X509CertificateLoader.LoadPkcs12(pfx, null);
#else
			return new X509Certificate2(pfx);
#endif
		}

		protected override TestGrpcLinqService StartHost(int port, Func<string?, MappingSchema?, DataConnection> connectionFactory)
		{
			var service = new TestGrpcLinqService(
				new TestLinqService((c, ms) => connectionFactory(c, ms))
				{
					AllowUpdates    = true,
					RemoteClientTag = "Grpc",
				});

			Startup.GrpcLinqService = service;

			var host = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(
				webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
					webBuilder.ConfigureKestrel(o => o.ConfigureHttpsDefaults(h => h.ServerCertificate = _certificate));
					webBuilder.UseUrls(GetServiceUrl(port));
				}).Build();

			host.Start();

			TestExternals.Log("gRCP host opened");

			return service;
		}

		protected override ITestDataContext CreateClientContext(TestGrpcLinqService service, int port, Func<ITestLinqService, DataOptions, DataOptions> optionBuilder)
		{
			return new TestGrpcDataContext(GetServiceUrl(port), o => optionBuilder(service, o));
		}

		public class Startup
		{
			internal static TestGrpcLinqService? GrpcLinqService;

			public void ConfigureServices(IServiceCollection services)
			{
				if (GrpcLinqService == null)
				{
					throw new InvalidOperationException("Grpc service should be created first");
				}

				services.AddGrpc();
				services.AddCodeFirstGrpc();
				services.AddSingleton(p => GrpcLinqService);

			}

			public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
			{
				app.UseDeveloperExceptionPage();

				app.UseRouting();

				app.UseEndpoints(endpoints =>
				{
					endpoints.MapGrpcService<TestGrpcLinqService>();
				});
			}
		}
	}
}
#endif
