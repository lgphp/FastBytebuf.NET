namespace Bytebuf
{
    public class ByteBufException 
    {
        public class ExceedCapacityException : System.ApplicationException
        {
            public ExceedCapacityException(string message) : base(message)
            {
                
            }
        }

        public class IllegalReferenceCountException : System.ApplicationException
        {
            public IllegalReferenceCountException(string message) : base(message)
            {
            }
        }
        
        
        public class OutOfMaxValueException : System.ApplicationException
        {
            public OutOfMaxValueException(string message) : base(message)
            {
            }
        }
    }
}