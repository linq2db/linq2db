using System;

using LinqToDB;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

using Tests.Model;

namespace Tests.Remote.ServerContainer
{
	public interface IServerContainer
	{
		bool KeepSamePortBetweenThreads { get; set; }

		ITestDataContext Prepare(MappingSchema? ms, IInterceptor? interceptor, string configuration, Func<DataOptions,DataOptions>? optionBuilder);
	}
}
