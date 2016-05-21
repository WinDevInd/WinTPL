using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPL.Timer
{
    public class DelayedCallbackProviderFactory
    {
        private static object syncObj = new object();
        private Type CommonCallbackImplType;
        private static DelayedCallbackProviderFactory instance;
        public static DelayedCallbackProviderFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncObj)
                    {
                        if (instance == null)
                        {
                            instance = new DelayedCallbackProviderFactory();
                        }
                    }
                }
                return instance;
            }
        }

        public void SetCommonCallbackImplementationClass(Type implClassType)
        {
            CommonCallbackImplType = implClassType;
        }

        public DelayedCallbackProvider CreateDelayedCallbackProvider(TimeSpan accuracy)
        {
            if (CommonCallbackImplType == null)
            {
                throw new Exception("Call SetCommonCallbackImplementationClass() before calling this method");
            }
            ITimedCallbackProvider provider = (ITimedCallbackProvider)Activator.CreateInstance(CommonCallbackImplType);
            return new DelayedCallbackProvider(provider, accuracy);
        }
    }
}
