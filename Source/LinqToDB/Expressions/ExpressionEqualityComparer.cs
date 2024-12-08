// BASEDON: https://github.com/aspnet/EntityFrameworkCore/blob/dev/src/EFCore/Query/Internal/ExpressionEqualityComparer.cs

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable SwitchStatementMissingSomeCases
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery
namespace LinqToDB.Expressions
{
	using Extensions;
	using Linq;
	using Reflection;

	/// <summary>
	///     This API supports the linq2db infrastructure and is not intended to be used
	///     directly from your code. This API may change or be removed in future releases.
	/// </summary>
	public sealed class ExpressionEqualityComparer : IEqualityComparer<Expression>
	{
		public static ExpressionEqualityComparer Instance { get; } = new ExpressionEqualityComparer();

		ExpressionEqualityComparer()
		{
		}

		public int GetHashCode(Expression? obj)
		{
			if (obj == null)
			{
				return 0;
			}

			unchecked
			{
				var hashCode = (int)obj.NodeType;

				hashCode += (hashCode * 397) ^ obj.Type.GetHashCode();

				switch (obj.NodeType)
				{
					case ExpressionType.Negate:
					case ExpressionType.NegateChecked:
					case ExpressionType.Not:
					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
					case ExpressionType.ArrayLength:
					case ExpressionType.Quote:
					case ExpressionType.TypeAs:
					case ExpressionType.UnaryPlus:
					case ExpressionType.Throw:
					{
						var unaryExpression = (UnaryExpression)obj;

						if (unaryExpression.Method != null)
						{
							hashCode += hashCode * 397 ^ unaryExpression.Method.GetHashCode();
						}

						hashCode += (hashCode * 397) ^ GetHashCode(unaryExpression.Operand);

						break;
					}
					case ExpressionType.Add:
					case ExpressionType.AddChecked:
					case ExpressionType.Subtract:
					case ExpressionType.SubtractChecked:
					case ExpressionType.Multiply:
					case ExpressionType.MultiplyChecked:
					case ExpressionType.Divide:
					case ExpressionType.Modulo:
					case ExpressionType.And:
					case ExpressionType.AndAlso:
					case ExpressionType.Or:
					case ExpressionType.OrElse:
					case ExpressionType.LessThan:
					case ExpressionType.LessThanOrEqual:
					case ExpressionType.GreaterThan:
					case ExpressionType.GreaterThanOrEqual:
					case ExpressionType.Equal:
					case ExpressionType.NotEqual:
					case ExpressionType.Coalesce:
					case ExpressionType.ArrayIndex:
					case ExpressionType.RightShift:
					case ExpressionType.LeftShift:
					case ExpressionType.ExclusiveOr:
					case ExpressionType.Power:
					case ExpressionType.Assign:
					{
						var binaryExpression = (BinaryExpression)obj;

						hashCode += (hashCode * 397) ^ GetHashCode(binaryExpression.Left);
						hashCode += (hashCode * 397) ^ GetHashCode(binaryExpression.Right);

						break;
					}
					case ExpressionType.TypeIs:
					{
						var typeBinaryExpression = (TypeBinaryExpression)obj;

						hashCode += (hashCode * 397) ^ GetHashCode(typeBinaryExpression.Expression);
						hashCode += (hashCode * 397) ^ typeBinaryExpression.TypeOperand.GetHashCode();

						break;
					}
					case ExpressionType.Constant:
					{
						var constantExpression = (ConstantExpression)obj;

						if (constantExpression.Value != null
							&& constantExpression.Value is not IQueryable 
							&& (constantExpression.Value is string or not IEnumerable))
						{
							hashCode += (hashCode * 397) ^ constantExpression.Value.GetHashCode();
						}

						break;
					}
					case ExpressionType.Parameter:
					{
						var parameterExpression = (ParameterExpression)obj;

						hashCode += hashCode * 397;
						// ReSharper disable once ConditionIsAlwaysTrueOrFalse
						if (parameterExpression.Name != null)
						{
							hashCode ^= parameterExpression.Name.GetHashCode();
						}

						break;
					}
					case ExpressionType.MemberAccess:
					{
						var memberExpression = (MemberExpression)obj;

						hashCode += (hashCode * 397) ^ MemberInfoEqualityComparer.Default.GetHashCode(memberExpression.Member);
						hashCode += (hashCode * 397) ^ GetHashCode(memberExpression.Expression);

						break;
					}
					case ExpressionType.Call:
					{
						var methodCallExpression = (MethodCallExpression)obj;

						hashCode += (hashCode * 397) ^ methodCallExpression.Method.GetHashCode();
						hashCode += (hashCode * 397) ^ GetHashCode(methodCallExpression.Object);
						hashCode += (hashCode * 397) ^ GetHashCode(methodCallExpression.Arguments);

						break;
					}
					case ExpressionType.Lambda:
					{
						var lambdaExpression = (LambdaExpression)obj;

						hashCode += (hashCode * 397) ^ lambdaExpression.ReturnType.GetHashCode();
						hashCode += (hashCode * 397) ^ GetHashCode(lambdaExpression.Body);
						hashCode += (hashCode * 397) ^ GetHashCode(lambdaExpression.Parameters);

						break;
					}
					case ExpressionType.New:
					{
						var newExpression = (NewExpression)obj;

						hashCode += (hashCode * 397) ^ (newExpression.Constructor?.GetHashCode() ?? 0);

						if (newExpression.Members != null)
						{
							for (var i = 0; i < newExpression.Members.Count; i++)
							{
								hashCode += (hashCode * 397) ^ newExpression.Members[i].GetHashCode();
							}
						}

						hashCode += (hashCode * 397) ^ GetHashCode(newExpression.Arguments);

						break;
					}
					case ExpressionType.NewArrayInit:
					case ExpressionType.NewArrayBounds:
					{
						var newArrayExpression = (NewArrayExpression)obj;

						hashCode += (hashCode * 397) ^ GetHashCode(newArrayExpression.Expressions);

						break;
					}
					case ExpressionType.Invoke:
					{
						var invocationExpression = (InvocationExpression)obj;

						hashCode += (hashCode * 397) ^ GetHashCode(invocationExpression.Expression);
						hashCode += (hashCode * 397) ^ GetHashCode(invocationExpression.Arguments);

						break;
					}
					case ExpressionType.MemberInit:
					{
						var memberInitExpression = (MemberInitExpression)obj;

						hashCode += (hashCode * 397) ^ GetHashCode(memberInitExpression.NewExpression);
						hashCode += (hashCode * 397) ^ GetHashCode(memberInitExpression.Bindings);

						break;
					}
					case ExpressionType.ListInit:
					{
						var listInitExpression = (ListInitExpression)obj;

						hashCode += (hashCode * 397) ^ GetHashCode(listInitExpression.NewExpression);

						for (var i = 0; i < listInitExpression.Initializers.Count; i++)
						{
							hashCode += (hashCode * 397) ^ GetHashCode(listInitExpression.Initializers[i].Arguments);
						}

						break;
					}
					case ExpressionType.Conditional:
					{
						var conditionalExpression = (ConditionalExpression)obj;

						hashCode += (hashCode * 397) ^ GetHashCode(conditionalExpression.Test);
						hashCode += (hashCode * 397) ^ GetHashCode(conditionalExpression.IfTrue);
						hashCode += (hashCode * 397) ^ GetHashCode(conditionalExpression.IfFalse);

						break;
					}
					case ExpressionType.Default:
					{
						hashCode += (hashCode * 397) ^ obj.Type.GetHashCode();
						break;
					}
					case ExpressionType.Extension:
					{
						hashCode += (hashCode * 397) ^ obj.GetHashCode();
						break;
					}
					case ChangeTypeExpression.ChangeTypeType:
					{
						hashCode += (hashCode * 397) ^ obj.GetHashCode();
						break;
					}
					case ExpressionType.Block:
					{
						var blockExpression = (BlockExpression)obj;
						for (var i = 0; i < blockExpression.Variables.Count; i++)
						{
							hashCode += (hashCode * 397) ^ GetHashCode(blockExpression.Variables[i]);
						}

						for (var i = 0; i < blockExpression.Expressions.Count; i++)
						{
							hashCode += (hashCode * 397) ^ GetHashCode(blockExpression.Expressions[i]);
						}

						break;
					}
					case ExpressionType.Index:
					{
						var indexExpression = (IndexExpression)obj;
						hashCode += (hashCode * 397) ^ GetHashCode(indexExpression.Object);
						for (var i = 0; i < indexExpression.Arguments.Count; i++)
						{
							hashCode += (hashCode * 397) ^ GetHashCode(indexExpression.Arguments[i]);
						}

						break;
					}
					case ExpressionType.Switch:
					{
						var switchExpression = (SwitchExpression)obj;

						hashCode += (hashCode * 397) ^ GetHashCode(switchExpression.SwitchValue);

						for (var i = 0; i < switchExpression.Cases.Count; i++)
						{
							var switchCase = switchExpression.Cases[i];
							hashCode += (hashCode * 397) ^ GetHashCode(switchCase.TestValues);
							hashCode += (hashCode * 397) ^ GetHashCode(switchCase.Body);
						}

						hashCode += (hashCode * 397) ^ GetHashCode(switchExpression.DefaultBody);

						break;
					}
					default:
						throw new NotImplementedException();
				}

				return hashCode;
			}
		}

