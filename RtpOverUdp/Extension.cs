namespace Extensions
{
    using System;

    public static class ArrayExtensions
    {
        public static T[] SubArray<T>(this T[] objs, int index, int length)
        {
            T[] subObjs = new T[length];
            Array.Copy(objs, index, subObjs, 0, length);
            return subObjs;
        }

        public static string ToBits(this byte src)
        {
            string bits = Convert.ToString(src, 2);
            while (bits.Length < 8)
                bits = "0" + bits;
            return bits;
        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }

    public static class DebugFunctions
    {
        public static void PrintChars(string str)
        {
            foreach (char ch in str)
            {
                if (ch == '\r')
                    System.Diagnostics.Debug.Write("(CR)");
                else if (ch == '\n')
                    System.Diagnostics.Debug.WriteLine("(LF)");
                else
                    System.Diagnostics.Debug.Write(ch);
            }
        }
        public static void PrintNChars(string str, int n = 0)
        {
            if (n == 0)
                System.Diagnostics.Debug.Write(str);
            else
            {
                for (int i = 0; i < n; i++)
                    System.Diagnostics.Debug.Write(str[i]);
            }
        }

        public static void PrintBits(byte[] data, int n=0)
        {
            if (n == 0)
                n = data.Length;

            for (int i = 0; i < n - 1; i++)
            {
                System.Diagnostics.Debug.Write(data[i].ToBits());
                System.Diagnostics.Debug.Write(" ");
            }
            System.Diagnostics.Debug.WriteLine(data[n - 1].ToBits());
        }

        public static void PrintBinary(byte[] data, int n = 0)
        {
            if (n == 0)
                n = data.Length;

            for (int i = 0; i < n - 1; i++)
            {
                System.Diagnostics.Debug.Write(data[i].ToString("X2"));
                System.Diagnostics.Debug.Write(" ");
            }
            System.Diagnostics.Debug.WriteLine(data[n - 1].ToString("X2"));
        }
    }
}
