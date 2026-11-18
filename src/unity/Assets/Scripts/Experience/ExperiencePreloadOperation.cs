using System;
using System.Collections;
using System.Collections.Generic;

// Unity otherwise logs a nested coroutine exception and can leave its caller waiting forever.
public static class ExperiencePreloadOperation
{
    public static IEnumerator Run(IEnumerator operation, Action<Exception> failed)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(operation);
        try
        {
            while (stack.Count > 0)
            {
                object current = null;
                bool moved = false;
                Exception error = null;
                try
                {
                    moved = stack.Peek().MoveNext();
                    if (moved) current = stack.Peek().Current;
                }
                catch (Exception exception) { error = exception; }
                if (error != null) { failed(error); yield break; }
                if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) stack.Push(nested);
                else yield return current;
            }
        }
        finally
        {
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
        }
    }
}
