namespace VsuStego.Helpers
{
    public static class MathHelper
    {
        public static int FitRange(int val, int l, int r)
        {
            if (val < l)
            {
                return l;
            }

            if (val >= r)
            {
                return r - 1;
            }

            return val;
        }
    }
}