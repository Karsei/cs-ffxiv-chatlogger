using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ffxiv_chatlogger
{
    internal static class LogWriter
    {
        public static void Info(string msg)
        {
            Console.WriteLine("[{0:yyyy/MM/dd HH:mm:ss}][정보] {1}", DateTime.Now, msg);
        }
        public static void Info(string msg, object arg0)
        {
            Console.WriteLine("[{0:yyyy/MM/dd HH:mm:ss}][정보] {1}", DateTime.Now, String.Format(msg, arg0));
        }
        public static void Info(string msg, object arg0, object arg1)
        {
            Console.WriteLine("[{0:yyyy/MM/dd HH:mm:ss}][정보] {1}", DateTime.Now, String.Format(msg, arg0, arg1));
        }

        public static void Error(string msg)
        {
            Console.WriteLine("[{0:yyyy/MM/dd HH:mm:ss}][오류] {1}", DateTime.Now, msg);
        }
        public static void Error(string msg, Exception e)
        {
            Console.WriteLine("[{0:yyyy/MM/dd HH:mm:ss}][오류] {1}", DateTime.Now, msg);
            Console.WriteLine("[{0:yyyy/MM/dd HH:mm:ss}][오류] {1}", DateTime.Now, e.Message);
            Console.WriteLine("[{0:yyyy/MM/dd HH:mm:ss}][오류] {1}", DateTime.Now, e.StackTrace.ToString());
        }
    }
}
