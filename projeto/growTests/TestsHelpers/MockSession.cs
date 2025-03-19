using Microsoft.AspNetCore.Http;
using System.Text;

namespace growTests
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

        private int code;
        public void setCodeInSession(int code)
        {
            this.code = code;
        }

        public int getCode()
        {
            return code;
        }
    }

    // Métodos de extensão para ISession
    public static class SessionExtensions
    {
        public static void SetString(this ISession session, string key, string value)
        {
            session.Set(key, Encoding.UTF8.GetBytes(value));
        }

        public static string GetString(this ISession session, string key)
        {
            if (session.TryGetValue(key, out byte[] data))
                return Encoding.UTF8.GetString(data);
            return null;
        }

        public static void SetInt32(this ISession session, string key, int value)
        {
            session.Set(key, BitConverter.GetBytes(value));
        }

        public static int? GetInt32(this ISession session, string key)
        {
            if (session.TryGetValue(key, out byte[] data))
                return BitConverter.ToInt32(data, 0);
            return null;
        }


    }
}