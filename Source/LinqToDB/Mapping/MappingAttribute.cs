using System;

#if NETFRAMEWORK
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
#endif

namespace LinqToDB.Mapping
{
	public abstract class MappingAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <see langword="null"/> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string? Configuration { get; set; }

		/// <summary>
		/// Returns mapping attribute id, based on all attribute options.
		/// </summary>
		public abstract string GetObjectID();

#if NETFRAMEWORK
		// "backport" fix for incorrect attribute equality implementation in .NET Framework for inherited attributes
		// https://github.com/dotnet/runtime/issues/6303
		// https://github.com/dotnet/coreclr/pull/6240
		// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Attribute.cs
		//
		// Licensed to the .NET Foundation under one or more agreements.
		// The .NET Foundation licenses this file to you under the MIT license.
		public override int GetHashCode()
		{
			var type = GetType();

			while (type != typeof(Attribute))
			{
				var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				object? vThis = null;

				for (int i = 0; i < fields.Length; i++)
				{
					var fieldValue = fields[i].GetValue(this);

					// The hashcode of an array ignores the contents of the array, so it can produce
					// different hashcodes for arrays with the same contents.
					// Since we do deep comparisons of arrays in Equals(), this means Equals and GetHashCode will
					// be inconsistent for arrays. Therefore, we ignore hashes of arrays.
					if (fieldValue != null && !fieldValue.GetType().IsArray)
						vThis = fieldValue;

					if (vThis != null)
						break;
				}

				if (vThis != null)
					return vThis.GetHashCode();

				type = type.BaseType!;
			}

			return type.GetHashCode();
		}

		public override bool Equals([NotNullWhen(true)] object? obj)
		{
			if (obj == null)
				return false;

			if (GetType() != obj.GetType())
				return false;

			var thisType = GetType();
			object thisObj = this;
			object? thisResult;
			object? thatResult;

			while (thisType != typeof(Attribute))
			{
				var thisFields = thisType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

				for (int i = 0; i < thisFields.Length; i++)
				{
					thisResult = thisFields[i].GetValue(thisObj);
					thatResult = thisFields[i].GetValue(obj);

					if (!AreFieldValuesEqual(thisResult, thatResult))
					{
						return false;
					}
				}

				thisType = thisType.BaseType!;
			}

			return true;
		}

		// Compares values of custom-attribute fields.
		private static bool AreFieldValuesEqual(object? thisValue, object? thatValue)
		{
			if (thisValue == null && thatValue == null)
				return true;
			if (thisValue == null || thatValue == null)
				return false;

			var thisValueType = thisValue.GetType();

			if (thisValueType.IsArray)
			{
				// Ensure both are arrays of the same type.
				if (!thisValueType.Equals(thatValue.GetType()))
				{
					return false;
				}

				var thisValueArray = (Array)thisValue;
				var thatValueArray = (Array)thatValue;
				if (thisValueArray.Length != thatValueArray.Length)
				{
					return false;
				}

				// Attributes can only contain single-dimension arrays, so we don't need to worry about
				// multidimensional arrays.
				for (int j = 0; j < thisValueArray.Length; j++)
				{
					if (!AreFieldValuesEqual(thisValueArray.GetValue(j), thatValueArray.GetValue(j)))
					{
						return false;
					}
				}
			}
			else
			{
				// An object of type Attribute will cause a stack overflow.
				// However, this should never happen because custom attributes cannot contain values other than
				// constants, single-dimensional arrays and typeof expressions.
				if (!thisValue.Equals(thatValue))
					return false;
			}

			return true;
		}
#endif
	}
}
