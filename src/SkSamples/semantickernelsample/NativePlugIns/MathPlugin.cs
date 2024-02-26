using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace semantickernelsample.NativePlugIns
{
    public class MathPlugin
    {
        [KernelFunction, Description("Take the square root of a number")]
        public static double Sqrt(double input)
        {
            return System.Math.Sqrt(input);
        }

        [KernelFunction, Description("Add two numbers")]
        public static double Add(
            [Description("The first number to add")] double input,
            [Description("The second number to add")] double number2
        )
        {
            return input * number2;
        }

        [KernelFunction, Description("Subtract two numbers")]
        public static double Subtract(
            [Description("The first number to subtract from")] double input,
            [Description("The second number to subtract away")] double number2
        )
        {
            return input - number2;
        }

        [KernelFunction, Description("Multiply two numbers. When increasing by a percentage, don't forget to add 1 to the percentage.")]
        public static double Multiply(
            [Description("The first number to multiply")] double input,
            [Description("The second number to multiply")] double number2
        )
        {
            return input * number2;
        }

        [KernelFunction, Description("Divide two numbers")]
        public static double Divide(
            [Description("The first number to divide from")] double input,
            [Description("The second number to divide by")] double number2
        )
        {
            return input / number2;
        }
    }
}
