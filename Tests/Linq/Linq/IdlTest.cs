using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data.DataProvider;
using LinqToDB.Data.Linq;
using LinqToDB.Data.Sql.SqlProvider;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
    using Model;

    [TestFixture]
    public class IdlTest : TestBase
    {
        #region PersonWithId

        public interface IHasID
        {
            int ID { get; set; }
        }

        [TableName(Name = "Person")]
        public class PersonWithId : IHasID
        {
            public PersonWithId()
            {
            }

            public PersonWithId(int id)
            {
                ID = id;
            }

            public PersonWithId(int id, string firstName)
            {
                ID = id;
                FirstName = firstName;
            }

            [Identity, PrimaryKey]
            [SequenceName("Firebird", "PersonID")]
            [MapField("PersonID")]
            public int ID { get; set; }
            public string FirstName { get; set; }
            public string LastName;
            [Nullable]
            public string MiddleName;
            public Gender Gender;

            [MapIgnore]
            public string Name { get { return FirstName + " " + LastName; } }

            public override bool Equals(object obj)
            {
                return Equals(obj as PersonWithId);
            }

            public bool Equals(PersonWithId other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return
                    other.ID == ID &&
                    Equals(other.LastName, LastName) &&
                    Equals(other.MiddleName, MiddleName) &&
                    other.Gender == Gender &&
                    Equals(other.FirstName, FirstName);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var result = ID;
                    result = (result * 397) ^ (LastName != null ? LastName.GetHashCode() : 0);
                    result = (result * 397) ^ (MiddleName != null ? MiddleName.GetHashCode() : 0);
                    result = (result * 397) ^ Gender.GetHashCode();
                    result = (result * 397) ^ (FirstName != null ? FirstName.GetHashCode() : 0);
                    return result;
                }
            }
        }
        #endregion

        #region ObjectId

        public struct ObjectId
        {
            public ObjectId(int value)
            {
                m_value = value;
            }

            private int m_value;

            public int Value
            {
                get { return m_value; }
                set { m_value = value; }
            }

            public static implicit operator int(ObjectId val)
            {
                return val.m_value;
            }
        }

        public struct NullableObjectId
        {
            public NullableObjectId(int? value)
            {
                m_value = value;
            }

            private int? m_value;

            public int? Value
            {
                get { return m_value; }
                set { m_value = value; }
            }

            public static implicit operator int?(NullableObjectId val)
            {
                return val.m_value;
            }
        }
        #endregion

        [Test]
        public void TestComplexExpression()
        {
            // failed with LinqToDB.Data.Linq.LinqException : 'new StationObjectId() {Value = ConvertNullable(child.ChildID)}' 
            //   cannot be converted to SQL.
            ForMySqlProvider(
                db =>
                {
                    var source = from child in db.GrandChild
                                 select
                                     new
                                     {
                                              NullableId = new NullableObjectId { Value = child.ChildID }
                                     };

                    var query = from e in source where e.NullableId == 1 select e;

                    var result = query.ToArray();
                    Assert.That(result, Is.Not.Null);
                });
        }


        [Test]
        public void TestJoin()
        {
            // failed with System.ArgumentOutOfRangeException : Index was out of range. Must be non-negative and less than 
            //   the size of the collection.
            // Parameter name: index
            ForMySqlProvider(
                db =>
                {
                    var source = from p1 in db.Person
                                 join p2 in db.Person on p1.ID equals p2.ID
                                 select
                                     new { ID1 = new ObjectId { Value = p1.ID }, FirstName2 = p2.FirstName, };

                    var query = from p1 in source select p1.ID1.Value;

                    var result = query.ToArray();
                    Assert.That(result, Is.Not.Null);
                });
        }

        [Test]
        public void TestNullableExpression()
        {
            // failed with System.NullReferenceException : Object reference not set to an instance of an object.
            ForMySqlProvider(
                db =>
                {
                    var source = from obj in db.Person select new { Id = obj.ID, };

                    // fails for bool?, double?, int32?, int64?, string
                    // works for byte?, int16?, DateTime? 
                    double? @p1 = null;

                    var r = from c in source where @p1 != null select c;

                    Assert.That(r.ToArray(), Is.Not.Null);
                });
        }

        [Test]
        public void TestLookupWithInterfaceProperty()
        {
            ForMySqlProvider(
                db =>
                    {
                        var r = GetById<PersonWithId>(db, 1).SingleOrDefault();
                        Assert.That(r, Is.Not.Null);
                    });
        }

        #region ObjectExt

        public abstract class ObjectWithId
        {
            public ObjectId Id;
        }

        public class ParentEx : ObjectWithId
        {
            public int? Value1;
        }

        #endregion

        [Test]
        public void TestForObjectExt()
        {
            ForMySqlProvider(db =>
                {
                    var r = from p in db.Parent
                                select new ParentEx
                                {
                                    Id = new ObjectId { Value = p.ParentID },
                                    Value1 = p.Value1,
                                };
                    Assert.That(r.ToArray(), Is.Not.Null);
                });
        }

        private void getData(ITestDataContext db, IEnumerable<int?> d, IEnumerable<int?> compareWith)
        {
            var r1 = db.GrandChild
                .Where(x => d.Contains(x.ParentID))
                .GroupBy(x => x.ChildID, x => x.GrandChildID)
                .ToList();
            foreach (var group in r1)
            {
                Assert.That(compareWith.Any(x => group.Contains(x)), Is.True);
            }
        }

        [Test]
        public void TestForGroupBy()
        {
            ForMySqlProvider(db =>
                {
                    /* no error in first call */
                    getData(db, new List<int?> { 2 }, new List<int?> { 211, 212, 221, 222 });

                    /* error in second and more calls */
                    /*
                     * GROUP BY select clause is correct
                        SELECT x.ChildID FROM GrandChild x WHERE x.ParentID IN (3) GROUP BY x.ChildID

                     * But next SELECT clause contains "x.ParentID IN (2)" instead "x.ParentID IN (3)"
                        -- DECLARE ?p1 Int32
                        -- SET ?p1 = 31
                        SELECT x.GrandChildID FROM GrandChild x WHERE x.ParentID IN (2) AND x.ChildID = ?p1
                     */
                    getData(db, new List<int?> { 3 }, new List<int?> { 311, 312, 313, 321, 333 });

                });
        }

        [Test]
        public void TestLinqMax()
        {
            ForMySqlProvider(
                db =>
                    {
                        Assert.That(db.Patient.Where(x => x.PersonID < 0).Select(x => (int?)x.PersonID).Max(), Is.Null);
                        Assert.That(db.Patient.Where(x => x.PersonID < 0).Max(x => (int?)x.PersonID), Is.Null);
                        Assert.Catch<InvalidOperationException>(
                            () => db.Patient.Where(x => x.PersonID < 0).Select(x => x.PersonID).Max());
                        Assert.Catch<InvalidOperationException>(
                            () => db.Patient.Where(x => x.PersonID < 0).Max(x => x.PersonID));
                    });
        }

        [Test]
        public void TestConvertFunction()
        {
            ForMySqlProvider(
                db =>
                {
                    var ds = new IdlPatientSource(db);
                    var r1 = ds.Patients().ToList();
                    var r2 = ds.Persons().ToList();

                    Assert.That(r1, Is.Not.Empty);
                    Assert.That(r2, Is.Not.Empty);

                    var r3 = ds.Patients().ToIdlPatientEx(ds);
                    var r4 = r3.ToList();
                    Assert.That(r4, Is.Not.Empty);
                });
        }

        [Test]
        public void TestJoinOrder()
        {
            ForMySqlProvider(
                db =>
                    {
                        var source = new IdlPatientSource(db);

                        // Success when use result from second JOIN
                        var query1 = from p1 in source.GrandChilds()
                                     join p2 in source.Persons() on p1.ParentID equals p2.Id
                                     join p3 in source.Persons() on p1.ChildID equals p3.Id
                                     select
                                         new
                                             {
                                                 p1.ChildID, 
                                                 p1.ParentID,
                                                 //Parent = p2,
                                                 Child = p3,
                                             };
                        var data1 = query1.ToList();

                        // Fail when use result from first JOIN
                        var query2 = from p1 in source.GrandChilds()
                                    join p2 in source.Persons() on p1.ParentID equals p2.Id
                                    join p3 in source.Persons() on p1.ChildID equals p3.Id
                                    select
                                        new
                                        {
                                            p1.ChildID,
                                            p1.ParentID,
                                            Parent = p2,
                                            //Child = p3,
                                        };
                        var data2 = query2.ToList();
                    });
        }

        private static IQueryable<T> GetById<T>(ITestDataContext db, int id) where T : class, IHasID
        {
            return db.GetTable<T>().Where(obj => obj.ID == id);
        }

        private void ForMySqlProvider(Action<ITestDataContext> func)
        {
            ForEachProvider(Providers.Select(p => p.Name).Except(new[] { ProviderName.MySql }).ToArray(), func);
        }

        [Test]
        public void ImplicitCastTest()
        {
            ForMySqlProvider(db =>
            {
                var people =
                    from p in db.Person
                    select new IdlPerson
                    {
                        Id   = new ObjectId { Value = p.ID },
                        Name = p.FirstName
                    };

                var sql1 = (from p in people where p.Id       == 1 select p).ToString();
                var sql2 = (from p in people where p.Id.Value == 1 select p).ToString();

                Assert.That(sql1, Is.EqualTo(sql2));
            });
        }
    }

    #region TestConvertFunction classes

    public class IdlPatient
    {
        public IdlTest.ObjectId Id { get; set; }
    }

    public class IdlPerson
    {
        public IdlTest.ObjectId Id { get; set; }
        public string Name { get; set; }
    }

    public class IdlGrandChild
    {
        public IdlTest.ObjectId ParentID { get; set; }
        public IdlTest.ObjectId ChildID { get; set; }
        public IdlTest.ObjectId GrandChildID { get; set; }
    }

    public class IdlPatientEx : IdlPatient
    {
        public IdlPerson Person { get; set; }
    }

    public class IdlPatientSource
    {
        private readonly ITestDataContext m_dc;

        public IdlPatientSource(ITestDataContext dc)
        {
            m_dc = dc;
        }

        public IQueryable<IdlGrandChild> GrandChilds()
        {
                return m_dc.GrandChild.Select(x => new IdlGrandChild
                    {
                        ChildID = new IdlTest.ObjectId {Value = x.ChildID.Value},
                        GrandChildID = new IdlTest.ObjectId { Value = x.GrandChildID.Value },
                        ParentID = new IdlTest.ObjectId { Value = x.ParentID.Value }
                    });
        }

        public IQueryable<IdlPatient> Patients()
        {
            return m_dc.Patient.Select(x => new IdlPatient { Id = new IdlTest.ObjectId { Value = x.PersonID }, });
        }

        public IQueryable<IdlPerson> Persons()
        {
            return
                m_dc.Person.Select(
                    x => new IdlPerson { Id = new IdlTest.ObjectId { Value = x.ID }, Name = x.FirstName, });
        }
    }

    public static class IdlPersonConverterExtensions
    {
        public static IEnumerable<IdlPatientEx> ToIdlPatientEx(this IQueryable<IdlPatient> list, IdlPatientSource source)
        {
            return from x in list
                   join person in source.Persons() on x.Id.Value equals person.Id.Value
                   select new IdlPatientEx
                       {
                           Id = x.Id,
                           Person = new IdlPerson { Id = new IdlTest.ObjectId { Value = person.Id }, Name = person.Name,},
                       };
        }
    }

    #endregion
}