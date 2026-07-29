// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace LinqToDB.NHibernate.Tests.Models.Northwind
{
	public class CustomerQuery : BaseEntity
	{
		public virtual string CompanyName { get; set; } = null!;
		public virtual int    OrderCount  { get; set; }
		public virtual string SearchTerm  { get; set; } = null!;
	}
}
