using System;

namespace DiscordBot.Utils
{
    public sealed class DualNum
    {
        public float Real => this.real;
        public float Dual => this.dual;

        public DualNum(float real = default(float), float dual = default(float))
        {
            this.real = real;
            this.dual = dual;
        }

        public static DualNum operator + (DualNum lhs, DualNum rhs)
        {
            return new DualNum(lhs.real + rhs.real, lhs.dual + rhs.dual);
        }

        public static DualNum operator - (DualNum lhs, DualNum rhs)
        {
            return new DualNum(lhs.real - rhs.real, lhs.dual - rhs.dual);
        }

        public static DualNum operator * (DualNum lhs, DualNum rhs)
        {
            return new DualNum(lhs.real * rhs.real, lhs.real * rhs.dual + lhs.dual * rhs.real);
        }

        public static DualNum operator / (DualNum lhs, DualNum rhs)
        {
            return new DualNum(lhs.real / rhs.real, (lhs.dual * rhs.real - lhs.real * rhs.dual) / (rhs.real * rhs.real));
        }

        public static DualNum sqrt(DualNum val)
        {
            float realSqrt = (float)Math.Sqrt(val.real);
            return new DualNum(realSqrt, 0.5f * val.dual / realSqrt);
        }

        private float real;
        private float dual;
    }
}