		int GetHashCode(IList<MemberBinding> bindings)
		{
			var hashCode = 0;
			for (var i = 0; i < bindings.Count; i++)
			{
				var memberBinding = bindings[i];

				hashCode += (hashCode * 397) ^ memberBinding.Member.GetHashCode();
				hashCode += (hashCode * 397) ^ (int)memberBinding.BindingType;

				switch (memberBinding.BindingType)
				{
					case MemberBindingType.Assignment:
					{
						var memberAssignment = (MemberAssignment)memberBinding;
						hashCode += (hashCode * 397) ^ GetHashCode(memberAssignment.Expression);
						break;
					}
					case MemberBindingType.ListBinding:
					{
						var memberListBinding = (MemberListBinding)memberBinding;
						for (var j = 0; j < memberListBinding.Initializers.Count; j++)
						{
							hashCode += (hashCode * 397) ^
							            GetHashCode(memberListBinding.Initializers[j].Arguments);
						}

						break;
					}
					case MemberBindingType.MemberBinding:
					{
						var memberMemberBinding = (MemberMemberBinding)memberBinding;

						hashCode += (hashCode * 397) ^ GetHashCode(memberMemberBinding.Bindings);

						break;
					}
					default:
						throw new NotImplementedException();
				}
			}

			return hashCode;
		}

