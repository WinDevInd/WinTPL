using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPL
{
    public class BenchClass<T>
    {
        public T OperationResult { get; set; }
        public double executionTimeInMilliseconds { get; set; }
    }

    public class BenchClass
    {
        public double executionTimeInMilliseconds { get; set; }
    }
}
