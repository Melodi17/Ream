namespace Libraries
{
    public class Math
    {
        public static double Add(double a, double b)
        {
            return a + b;
        }

        public static double Subtract(double a, double b)
        {
            return a - b;
        }

        public static double Multiply(double a, double b)
        {
            return a * b;
        }

        public static double Divide(double a, double b)
        {
            return a / b;
        }

        public static double Power(double a, double b)
        {
            return System.Math.Pow(a, b);
        }

        public static double Sqrt(double a)
        {
            return System.Math.Sqrt(a);
        }

        public static double Sin(double a)
        {
            return System.Math.Sin(a);
        }

        public static double Cos(double a)
        {
            return System.Math.Cos(a);
        }

        public static double Tan(double a)
        {
            return System.Math.Tan(a);
        }

        public static double Asin(double a)
        {
            return System.Math.Asin(a);
        }

        public static double Acos(double a)
        {
            return System.Math.Acos(a);
        }

        public static double Atan(double a)
        {
            return System.Math.Atan(a);
        }

        public static double Log(double a)
        {
            return System.Math.Log(a);
        }

        public static double Log10(double a)
        {
            return System.Math.Log10(a);
        }

        public static double Exp(double a)
        {
            return System.Math.Exp(a);
        }

        public static double Abs(double a)
        {
            return System.Math.Abs(a);
        }

        public static double Max(double a, double b)
        {
            return System.Math.Max(a, b);
        }

        public static double Min(double a, double b)
        {
            return System.Math.Min(a, b);
        }

        public static double Clamp(double a, double min, double max)
        {
            return System.Math.Min(System.Math.Max(a, min), max);
        }

        public static double Round(double a)
        {
            return System.Math.Round(a);
        }

        public static double Floor(double a)
        {
            return System.Math.Floor(a);
        }

        public static double Ceiling(double a)
        {
            return System.Math.Ceiling(a);
        }

        public static double Truncate(double a)
        {
            return System.Math.Truncate(a);
        }

        public static double Sign(double a)
        {
            return System.Math.Sign(a);
        }

        public static double Mod(double a, double b)
        {
            return a % b;
        }

        public static double Factorial(double a)
        {
            double result = 1;
            for (int i = 1; i <= a; i++)
            {
                result *= i;
            }
            return result;
        }

        public static double ToRadians(double a)
        {
            return a * System.Math.PI / 180;
        }

        public static double ToDegrees(double a)
        {
            return a * 180 / System.Math.PI;
        }

        public static double Random(double min, double max)
        {
            return randomObj.Next(System.Convert.ToInt32(min), System.Convert.ToInt32(max));
        }

        public static double PI
        {
            get
            {
                return System.Math.PI;
            }
        }

        public static double E
        {
            get
            {
                return System.Math.E;
            }
        }

        public static double Deg2Rad
        {
            get
            {
                return System.Math.PI / 180;
            }
        }

        public static double Rad2Deg
        {
            get
            {
                return 180 / System.Math.PI;
            }
        }

        private static System.Random randomObj = new System.Random();
    }
}