using System;
using System.Threading;
using System.Threading.Tasks;

public class AsyncLock
{
    // This has to be released and acquired
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    // This acquires the lock
    public async Task<Releaser> LockAsync()
    {
        await _semaphore.WaitAsync();
        return new Releaser(_semaphore);
    }


    // this creates a releaser to release the lock
    public struct Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public Releaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}