		int GetHashCode<T>(IList<T> expressions)
			where T : Expression
		{
			var hashCode = 0;

			for (var i = 0; i < expressions.Count; i++)
			{
				hashCode += (hashCode * 397) ^ GetHashCode(expressions[i]);
			}

			return hashCode;
		}

		public bool Equals(Expression? x, Expression? y) => new ExpressionComparer().Compare(x, y);

		sealed class ExpressionComparer
		{
			ScopedDictionary<ParameterExpression, ParameterExpression>? _parameterScope;

			public bool Compare(Expression? a, Expression? b)
			{
				if (a == b)
				{
					return true;
				}

				if (a == null || b == null)
				{
					return false;
				}

				if (a.NodeType != b.NodeType)
				{
					return false;
				}

				if (a.Type != b.Type)
				{
					return false;
				}

				switch (a.NodeType)
				{
					case ExpressionType.Negate:
					case ExpressionType.NegateChecked:
					case ExpressionType.Not:
					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
					case ExpressionType.ArrayLength:
					case ExpressionType.Quote:
					case ExpressionType.TypeAs:
					case ExpressionType.UnaryPlus:
						return CompareUnary((UnaryExpression)a, (UnaryExpression)b);
					case ExpressionType.Add:
					case ExpressionType.AddChecked:
					case ExpressionType.Subtract:
					case ExpressionType.SubtractChecked:
					case ExpressionType.Multiply:
					case ExpressionType.MultiplyChecked:
					case ExpressionType.Divide:
					case ExpressionType.Modulo:
					case ExpressionType.And:
					case ExpressionType.AndAlso:
					case ExpressionType.Or:
					case ExpressionType.OrElse:
					case ExpressionType.LessThan:
					case ExpressionType.LessThanOrEqual:
					case ExpressionType.GreaterThan:
					case ExpressionType.GreaterThanOrEqual:
					case ExpressionType.Equal:
					case ExpressionType.NotEqual:
					case ExpressionType.Coalesce:
					case ExpressionType.ArrayIndex:
					case ExpressionType.RightShift:
					case ExpressionType.LeftShift:
					case ExpressionType.ExclusiveOr:
					case ExpressionType.Power:
					case ExpressionType.Assign:
						return CompareBinary((BinaryExpression)a, (BinaryExpression)b);
					case ExpressionType.TypeIs:
						return CompareTypeIs((TypeBinaryExpression)a, (TypeBinaryExpression)b);
					case ExpressionType.Conditional:
						return CompareConditional((ConditionalExpression)a, (ConditionalExpression)b);
					case ExpressionType.Default: return true;
					case ExpressionType.Constant:
						return CompareConstant((ConstantExpression)a, (ConstantExpression)b);
					case ExpressionType.Parameter:
						return CompareParameter((ParameterExpression)a, (ParameterExpression)b);
					case ExpressionType.MemberAccess:
						return CompareMemberAccess((MemberExpression)a, (MemberExpression)b);
					case ExpressionType.Call:
						return CompareMethodCall((MethodCallExpression)a, (MethodCallExpression)b);
					case ExpressionType.Lambda:
						return CompareLambda((LambdaExpression)a, (LambdaExpression)b);
					case ExpressionType.New:
						return CompareNew((NewExpression)a, (NewExpression)b);
					case ExpressionType.NewArrayInit:
					case ExpressionType.NewArrayBounds:
						return CompareNewArray((NewArrayExpression)a, (NewArrayExpression)b);
					case ExpressionType.Invoke:
						return CompareInvocation((InvocationExpression)a, (InvocationExpression)b);
					case ExpressionType.MemberInit:
						return CompareMemberInit((MemberInitExpression)a, (MemberInitExpression)b);
					case ExpressionType.ListInit:
						return CompareListInit((ListInitExpression)a, (ListInitExpression)b);
					case ExpressionType.Extension:
						return CompareExtension(a, b);
					case ChangeTypeExpression.ChangeTypeType:
						return a.Equals(b);
					case ExpressionType.Block:
						return CompareBlock((BlockExpression)a, (BlockExpression)b);
					case ExpressionType.Throw:
						return CompareUnary((UnaryExpression)a, (UnaryExpression)b);
					case ExpressionType.Index:
						return CompareIndex((IndexExpression)a, (IndexExpression)b);
					case ExpressionType.Switch:
						return CompareSwitch((SwitchExpression)a, (SwitchExpression)b);
					default:
						throw new NotImplementedException();
				}
			}

