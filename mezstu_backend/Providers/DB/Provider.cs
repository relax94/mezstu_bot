using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;

namespace mezstu_backend.Providers.DB
{
    public abstract class Provider
    {
     
        public abstract void Push<T>(T instance);
        public abstract Task<IEnumerable<T>> Pop<T>(Expression<Func<T, bool>> predicate);
        public abstract void Replace<T>(Expression<Func<T, bool>> predicate, T instance);
    }
}