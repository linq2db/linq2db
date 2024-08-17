// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public class CustomerQuery : BaseEntity
	{
		public string CompanyName { get; set; } = null!;
		public int OrderCount { get; set; }
		public string SearchTerm { get; set; } = null!;
	}
}
