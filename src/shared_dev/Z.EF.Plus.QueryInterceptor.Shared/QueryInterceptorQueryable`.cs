﻿// Description: Entity Framework Bulk Operations & Utilities (EF Bulk SaveChanges, Insert, Update, Delete, Merge | LINQ Query Cache, Deferred, Filter, IncludeFilter, IncludeOptimize | Audit)
// Website & Documentation: https://github.com/zzzprojects/Entity-Framework-Plus
// Forum & Issues: https://github.com/zzzprojects/EntityFramework-Plus/issues
// License: https://github.com/zzzprojects/EntityFramework-Plus/blob/master/LICENSE
// More projects: http://www.zzzprojects.com/
// Copyright © ZZZ Projects Inc. 2014 - 2016. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;

namespace Z.EntityFramework.Plus
{
    /// <summary>A class for query include filter parent queryable.</summary>
    /// <typeparam name="T">The type of elements of the query.</typeparam>
#if NET45
    public class QueryInterceptorQueryable<T> : IOrderedQueryable<T>, IDbAsyncEnumerable<T>
#else
    public class QueryInterceptorQueryable<T> : IOrderedQueryable<T>
#endif
    {
        /// <summary>Constructor.</summary>
        /// <param name="query">The query.</param>
        /// <param name="visitors">The visitors.</param>
        public QueryInterceptorQueryable(IQueryable<T> query, ExpressionVisitor[] visitors)
        {
            OriginalQueryable = query;
            Visitors = visitors;
        }

        /// <summary>Gets or sets the visitors.</summary>
        /// <value>The visitors.</value>
        public ExpressionVisitor[] Visitors { get; set; }

        /// <summary>Gets or sets the internal provider.</summary>
        /// <value>The internal provider.</value>
        public QueryInterceptorProvider<T> InternalProvider { get; set; }

        /// <summary>Gets or sets the original queryable.</summary>
        /// <value>The original queryable.</value>
        public IQueryable<T> OriginalQueryable { get; set; }

        /// <summary>Gets the type of the element.</summary>
        /// <value>The type of the element.</value>
        public Type ElementType
        {
            get { return OriginalQueryable.ElementType; }
        }

        /// <summary>Gets the expression.</summary>
        /// <value>The expression.</value>
        public Expression Expression
        {
            get { return OriginalQueryable.Expression; }
        }

        /// <summary>Gets the provider.</summary>
        /// <value>The provider.</value>
        public IQueryProvider Provider
        {
            get { return InternalProvider ?? (InternalProvider = new QueryInterceptorProvider<T>((IDbAsyncQueryProvider) OriginalQueryable.Provider) {CurrentQueryable = this}); }
        }

        /// <summary>Gets the enumerator.</summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return Visit().GetEnumerator();
        }

        /// <summary>Gets the enumerator.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Visit().GetEnumerator();
        }

        /// <summary>Gets the visit.</summary>
        /// <returns>An IQueryable&lt;T&gt;</returns>
        public IQueryable<T> Visit()
        {
            var query = OriginalQueryable;
            var expression = OriginalQueryable.Expression;

            foreach (var visitor in Visitors)
            {
                expression = visitor.Visit(expression);
            }

            if (expression != OriginalQueryable.Expression)
            {
                query = OriginalQueryable.Provider.CreateQuery<T>(expression);
            }
            return query;
        }

        /// <summary>Gets the visit.</summary>
        /// <param name="expression">The expression.</param>
        /// <returns>An IQueryable&lt;T&gt;</returns>
        public Expression Visit(Expression expression)
        {
            foreach (var visitor in Visitors)
            {
                expression = visitor.Visit(expression);
            }

            return expression;
        }

        /// <summary>Includes the given file.</summary>
        /// <param name="path">Full pathname of the file.</param>
        /// <returns>An IQueryable&lt;T&gt;</returns>
        public IQueryable<T> Include(string path)
        {
            var objectQuery = OriginalQueryable.GetObjectQuery();
            var objectQueryIncluded = objectQuery.Include(path);
            return new QueryInterceptorQueryable<T>(objectQueryIncluded, Visitors);
        }

#if NET45
        /// <summary>Gets asynchronous enumerator.</summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <returns>The asynchronous enumerator.</returns>
        IDbAsyncEnumerator<T> IDbAsyncEnumerable<T>.GetAsyncEnumerator()
        {
            return ((IDbAsyncEnumerable<T>) Visit().GetObjectQuery()).GetAsyncEnumerator();
        }

        /// <summary>Gets asynchronous enumerator.</summary>
        /// <returns>The asynchronous enumerator.</returns>
        public IDbAsyncEnumerator GetAsyncEnumerator()
        {
            return ((IDbAsyncEnumerable) Visit().GetObjectQuery()).GetAsyncEnumerator();
        }
#endif
    }
}