using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectService
{
    public static class Utils
    {
        public static void LogLine(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        public static void Log(string message)
        {
            System.Diagnostics.Debug.Write(message);
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
