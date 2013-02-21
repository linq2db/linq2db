using System;
using System.ComponentModel.DataAnnotations;

namespace T4Model.Tests
{
	partial class TestClass1
	{
		partial class CustomValidator
		{
			public static ValidationResult ValidateEditableLong1(TestClass1 obj)
			{
				return ValidationResult.Success;
			}

			public static ValidationResult ValidateEditableInt1(TestClass1 obj)
			{
				return ValidationResult.Success;
			}
		}
	}
}
