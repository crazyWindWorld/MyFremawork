using UnityEngine;

namespace Fuel.Log
{
    public static class DebugLogger
    {
        public static bool Enable { get; set; } = true;

        public static void Log(LogWriter writer, object message)
        {
            if (!Enable) return;
            Debug.Log(FormatMessage(writer, message));
        }

        public static void LogWarning(LogWriter writer, object message)
        {
            if (!Enable) return;
            Debug.LogWarning(FormatMessage(writer, message));
        }

        public static void LogError(LogWriter writer, object message)       
        {
            if (!Enable) return;
            Debug.LogError(FormatMessage(writer, message));
        }



        public static string Color(object message, string color)
        {
            return  $"<color={color}>{message}</color>";
        }

        public static string Bold(object message)
        {
            return  $"<b>{message}</b>";
        }

        public static string Italic(object message)
        {
            return $"<i>{message}</i>" ;
        }

        private static string FormatMessage(LogWriter writer, object message)
        {
            return writer == LogWriter.Default ? message?.ToString() : $"[{writer}] {message}";
        }
    }
    public enum LogWriter
    {
        Default,
        SceneManager,
    }
}
