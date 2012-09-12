using System.Text;

namespace SharpServer
{
    public static class Helper
    {
        public static bool IsDefault<T>(this T value) where T : struct
        {
            return value.Equals(default(T));
        }

        public static bool EndsWith(this StringBuilder sb, string value)
        {
            if (sb.Length >= value.Length)
            {
                for (int i = sb.Length - 1, j = value.Length - 1; j >= 0; i--, j--)
                {
                    if (sb[i] != value[j])
                        return false;
                }

                return true;
            }

            return false;
        }
    }
}
