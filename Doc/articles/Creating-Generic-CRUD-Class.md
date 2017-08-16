```c#
public static class DBGenericActions
{
    public static void UpdateEntity<T>(T entity) where T : class
    {
        using (var db = new myDB())
        {
            db.Update<T>(entity);
        }
    }

    public static object InsertEntity<T>(T entity) where T : class
    {
        using (var db = new myDB())
        {
            return db.InsertWithIdentity<T>(entity);
        }
    }

    public static void DeleteEntity<T>(T entity) where T : class
    {
        using (var db = new myDB())
        {
            db.Delete<T>(entity);
        }
    }

    public static List<T> GetAllFromEntity<T>() where T : class
    {
        using (var db = new myDB())
        {
            return db.GetTable<T>().ToList();
        }
    }
    
    public static List<T> GetEntitiesByParameters<T>(Func<T, bool> where) where T : class
    {
        using (var db = new nassif_devDB())
        {
            return db.GetTable<T>().Where<T>(where).Where<T>(GetLogicExclusion<T>()).ToList);                    
        }
    }
            
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">linqToDb Table mapped</typeparam>
    /// <param name="pk"> Have to be of the same type of primary key atribute of T table mapped</param>
    /// <returns>T linqToDb mapped class</returns>
    public static T GetEntityByPK<T>(object pk) where T : class
    {
        using (var db = new myDB())
        {
            var pkName =  typeof(T).GetProperties().Where(prop => prop.GetCustomAttributes(typeof(LinqToDB.Mapping.PrimaryKeyAttribute), false).Count() > 0).First();
            var expression = SimpleComparison<T>(pkName.Name, pk);

            return db.GetTable<T>().Where<T>(expression).FirstOrDefault();
        }
    }

    /// <summary>
    /// Excelent to use to get entities by FK
    /// </summary>
    /// <typeparam name="T">Entity To Filter From DB Mapped</typeparam>
    /// <typeparam name="D">Type of property to filter using Equals Comparer</typeparam>
    /// <param name="propertyName">Name of property</param>
    /// <param name="valueToFilter">Value to filter query</param>
    /// <returns>List of T</returns>
    public static List<T> GetAllEntititiesByPropertyValue<T, D>(string propertyName, D valueToFilter)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return GetAllFromEntity<T>();

        var expression = SimpleComparison<T,D>(propertyName, valueToFilter);

        using (var db = new myDB())
        {
            var data = db.GetTable<T>().Where<T>(expression).ToList();
            return data;
        }
    }

    public static Func<T, bool> SimpleComparison<T>(string property, object value) where T : class
    {
        var type = typeof (T);
        var pe = Expression.Parameter(type, "p");
        var propertyReference = Expression.Property(pe, property);
        var constantReference = Expression.Constant(value);

        return Expression.Lambda<Func<T, bool>>
            (Expression.Equal(propertyReference, constantReference),
            new[] { pe }).Compile();
    }

    private static Func<T, bool> SimpleComparison<T,D>(string propertyName, D value ) where T : class
    {
        var type = typeof(T);
        var pe = Expression.Parameter(type, "p");
        var constantReference = Expression.Constant(value);
        var propertyReference = Expression.Property(pe, propertyName);

        return Expression.Lambda<Func<T, bool>>(
            Expression.Equal(propertyReference, constantReference),
            new[] { pe }).Compile();              
    }
}
