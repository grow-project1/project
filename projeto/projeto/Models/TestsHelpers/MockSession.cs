namespace growTests.Models.TestsHelpers
{
    public class MockSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new Dictionary<string, byte[]>();

        public IEnumerable<string> Keys => _sessionStorage.Keys;

        public bool IsAvailable => true;

        public string Id => Guid.NewGuid().ToString();

        public void Clear() => _sessionStorage.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Remove(string key)
            => _sessionStorage.Remove(key);

        public void Set(string key, byte[] value)
            => _sessionStorage[key] = value;

        public bool TryGetValue(string key, out byte[] value)
            => _sessionStorage.TryGetValue(key, out value);
    }
}