			bool CompareIndex(IndexExpression a, IndexExpression b)
			{
				return Equals(a.Indexer, b.Indexer)
					&& Compare(a.Object, b.Object)
					&& CompareExpressionList(a.Arguments, b.Arguments);
			}

			bool CompareSwitch(SwitchExpression a, SwitchExpression b)
			{
				if (! (Compare(a.SwitchValue, b.SwitchValue)
						&& Compare(a.DefaultBody, b.DefaultBody)
						&& Equals(a.Comparison, b.Comparison)
						&& a.Cases.Count == b.Cases.Count))
				{
					return false;
				}

				for (var i = 0; i < a.Cases.Count; i++)
				{
					if (!Compare(a.Cases[i].Body, b.Cases[i].Body))
						return false;
					if (!Compare(a.Cases[i].Body, b.Cases[i].Body))
						return false;
					if (!CompareExpressionList(a.Cases[i].TestValues, b.Cases[i].TestValues))
						return false;
				}

				return true;
			}

			bool CompareUnary(UnaryExpression a, UnaryExpression b)
				=> Equals(a.Method, b.Method)
				   && a.IsLifted == b.IsLifted
				   && a.IsLiftedToNull == b.IsLiftedToNull
				   && Compare(a.Operand, b.Operand);

			bool CompareBinary(BinaryExpression a, BinaryExpression b)
				=> Equals(a.Method, b.Method)
				   && a.IsLifted == b.IsLifted
				   && a.IsLiftedToNull == b.IsLiftedToNull
				   && Compare(a.Left, b.Left)
				   && Compare(a.Right, b.Right);

			bool CompareTypeIs(TypeBinaryExpression a, TypeBinaryExpression b)
				=> a.TypeOperand == b.TypeOperand
				   && Compare(a.Expression, b.Expression);

