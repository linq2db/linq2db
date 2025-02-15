﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public partial class NorthwindData
	{
		private readonly Customer[] _customers;
		private readonly CustomerView[] _customerViews;
		private readonly Employee[] _employees;
		private readonly Product[] _products;
		private readonly Order[] _orders;
		private readonly OrderDetail[] _orderDetails;

		public NorthwindData()
		{
			_customers = CreateCustomers();
			_employees = CreateEmployees();
			_products = CreateProducts();
			_orders = CreateOrders();
			_orderDetails = CreateOrderDetails();

			var customerViews = new List<CustomerView>();

			foreach (var customer in _customers)
			{
				customer.Orders = new List<Order>();

				customerViews.Add(
					new CustomerView
					{
						Address = customer.Address,
						City = customer.City,
						CompanyName = customer.CompanyName,
						ContactName = customer.ContactName,
						ContactTitle = customer.ContactTitle
					});
			}

			_customerViews = customerViews.ToArray();

			foreach (var product in _products)
			{
				product.OrderDetails = new List<OrderDetail>();
			}

 
			foreach (var orderDetail in _orderDetails)
			{
				var order = _orders.First(o => o.OrderId == orderDetail.OrderId);
				orderDetail.Order = order;
				order.OrderDetails.Add(orderDetail);

				var product = _products.First(p => p.ProductId == orderDetail.ProductId);
				orderDetail.Product = product;
				product.OrderDetails.Add(orderDetail);
			}
		}

		public IQueryable<TEntity> Set<TEntity>()
			where TEntity : class
		{
			if (typeof(TEntity) == typeof(Customer))
			{
				return new AsyncEnumerable<TEntity>(_customers.Cast<TEntity>());
			}

			if (typeof(TEntity) == typeof(Employee))
			{
				return new AsyncEnumerable<TEntity>(_employees.Cast<TEntity>());
			}

			if (typeof(TEntity) == typeof(Order))
			{
				return new AsyncEnumerable<TEntity>(_orders.Cast<TEntity>());
			}

			if (typeof(TEntity) == typeof(OrderDetail))
			{
				return new AsyncEnumerable<TEntity>(_orderDetails.Cast<TEntity>());
			}

			if (typeof(TEntity) == typeof(Product))
			{
				return new AsyncEnumerable<TEntity>(_products.Cast<TEntity>());
			}

			if (typeof(TEntity) == typeof(CustomerView))
			{
				return new AsyncEnumerable<TEntity>(_customerViews.Cast<TEntity>());
			}

			throw new InvalidOperationException(FormattableString.Invariant($"Invalid entity type: {typeof(TEntity)}"));
		}

		public static void Seed(DbContext context)
		{
			AddEntities(context);

			context.SaveChanges();
		}

		public static Task SeedAsync(DbContext context)
		{
			AddEntities(context);

			return context.SaveChangesAsync();
		}

		private static void AddEntities(DbContext context)
		{
			context.Set<Customer>().AddRange(CreateCustomers());

			var titleProperty = context.Model.FindEntityType(typeof(Employee))!.FindProperty("Title")!;
			foreach (var employee in CreateEmployees())
			{
				context.Set<Employee>().Add(employee);
#pragma warning disable EF1001 // Internal EF Core API usage.
				context.Entry(employee).GetInfrastructure()[titleProperty] = employee.Title;
#pragma warning restore EF1001 // Internal EF Core API usage.
			}

			context.Set<Order>().AddRange(CreateOrders());
			context.Set<Category>().AddRange(CreateCategories());
			context.Set<Supplier>().AddRange(CreateSupliers());
			context.Set<Shipper>().AddRange(CreateShippers());
			context.Set<Product>().AddRange(CreateProducts());
			context.Set<OrderDetail>().AddRange(CreateOrderDetails());
		}

		private sealed class AsyncEnumerable<T> : IAsyncQueryProvider, IOrderedQueryable<T>
		{
			private readonly EnumerableQuery<T> _enumerableQuery;

			public AsyncEnumerable(IEnumerable<T> enumerable)
			{
				_enumerableQuery = new EnumerableQuery<T>(enumerable);
			}

			private AsyncEnumerable(Expression expression)
			{
				_enumerableQuery = new EnumerableQuery<T>(expression);
			}

			public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
				=> new AsyncEnumerable<TElement>(RewriteShadowPropertyAccess(expression));

			public TResult Execute<TResult>(Expression expression)
				=> ((IQueryProvider)_enumerableQuery)
					.Execute<TResult>(RewriteShadowPropertyAccess(expression));

			public IEnumerator<T> GetEnumerator() => ((IQueryable<T>)_enumerableQuery).GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			public Expression Expression => ((IQueryable)_enumerableQuery).Expression;
			public Type ElementType => typeof(T);
			public IQueryProvider Provider => this;

			private static Expression RewriteShadowPropertyAccess(Expression expression)
				=> new ShadowStateAccessRewriter().Visit(expression);

			private sealed class ShadowStateAccessRewriter : ExpressionVisitor
			{
				[return: NotNullIfNotNull(nameof(expr))]
				static Expression? RemoveConvert(Expression? expr)
				{
					while (expr?.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
					{
						expr = ((UnaryExpression) expr).Operand;
					}

					return expr;
				}

				protected override Expression VisitMethodCall(MethodCallExpression node)
					=> node.Method.IsEFPropertyMethod()
						? Expression.Property(
							RemoveConvert(node.Arguments[0]),
							Expression.Lambda<Func<string>>(node.Arguments[1]).Compile().Invoke())
						: base.VisitMethodCall(node);
			}

			public IQueryable CreateQuery(Expression expression)
			{
				throw new NotImplementedException();
			}

			public object Execute(Expression expression)
			{
				throw new NotImplementedException();
			}

			public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}
		}
	}
}
