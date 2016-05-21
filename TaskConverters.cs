using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPL
{
    internal class TaskConverters
    {
        internal static Func<Task<T>> ConvertToFuncToTask<T>(Func<T> func)
        {
            return () => funcEncapsulatorAsync<T>(func);
        }
        static async Task<T> funcEncapsulatorAsync<T>(Func<T> func)
        {
            return func();
        }

        static async Task<T> funcEncapsulatorAsync<T>(Action act)
        {
            act.Invoke();
            return default(T);
        }
        internal static Func<Task<T>> ConvertToActionToTask<T>(Action act)
        {
            return () => funcEncapsulatorAsync<T>(act);
        }

        static async Task<T> funcEncapsulatorAsync<T>(Func<Task> func)
        {
            await func();
            return default(T);
        }
        internal static Func<Task<T>> ConvertToFuncToTask<T>(Func<Task> func)
        {
            return () => funcEncapsulatorAsync<T>(func);
        }
    }
}
