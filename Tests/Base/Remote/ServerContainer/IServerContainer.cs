﻿using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using Tests.Model;

namespace Tests.Remote.ServerContainer
{
	public interface IServerContainer
	{
		bool KeepSamePortBetweenThreads { get; set; }

		ITestDataContext CreateContext(MappingSchema? ms, string configuration, Func<DataOptions,DataOptions>? optionBuilder, Func<string, MappingSchema?, DataConnection> connectionFactory);
	}
}