			bool CompareConditional(ConditionalExpression a, ConditionalExpression b)
				=> Compare(a.Test, b.Test)
				   && Compare(a.IfTrue, b.IfTrue)
				   && Compare(a.IfFalse, b.IfFalse);

			static bool CompareConstant(ConstantExpression a, ConstantExpression b)
			{
				if (a.Value == b.Value)
				{
					return true;
				}

				if (a.Value == null
					|| b.Value == null)
				{
					return false;
				}

				if (a.Value is EnumerableQuery
					&& b.Value is EnumerableQuery)
				{
					return false; // EnumerableQueries are opaque
				}

				if (a.Value is IEnumerable ae && b.Value is IEnumerable be)
				{
					var enum1 = ae.GetEnumerator();
					var enum2 = be.GetEnumerator();
					using (enum1 as IDisposable)
					using (enum2 as IDisposable)
					{
						while (enum1.MoveNext())
						{
							if (!enum2.MoveNext() || !Equals(enum1.Current, enum2.Current))
								return false;
						}

						if (enum2.MoveNext())
							return false;
					}

					return true;
				}

				if (typeof(ExpressionQuery<>).IsSameOrParentOf(a.GetType())
					&& typeof(ExpressionQuery<>).IsSameOrParentOf(b.GetType())
					&& a.Value.GetType() == b.Value.GetType())
				{
					return true;
				}

				return Equals(a.Value, b.Value);
			}

			bool CompareParameter(ParameterExpression a, ParameterExpression b)
			{
				if (_parameterScope != null)
				{
					if (_parameterScope.TryGetValue(a, out var mapped))
					{
						return mapped!.Name == b.Name
							   && mapped.Type == b.Type;
					}
				}

				return a.Name == b.Name
					   && a.Type == b.Type;
			}

			bool CompareMemberAccess(MemberExpression a, MemberExpression b)
				=> MemberInfoEqualityComparer.Default.Equals(a.Member, b.Member)
				   && Compare(a.Expression, b.Expression);

			bool CompareMethodCall(MethodCallExpression a, MethodCallExpression b)
				=> Equals(a.Method, b.Method)
				   && Compare(a.Object, b.Object)
				   && CompareExpressionList(a.Arguments, b.Arguments);

			bool CompareLambda(LambdaExpression a, LambdaExpression b)
			{
				var n = a.Parameters.Count;

				if (b.Parameters.Count != n)
				{
					return false;
				}

				// all must have same type
				for (var i = 0; i < n; i++)
				{
					if (a.Parameters[i].Type != b.Parameters[i].Type)
					{
						return false;
					}
				}

				var save = _parameterScope;

				_parameterScope = new ScopedDictionary<ParameterExpression, ParameterExpression>(_parameterScope);

				try
				{
					for (var i = 0; i < n; i++)
					{
						_parameterScope.Add(a.Parameters[i], b.Parameters[i]);
					}

					return Compare(a.Body, b.Body);
				}
				finally
				{
					_parameterScope = save;
				}
			}

			bool CompareNew(NewExpression a, NewExpression b)
				=> Equals(a.Constructor, b.Constructor)
				   && CompareExpressionList(a.Arguments, b.Arguments)
				   && CompareMemberList(a.Members, b.Members);

			bool CompareExpressionList(IReadOnlyList<Expression>? a, IReadOnlyList<Expression>? b)
			{
				if (Equals(a, b))
				{
					return true;
				}

				if (a == null
					|| b == null)
				{
					return false;
				}

				if (a.Count != b.Count)
				{
					return false;
				}

				for (int i = 0, n = a.Count; i < n; i++)
				{
					if (!Compare(a[i], b[i]))
					{
						return false;
					}
				}

				return true;
			}

			static bool CompareMemberList(IReadOnlyList<MemberInfo>? a, IReadOnlyList<MemberInfo>? b)
			{
				if (ReferenceEquals(a, b))
				{
					return true;
				}

				if (a == null
					|| b == null)
				{
					return false;
				}

				if (a.Count != b.Count)
				{
					return false;
				}

				for (int i = 0, n = a.Count; i < n; i++)
				{
					if (!Equals(a[i], b[i]))
					{
						return false;
					}
				}

				return true;
			}

			bool CompareNewArray(NewArrayExpression a, NewArrayExpression b)
				=> CompareExpressionList(a.Expressions, b.Expressions);

