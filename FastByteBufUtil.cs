using System;
using System.Text;

namespace Bytebuf
{
    public class FastByteBufUtil
    {
        public static FastBytebuf EnsureAccessible(FastBytebuf buf)
        {
            if (!buf.Accessible())
            {
                throw new ByteBufException.IllegalReferenceCountException(buf.RefCnt() + "");
            }

            return buf;
        }

        public static void PrettyPrint(FastBytebuf buf)
        {
            var c = 0;
            foreach (var b in buf.AvailableBytes())
            {
                if (c % 14 == 0)
                {
                    Console.WriteLine();
                }

                Console.Write("{0} ", b);
                c++;
            }

            Console.WriteLine();
        }


        public static string HexString(FastBytebuf buf)
        {
            var strBuider = new StringBuilder();
            foreach (var t in buf)
            {
                strBuider.Append(((int) t).ToString("X2"));
            }

            return strBuider.ToString();
        }


        public static string Base64String(FastBytebuf buf)
        {
            try
            {
                var encode = Convert.ToBase64String(buf.AvailableBytes());
                return encode;
            }
            catch
            {
                return null;
            }
        }

        public static FastBytebuf NewByteBufWithCapacity(int capacity)
        {
            return FastBytebuf.NewByteBufWithCapacity(capacity);
        }

        public static FastBytebuf NewBytebufWithDefault()
        {
            return FastBytebuf.NewBytebufWithDefault();
        }
        
        public static FastBytebuf NewBytebufWithBytes(byte[] bytes)
        {
            return new FastBytebuf(bytes);
        }
    }
}