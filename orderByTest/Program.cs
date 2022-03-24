using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace OrderByTest
{
    class Program
    {
        static void Main(string[] args)
        {
            List<TestClass> testClasses = new List<TestClass>()
            {
                new TestClass(){Title = "Works", Created = DateTime.UtcNow.AddDays(-3)},
                new TestClass(){Title = "Aorks", Created = DateTime.UtcNow.AddDays(-9)},
                new TestClass(){Title = "Sorks", Created = DateTime.UtcNow.AddDays(-5)},
                new TestClass(){Title = "Zorks", Created = DateTime.UtcNow.AddDays(-9).AddHours(3)},
                new TestClass(){Title = "Bbbb"},
                new TestClass(){}
            };

            var queryable = testClasses.AsQueryable();

            queryable.OrderByCustom(nameof(TestClass.Title), false);
            var result = queryable.AsQueryable();

        }
    }

    public static class QueryableExtensions
    {
        public static IOrderedQueryable<TSource> OrderByCustom<TSource>(
            this IQueryable<TSource> query, string propertyName, bool isAscending = true)
        {
            var entityType = typeof(TSource);

            var propertyInfo = entityType.GetProperty(propertyName);
            try
            {
                if (propertyInfo == null)
                {
                    throw new ArgumentNullException($"this property does not belog to " + entityType.Name);
                }
                else
                {
                    ParameterExpression arg = Expression.Parameter(entityType, "x");
                    MemberExpression property = Expression.Property(arg, propertyName);
                    var selector = Expression.Lambda(property, new ParameterExpression[] { arg });

                    //Get System.Linq.Queryable.OrderBy() method.
                    var enumarableType = typeof(Queryable);
                    var methodName = isAscending ? "OrderBy" : "OrderByDescending";
                    var method = enumarableType.GetMethods()
                        .Where(m => m.Name == methodName && m.IsGenericMethodDefinition)
                        .Where(m =>
                        {
                            var parameters = m.GetParameters().ToList();
                            //Put more restriction here to ensure selecting the right overload                
                            return parameters.Count == 2;//overload that has 2 parameters
                        }).Single();
                    //The linq's OrderBy<TSource, TKey> has two generic types, which provided here
                    MethodInfo genericMethod = method
                        .MakeGenericMethod(entityType, propertyInfo.PropertyType);

                    var newQuery = (IOrderedQueryable<TSource>)genericMethod
                        .Invoke(genericMethod, new object[] { query, selector });

                    return newQuery;
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentNullException(ex.Message);
            }
        }
    }

    public class TestClass
    {
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string Title { get; set; }
    }
}