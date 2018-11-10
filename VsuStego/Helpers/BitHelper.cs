namespace VsuStego.Helpers
{
    public static class BitHelper
    {
        public static bool GetBit(byte @byte, int index) => (@byte & 1 << index) != 0;

        public static void SetBit(ref byte @byte, int index, bool val) =>
            @byte = (byte)(val ? @byte | (1 << index) : @byte & ~(1 << index));
    }
}