using System;
using System.Globalization;
namespace Fuel.Tools
{
    public static class NumberFormatter
    {
        public enum FormatMode
        {
            /// <summary>
            /// 保留整数，四舍五入
            /// </summary>
            Integer,

            /// <summary>
            /// 固定保留两位小数
            /// </summary>
            TwoDecimal,

            /// <summary>
            /// 最多保留两位小数，并去掉末尾 0
            /// </summary>
            TrimZero
        }

        private struct Unit
        {
            public decimal Value { get; }
            public string Suffix { get; }

            public Unit(decimal value, string suffix)
            {
                Value = value;
                Suffix = suffix;
            }
        }

        private static readonly Unit[] Units =
        {
        new Unit(1_000_000_000_000m, "T"),
        new Unit(1_000_000_000m, "B"),
        new Unit(1_000_000m, "M"),
        new Unit(1_000m, "K")
    };

        /// <summary>
        /// 格式化数值，支持 int、long、float、double、decimal 等类型
        /// </summary>
        public static string Format<T>(
            T value,
            FormatMode mode = FormatMode.TrimZero
        ) where T : struct, IConvertible
        {
            decimal number;

            try
            {
                number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return "0";
            }

            return FormatDecimal(number, mode);
        }

        /// <summary>
        /// int 专用重载
        /// </summary>
        public static string Format(int value, FormatMode mode = FormatMode.TrimZero)
        {
            return FormatDecimal(value, mode);
        }

        /// <summary>
        /// long 专用重载
        /// </summary>
        public static string Format(long value, FormatMode mode = FormatMode.TrimZero)
        {
            return FormatDecimal(value, mode);
        }

        /// <summary>
        /// float 专用重载
        /// </summary>
        public static string Format(float value, FormatMode mode = FormatMode.TrimZero)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return "0";

            return FormatDecimal((decimal)value, mode);
        }

        /// <summary>
        /// double 专用重载
        /// </summary>
        public static string Format(double value, FormatMode mode = FormatMode.TrimZero)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "0";

            return FormatDecimal((decimal)value, mode);
        }

        /// <summary>
        /// decimal 专用重载
        /// </summary>
        public static string Format(decimal value, FormatMode mode = FormatMode.TrimZero)
        {
            return FormatDecimal(value, mode);
        }

        private static string FormatDecimal(decimal value, FormatMode mode)
        {
            string suffix = "";
            decimal displayValue = value;
            decimal absValue = Math.Abs(value);

            foreach (var unit in Units)
            {
                if (absValue >= unit.Value)
                {
                    displayValue = value / unit.Value;
                    suffix = unit.Suffix;
                    break;
                }
            }

            string numberText = mode switch
            {
                FormatMode.Integer =>
                    Math.Round(displayValue, 0, MidpointRounding.AwayFromZero)
                        .ToString("0", CultureInfo.InvariantCulture),

                FormatMode.TwoDecimal =>
                    displayValue.ToString("0.00", CultureInfo.InvariantCulture),

                FormatMode.TrimZero =>
                    displayValue.ToString("0.##", CultureInfo.InvariantCulture),

                _ =>
                    displayValue.ToString(CultureInfo.InvariantCulture)
            };

            return numberText + suffix;
        }
    }
}