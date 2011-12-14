using System;

namespace LinqToDB.TypeBuilder
{
	[AttributeUsage(AttributeTargets.Property)]
	public class ParameterAttribute : Attribute
	{
		protected ParameterAttribute()
		{
			SetParameters();
		}

		public ParameterAttribute(object parameter1)
		{
			SetParameters(parameter1);
		}

		public ParameterAttribute(
			object parameter1,
			object parameter2)
		{
			SetParameters(parameter1, parameter2);
		}

		public ParameterAttribute(
			object parameter1,
			object parameter2,
			object parameter3)
		{
			SetParameters(parameter1, parameter2, parameter3);
		}

		public ParameterAttribute(
			object parameter1,
			object parameter2,
			object parameter3,
			object parameter4)
		{
			SetParameters(parameter1, parameter2, parameter3, parameter4);
		}
		
		public ParameterAttribute(
			object parameter1,
			object parameter2,
			object parameter3,
			object parameter4,
			object parameter5)
		{
			SetParameters(parameter1, parameter2, parameter3, parameter4, parameter5);
		}

		protected void SetParameters(params object[] parameters)
		{
			_parameters = parameters;
		}

		private object[] _parameters;
		public  object[]  Parameters
		{
			get { return _parameters;  }
		}
	}
}
