using System.Threading.Tasks;

namespace TPL
{
    class SyncContext
    {
        public bool IsSuccess { get; set; } = true;
        public SyncContext Set(bool isSuccess)
        {
            IsSuccess = isSuccess;
            return this;
        }
    }

    public class Synchronizer
    {
        private InstanceQM<SyncContext> synchronizationHandle = new InstanceQM<SyncContext>(1, true);
        private SyncContext synchronizationContext = null;
        public async Task<bool> Reserve()
        {
            synchronizationContext = await synchronizationHandle.GetFreeInstanceAsync();
            return synchronizationContext.IsSuccess;
        }
        public void Free()
        {
            if (synchronizationContext != null)
                synchronizationHandle.ReleaseInstance(synchronizationContext.Set(true));
        }
        public void Break()
        {
            if (synchronizationContext != null)
                synchronizationHandle.ReleaseInstance(synchronizationContext.Set(false));
        }
    }
}