			bool CompareExtension(Expression a, Expression b)
			{
				return a.Equals(b);
			}

			bool CompareBlock(BlockExpression a, BlockExpression b)
			{
				return CompareExpressionList(a.Variables, b.Variables) && CompareExpressionList(b.Expressions, a.Expressions);
			}

			bool CompareInvocation(InvocationExpression a, InvocationExpression b)
				=> Compare(a.Expression, b.Expression)
				   && CompareExpressionList(a.Arguments, b.Arguments);

			bool CompareMemberInit(MemberInitExpression a, MemberInitExpression b)
				=> Compare(a.NewExpression, b.NewExpression)
				   && CompareBindingList(a.Bindings, b.Bindings);

			bool CompareBindingList(IReadOnlyList<MemberBinding>? a, IReadOnlyList<MemberBinding>? b)
			{
				if (ReferenceEquals(a, b))
				{
					return true;
				}

				if (a == null
					|| b == null)
				{
					return false;
				}

				if (a.Count != b.Count)
				{
					return false;
				}

				for (int i = 0, n = a.Count; i < n; i++)
				{
					if (!CompareBinding(a[i], b[i]))
					{
						return false;
					}
				}

				return true;
			}

			bool CompareBinding(MemberBinding? a, MemberBinding? b)
			{
				if (a == b)
				{
					return true;
				}

				if (a == null
					|| b == null)
				{
					return false;
				}

				if (a.BindingType != b.BindingType)
				{
					return false;
				}

				if (!Equals(a.Member, b.Member))
				{
					return false;
				}

				return a.BindingType switch
				{
					MemberBindingType.Assignment	=> CompareMemberAssignment((MemberAssignment)a, (MemberAssignment)b),
					MemberBindingType.ListBinding	=> CompareMemberListBinding((MemberListBinding)a, (MemberListBinding)b),
					MemberBindingType.MemberBinding => CompareMemberMemberBinding((MemberMemberBinding)a, (MemberMemberBinding)b),
					_                               => throw new NotImplementedException(),
				};
			}

			bool CompareMemberAssignment(MemberAssignment a, MemberAssignment b)
				=> Equals(a.Member, b.Member)
				   && Compare(a.Expression, b.Expression);

			bool CompareMemberListBinding(MemberListBinding a, MemberListBinding b)
				=> Equals(a.Member, b.Member)
				   && CompareElementInitList(a.Initializers, b.Initializers);

			bool CompareMemberMemberBinding(MemberMemberBinding a, MemberMemberBinding b)
				=> Equals(a.Member, b.Member)
				   && CompareBindingList(a.Bindings, b.Bindings);

			bool CompareListInit(ListInitExpression a, ListInitExpression b)
				=> Compare(a.NewExpression, b.NewExpression)
				   && CompareElementInitList(a.Initializers, b.Initializers);

			bool CompareElementInitList(IReadOnlyList<ElementInit>? a, IReadOnlyList<ElementInit>? b)
			{
				if (ReferenceEquals(a, b))
				{
					return true;
				}

				if (a == null || b == null)
				{
					return false;
				}

				if (a.Count != b.Count)
				{
					return false;
				}

				for (int i = 0, n = a.Count; i < n; i++)
				{
					if (!CompareElementInit(a[i], b[i]))
					{
						return false;
					}
				}

				return true;
			}

			bool CompareElementInit(ElementInit a, ElementInit b)
				=> Equals(a.AddMethod, b.AddMethod)
				   && CompareExpressionList(a.Arguments, b.Arguments);

			sealed class ScopedDictionary<TKey, TValue>
				where TKey : notnull
			{
				readonly ScopedDictionary<TKey, TValue>? _previous;
				readonly Dictionary<TKey, TValue>        _map;

				public ScopedDictionary(ScopedDictionary<TKey, TValue>? previous)
				{
					_previous = previous;
					_map = new Dictionary<TKey, TValue>();
				}

				public void Add(TKey key, TValue value) => _map.Add(key, value);

				public bool TryGetValue(TKey key, out TValue? value)
				{
					for (var scope = this; scope != null; scope = scope._previous!)
					{
						if (scope._map.TryGetValue(key, out value))
						{
							return true;
						}
					}

					value = default;

					return false;
				}
			}
		}
	}
}
