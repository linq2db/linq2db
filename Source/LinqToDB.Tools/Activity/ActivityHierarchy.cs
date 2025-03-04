using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using LinqToDB.Metrics;

namespace LinqToDB.Tools.Activity
{
	/// <summary>
	/// Collects LinqToDB call hierarchy information.
	/// </summary>
	public class ActivityHierarchy : ActivityBase
	{
		readonly string                  _name;
		readonly Action<string>          _pushReport;
		readonly ActivityHierarchy?      _parent;
		readonly List<ActivityHierarchy> _children = [];

		int _count = 1;

		/// <summary>
		/// Gets or sets the indent string for the hierarchy report.
		/// </summary>
		public string Indent { get; set; } = "  ";

		/// <summary>
		/// <para>
		/// Creates a new instance of the <see cref="ActivityHierarchy"/> class.
		/// Can be used in a factory method for <see cref="ActivityService"/>:
		/// </para>
		/// <code>
		/// ActivityService.AddFactory(activityID =&gt; new ActivityHierarchy(activityID, s =&gt; hierarchyBuilder.AppendLine(s)));
		/// </code>
		/// </summary>
		/// <param name="activityID">One of the <see cref="ActivityID"/> values. </param>
		/// <param name="pushReport">
		/// A delegate that is called to provide a report when the root activity is disposed.
		/// </param>
		public ActivityHierarchy(ActivityID activityID, Action<string> pushReport)
			: base(activityID)
		{
			_pushReport = pushReport;
			_parent     = Current;
			_name       = ActivityStatistics.GetStat(activityID).Name.TrimStart();

			Current = this;

			if (_parent != null)
			{
				if (_parent._children.Count == 0)
				{
					_parent._children.Add(this);
				}
				else
				{
					var p = _parent._children[^1];

					if (p.ActivityID == ActivityID)
						p._count++;
					else
						_parent._children.Add(this);
				}
			}
		}

		static readonly System.Threading.AsyncLocal<ActivityHierarchy?> _currentImpl = new ();

		static ActivityHierarchy? Current
		{
			get => _currentImpl.Value;
			set => _currentImpl.Value = value;
		}

		/// <summary>
		/// Implements Dispose pattern.
		/// </summary>
		public override void Dispose()
		{
			Current = _parent;

			if (_parent == null)
			{
				var sb = new StringBuilder();

				Print(this, "");

				_pushReport(sb.ToString());

				void Print(ActivityHierarchy a, string indent)
				{
					sb
						.Append(indent)
						.Append(a._name);

					if (a._count > 1)
						sb.Append(CultureInfo.InvariantCulture, $" ({a._count})");

					sb.AppendLine();

					foreach (var c in a._children)
						Print(c, indent + Indent);
				}
			}
		}
	}
}
