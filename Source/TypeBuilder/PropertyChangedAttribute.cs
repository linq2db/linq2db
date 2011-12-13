using System;
using LinqToDB.Common;
using LinqToDB.TypeBuilder.Builders;

namespace LinqToDB.TypeBuilder
{
	/// <summary>
	/// This attribute allows to control generation of PropertyChanged notification at class level 
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
	public sealed class PropertyChangedAttribute : AbstractTypeBuilderAttribute
	{
		/// <summary>
		/// Specifies default generation options should be used for PropertyChanged notification
		/// </summary>
		public PropertyChangedAttribute()
			:this(Common.Configuration.NotifyOnEqualSet)
		{
		}

		/// <summary>
		///	This constructor allows control of PropertyChanged code generation
		/// </summary>
		/// <param name="notifyOnEqualSet">See <see cref="NotifyOnEqualSet"/></param>
		public PropertyChangedAttribute(bool notifyOnEqualSet)
			:this(notifyOnEqualSet, true)
		{
		}

		/// <summary>
		/// This constructor allows control of PropertyChanged code generation
		/// </summary>
		/// <param name="notifyOnEqualSet">See <see cref="NotifyOnEqualSet"/></param>
		/// <param name="useReferenceEquals">See <see cref="UseReferenceEquals"/></param>
		public PropertyChangedAttribute(bool notifyOnEqualSet, bool useReferenceEquals)
			:this(notifyOnEqualSet, useReferenceEquals, true)
		{
		}

		/// <summary>
		/// This constructor allows control of PropertyChanged code generation
		/// </summary>
		/// <param name="notifyOnEqualSet">See <see cref="NotifyOnEqualSet"/></param>
		/// <param name="useReferenceEquals">See <see cref="UseReferenceEquals"/></param>
		/// <param name="skipSetterOnNoChange">See <see cref="SkipSetterOnNoChange"/></param>
		public PropertyChangedAttribute(bool notifyOnEqualSet, bool useReferenceEquals, bool skipSetterOnNoChange)
		{
			_notifyOnEqualSet     = notifyOnEqualSet;
			_useReferenceEquals   = useReferenceEquals;
			_skipSetterOnNoChange = skipSetterOnNoChange;
		}

		private bool _notifyOnEqualSet;
		/// <summary>
		/// Controls whether OnPropertyChanged notifications are sent when current value is same as new one.
		/// 
		/// Default value controlled via <see cref="Configuration.NotifyOnEqualSet"/> and by default is set to false
		/// </summary>
		public  bool  NotifyOnEqualSet
		{
			get { return _notifyOnEqualSet;  }
			set { _notifyOnEqualSet = value; }
		}

		private bool _useReferenceEquals;
		/// <summary>
		/// Specifies if <see cref="Object.ReferenceEquals">Object.ReferenceEquals</see> should be used for equality comparison of current and new value
		/// for reference types. If value type implements op_Inequality, UseReferenceEquals is ignored.
		/// </summary>
		public  bool  UseReferenceEquals
		{
			get { return _useReferenceEquals;  }
			set { _useReferenceEquals = value; }
		}

		private bool _skipSetterOnNoChange;
		/// <summary>
		/// Specifies whether call to setter is made when current value is same as new one
		/// </summary>
		public  bool  SkipSetterOnNoChange
		{
			get { return _skipSetterOnNoChange;  }
			set { _skipSetterOnNoChange = value; }
		}

		public override IAbstractTypeBuilder TypeBuilder
		{
			get { return new PropertyChangedBuilder(_notifyOnEqualSet, _useReferenceEquals, _skipSetterOnNoChange); }
		}
	}
}
