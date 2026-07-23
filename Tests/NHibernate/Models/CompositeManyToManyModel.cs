using System.Collections.Generic;

using FluentNHibernate.Mapping;

namespace LinqToDB.NHibernate.Tests.Models.CompositeManyToMany
{
	// Entities with COMPOSITE primary keys, linked many-to-many through a junction that carries all four
	// key columns. Exercises the multi-column join predicate synthesized for an m2m association: every
	// key/element column pair must be ANDed and paired in the correct order.
	public class Course
	{
		public Course()
		{
			Students = new HashSet<Student>();
		}

		public virtual int                  DeptId   { get; set; }
		public virtual int                  Number   { get; set; }
		public virtual string               Title    { get; set; } = null!;
		public virtual ICollection<Student> Students { get; set; }

		public override bool Equals(object? obj)
			=> obj is Course other && other.DeptId == DeptId && other.Number == Number;

		public override int GetHashCode()
			=> (DeptId, Number).GetHashCode();
	}

	public class Student
	{
		public virtual int    CampusId { get; set; }
		public virtual int    Roll     { get; set; }
		public virtual string Name     { get; set; } = null!;

		public override bool Equals(object? obj)
			=> obj is Student other && other.CampusId == CampusId && other.Roll == Roll;

		public override int GetHashCode()
			=> (CampusId, Roll).GetHashCode();
	}

	// The junction, mapped as a real entity (composite key over all four columns) so linq2db can query it.
	public class CourseStudent
	{
		public virtual int DeptId   { get; set; }
		public virtual int Number   { get; set; }
		public virtual int CampusId { get; set; }
		public virtual int Roll     { get; set; }

		public override bool Equals(object? obj)
			=> obj is CourseStudent other
				&& other.DeptId == DeptId && other.Number == Number
				&& other.CampusId == CampusId && other.Roll == Roll;

		public override int GetHashCode()
			=> (DeptId, Number, CampusId, Roll).GetHashCode();
	}

	public class CourseMap : ClassMap<Course>
	{
		public CourseMap()
		{
			Table("Courses");
			CompositeId()
				.KeyProperty(x => x.DeptId, "DeptId")
				.KeyProperty(x => x.Number, "Number");
			Map(x => x.Title).Column("Title").Not.Nullable();
			var students = HasManyToMany(x => x.Students).Table("CourseStudent");
			students.ParentKeyColumns.Add("DeptId", "Number");  // junction -> course (composite)
			students.ChildKeyColumns.Add("CampusId", "Roll");   // junction -> student (composite)
		}
	}

	public class StudentMap : ClassMap<Student>
	{
		public StudentMap()
		{
			Table("Students");
			CompositeId()
				.KeyProperty(x => x.CampusId, "CampusId")
				.KeyProperty(x => x.Roll,     "Roll");
			Map(x => x.Name).Column("Name").Not.Nullable();
		}
	}

	public class CourseStudentMap : ClassMap<CourseStudent>
	{
		public CourseStudentMap()
		{
			Table("CourseStudent");
			CompositeId()
				.KeyProperty(x => x.DeptId,   "DeptId")
				.KeyProperty(x => x.Number,   "Number")
				.KeyProperty(x => x.CampusId, "CampusId")
				.KeyProperty(x => x.Roll,     "Roll");
		}
	}
}
