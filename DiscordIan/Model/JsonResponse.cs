using System;

namespace DiscordIan.Model
{
    public class JsonResponse<T>
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}
