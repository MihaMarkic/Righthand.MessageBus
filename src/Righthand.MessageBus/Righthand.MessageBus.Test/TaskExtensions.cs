using System.Threading.Tasks;
using System;

namespace Righthand.MessageBus.Test;

public static class TaskExtensions
{
    internal static async Task<bool> TaskWithTimeoutAsync<T>(this Task<T> task, TimeSpan? timeout = null)
    {
        var winner = await Task.WhenAny(task, Task.Delay(timeout ?? TimeSpan.FromSeconds(1)));
        return winner == task;
    }
}
