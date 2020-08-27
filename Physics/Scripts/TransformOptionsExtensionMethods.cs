namespace KRG
{
    public static class TransformOptionsExtensionMethods
    {
        public static bool HasFlag(this TransformOptions opt, TransformOptions flag)
        {
            return (opt & flag) == flag;
        }
    }
}