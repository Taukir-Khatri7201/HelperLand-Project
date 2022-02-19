using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace HelperLand.Extensions
{
    public static class SessionExtensions
    {
        public static void Set<T>(this ISession session, string key, T value)
        {
            var serialzedval = JsonSerializer.Serialize(value);
            session.SetString(key, serialzedval);
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}
