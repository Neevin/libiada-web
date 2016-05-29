﻿namespace LibiadaWeb.Math
{
    using System;
    using System.Linq;

    /// <summary>
    /// The auto correlation.
    /// </summary>
    public static class AutoCorrelation
    {
        /// <summary>
        /// The calculate autocorrelation.
        /// </summary>
        /// <param name="characteristics">
        /// The characteristics.
        /// </param>
        /// <returns>
        /// Autocorrelation as <see cref="T:double[][]"/>.
        /// </returns>
        public static double[][] CalculateAutocorrelation(double[][] characteristics)
        {
            var result = new double[characteristics.Length][];

            for (int i = 0; i < characteristics.Length; i++)
            {
                result[i] = new double[characteristics[0].Length];
            }

            // cycle through all characteristics
            for (int i = 0; i < characteristics[0].Length; i++)
            {
                var currentCharacteristic = characteristics.Select(c => c[i]).ToArray();
                double[] characteristicResult = CalculateAutocorrelation(currentCharacteristic);

                // cycle through all sequence fragments
                for (int j = 0; j < characteristics.Length; j++)
                {
                    result[j][i] = characteristicResult[j];
                }             
            }

            return result;
        }

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/>.
        /// </returns>
        public static double[] CalculateAutocorrelation(double[] x)
        {
            double[] result = GetAutoCorrelationOfSeries(x);
            return result;
        }

        /// <summary>
        /// The get average.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if array is empty.
        /// </exception>
        public static double GetAverage(double[] data)
        {
            int length = data.Length;

            if (length == 0)
            {
                throw new Exception("No data");
            }

            double sum = 0;

            for (int i = 0; i < length; i++)
            {
                sum += data[i];
            }

            return sum / length;
        }

        /// <summary>
        /// The get variance.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double GetVariance(double[] data)
        {
            int len = data.Length;

            // Get average
            double avg = GetAverage(data);

            double sum = data.Sum(t => Math.Pow(t - avg, 2));

            return sum / len;
        }

        /// <summary>
        /// The get stdev.
        /// </summary>
        /// <param name="data">
        /// The data.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public static double GetStdev(double[] data)
        {
            return Math.Sqrt(GetVariance(data));
        }

        /// <summary>
        /// The get correlation.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <param name="y">
        /// The y.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if input arrays has different length. 
        /// </exception>
        public static double GetCorrelation(double[] x, double[] y)
        {
            if (x.Length != y.Length)
            {
                throw new Exception("Length of sources is different");
            }

            double avgX = GetAverage(x);
            double stdevX = GetStdev(x);
            double avgY = GetAverage(y);
            double stdevY = GetStdev(y);
            double covXY = 0;
            double pearson = 0;
            int len = x.Length;
            for (int i = 0; i < len; i++)
            {
                covXY += (x[i] - avgX) * (y[i] - avgY);
            }

            covXY /= len;
            pearson = covXY / (stdevX * stdevY);
            return pearson;
        }

        /// <summary>
        /// The get auto correlation of series.
        /// </summary>
        /// <param name="x">
        /// The x.
        /// </param>
        /// <returns>
        /// The <see cref="T:double[]"/>.
        /// </returns>
        public static double[] GetAutoCorrelationOfSeries(double[] x)
        {
            int half = x.Length / 2;
            var autoCorrelation = new double[half];
            var a = new double[half];
            var b = new double[half];
            for (int i = 0; i < half; i++)
            {
                a[i] = x[i];
                b[i] = x[i + i];
                autoCorrelation[i] = GetCorrelation(a, b);
            }

            return autoCorrelation;
        }
    }
}
