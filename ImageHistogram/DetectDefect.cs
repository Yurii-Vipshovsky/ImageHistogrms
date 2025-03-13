using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace ImageHistogram
{
    public class DefectDefectResult
    {
        public bool hasDefect { get; set; }
        public Bitmap result { get; set; }
        public long time { get; set; }
    }

    internal class DetectDefectClass
    {
        int width;
        int height;
        int intensityIntervals;
        int minimumIntensity;
        int maximumIntensity;
        int intensityRange;
        int defectsThreshold;
        private bool isVerticalScan;
        private int threadCount = 10;
        private bool isParalel = true;

        public DetectDefectClass(int width, int height, int intensityIntervals, int minimumIntensity, int maximumIntensity, int defectsThreshold, bool isVerticalScan)
        {
            this.width = width;
            this.height = height;
            this.intensityIntervals = intensityIntervals;
            this.minimumIntensity = minimumIntensity;
            this.maximumIntensity = maximumIntensity;
            this.intensityRange = (maximumIntensity - minimumIntensity) / intensityIntervals;
            this.defectsThreshold = defectsThreshold;
            this.isVerticalScan = isVerticalScan;
        }

        private int calcMostFrequentIntensity(Bitmap image)
        {
            int[] pixelCount = new int[maximumIntensity - minimumIntensity + 1];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Отримуємо кольор пікселя на позиції (x, y)
                    Color pixelColor = image.GetPixel(x, y);

                    // Обчислюємо яскравість пікселя (середнє значення кольорів)
                    int brightness = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;

                    pixelCount[brightness] += 1;

                }
            }
            return Array.IndexOf(pixelCount, pixelCount.Max());
        }

        private Color[,] calcPositionInterval(int startPos, int endPos, Bitmap imageCopy, bool isVertical)
        {
            if (isVertical)
            {
                Color[,] res = new Color[endPos - startPos, (maximumIntensity - minimumIntensity) + 1];
                if (intensityIntervals == 255)
                {
                    for (int x = 0; x < endPos - startPos; ++x)
                    {
                        for (int y = 0; y < 256; ++y)
                        {
                            res[x, y] = Color.Black;
                        }
                    }
                    for (int x = startPos; x < endPos; x++)
                    {
                        for (int y = 0; y < height; ++y)
                        {
                            Color pixelColor = imageCopy.GetPixel(x, y);
                            res[x - startPos, (int)(pixelColor.R + pixelColor.G + pixelColor.B) / 3] = Color.White;

                        }
                    }
                    return res;
                }
                for (int x = startPos; x < endPos; x++)
                {
                    for (int i = 0; i < intensityIntervals; i++)
                    {
                        int[] columnHistogram = new int[intensityRange];
                        double[] columnCumulativeHistogram = new double[intensityRange];
                        int pixelCount = 0;
                        // Перебираємо рядки
                        for (int y = 0; y < height; y++)
                        {
                            // Отримуємо кольор пікселя на позиції (x, y)
                            Color pixelColor = imageCopy.GetPixel(x, y);

                            // Обчислюємо яскравість пікселя (середнє значення кольорів)
                            int brightness = (int)(pixelColor.R + pixelColor.G + pixelColor.B) / 3;

                            if (brightness >= minimumIntensity && brightness <= maximumIntensity
                                && brightness >= i * intensityRange && brightness < (i + 1) * intensityRange)
                            {
                                columnHistogram[brightness % intensityRange]++;
                                pixelCount++;
                            }
                            // Оновлюємо гістограму для відповідної яскравості

                        }
                        double accumulator = 0;
                        for (int brightness = 0; brightness < intensityRange; brightness++)
                        {
                            if (pixelCount == 0)
                            {
                                res[x - startPos, brightness + i * intensityRange] = Color.Black;
                                continue;
                            }
                            accumulator += columnHistogram[brightness];
                            columnCumulativeHistogram[brightness] = (accumulator) / pixelCount;
                            int colorValue = (int)(columnCumulativeHistogram[brightness] * 255);
                            res[x - startPos, brightness + i * intensityRange] = Color.FromArgb(colorValue, colorValue, colorValue);
                        }
                    }
                }
                return res;
            }
            else
            {
                Color[,] res = new Color[(maximumIntensity - minimumIntensity) + 1, endPos - startPos];
                if (intensityIntervals == 255)
                {
                    for (int y = 0; y < endPos - startPos; ++y)
                    {
                        for (int x = 0; x < 256; ++x)
                        {
                            res[x, y] = Color.Black;
                        }
                    }
                    for (int y = startPos; y < endPos; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            Color pixelColor = imageCopy.GetPixel(x, y);
                            res[(int)(pixelColor.R + pixelColor.G + pixelColor.B) / 3, y - startPos] = Color.White;

                        }
                    }
                    return res;
                }
                for (int y = startPos; y < endPos; y++)
                {
                    for (int i = 0; i < intensityIntervals; i++)
                    {
                        int[] columnHistogram = new int[intensityRange];
                        double[] columnCumulativeHistogram = new double[intensityRange];
                        int pixelCount = 0;
                        // Перебираємо рядки
                        for (int x = 0; x < width; x++)
                        {
                            // Отримуємо кольор пікселя на позиції (x, y)
                            Color pixelColor = imageCopy.GetPixel(x, y);

                            // Обчислюємо яскравість пікселя (середнє значення кольорів)
                            int brightness = (int)(pixelColor.R + pixelColor.G + pixelColor.B) / 3;

                            if (brightness >= minimumIntensity && brightness <= maximumIntensity
                                && brightness >= i * intensityRange && brightness < (i + 1) * intensityRange)
                            {
                                columnHistogram[brightness % intensityRange]++;
                                pixelCount++;
                            }
                            // Оновлюємо гістограму для відповідної яскравості

                        }
                        double accumulator = 0;
                        for (int brightness = 0; brightness < intensityRange; brightness++)
                        {
                            if (pixelCount == 0)
                            {
                                res[brightness + i * intensityRange, y - startPos] = Color.Black;
                                continue;
                            }
                            accumulator += columnHistogram[brightness];
                            columnCumulativeHistogram[brightness] = (accumulator) / pixelCount;
                            int colorValue = (int)(columnCumulativeHistogram[brightness] * 255);
                            res[brightness + i * intensityRange, y - startPos] = Color.FromArgb(colorValue, colorValue, colorValue);
                        }
                    }
                }
                return res;
            }
        }

        public DefectDefectResult DetectDefect(Bitmap _image, Bitmap _histogramImage)
        {
            var test = Stopwatch.StartNew();
            int mostFrequentIntensity = 0;
            bool hasDefects = false;
            Thread mostFrequentIntensityThread = new Thread(() => { mostFrequentIntensity = calcMostFrequentIntensity((Bitmap)_image.Clone()); });
            mostFrequentIntensityThread.Start();
            if (isVerticalScan)
            {
                _histogramImage = new Bitmap(width, (maximumIntensity - minimumIntensity));

                if (isParalel)
                {
                    Color[][,] paralelResults = new Color[threadCount][,];
                    Thread[] threads = new Thread[threadCount];
                    int elemsForThread = width / threadCount;
                    for (int i = 0; i < threadCount; ++i)
                    {
                        int index = i;
                        Bitmap imgClone = (Bitmap)_image.Clone();
                        if (i == threadCount - 1)
                        {
                            threads[i] = new Thread(() =>
                            {
                                paralelResults[index] = calcPositionInterval(index * elemsForThread, width, imgClone, isVerticalScan);
                            });
                        }
                        else
                        {
                            threads[i] = new Thread(() =>
                            {
                                paralelResults[index] = calcPositionInterval(index * elemsForThread, (index + 1) * elemsForThread, imgClone, isVerticalScan);
                            });
                        }
                        threads[i].Start();
                    }
                    foreach (Thread thread in threads)
                    {
                        thread.Join();
                    }
                    mostFrequentIntensityThread.Join();
                    test.Stop();
                    //toolStripStatusLabel3.Text = "Час обчислення: " + test.ElapsedMilliseconds;
                    for (int i = 0; i < threadCount; i++)
                    {
                        for (int x = 0; x < paralelResults[i].GetLength(0); x++)
                        {
                            for (int y = 0; y < paralelResults[i].GetLength(1) - 1; y++)
                            {
                                if (y == mostFrequentIntensity + defectsThreshold || y == mostFrequentIntensity - defectsThreshold)
                                {
                                    _histogramImage.SetPixel(i * elemsForThread + x, y, Color.Aquamarine);
                                    continue;
                                }
                                else if (paralelResults[i][x, y] != Color.Black && (y > mostFrequentIntensity + defectsThreshold
                                    || y < mostFrequentIntensity - defectsThreshold))
                                {
                                    hasDefects = true;
                                }
                                _histogramImage.SetPixel(i * elemsForThread + x, y, paralelResults[i][x, y]);
                            }
                        }
                    }
                }
                //useles not paralel code
                else
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int i = 0; i < intensityIntervals; i++)
                        {
                            int[] columnHistogram = new int[intensityRange];
                            double[] columnCumulativeHistogram = new double[intensityRange];
                            int pixelCount = 0;
                            for (int y = 0; y < height; y++)
                            {
                                Color pixelColor = _image.GetPixel(x, y);
                                int brightness = (int)(pixelColor.R + pixelColor.G + pixelColor.B) / 3;

                                if (brightness >= minimumIntensity && brightness <= maximumIntensity
                                    && brightness >= i * intensityRange && brightness < (i + 1) * intensityRange)
                                {
                                    columnHistogram[brightness % intensityRange]++;
                                    pixelCount++;
                                }

                            }
                            double accumulator = 0;
                            for (int brightness = 0; brightness < intensityRange; brightness++)
                            {
                                if (pixelCount == 0)
                                {
                                    _histogramImage.SetPixel(x, brightness + i * intensityRange, Color.FromArgb(0, 0, 0));
                                    continue;
                                }
                                accumulator += columnHistogram[brightness];
                                columnCumulativeHistogram[brightness] = (accumulator) / pixelCount;
                                int colorValue = (int)(columnCumulativeHistogram[brightness] * 255);
                                _histogramImage.SetPixel(x, brightness + i * intensityRange, Color.FromArgb(colorValue, colorValue, colorValue));
                            }
                        }
                    }
                }
                test.Stop();
                //toolStripStatusLabel3.Text = "Час обчислення: " + test.ElapsedMilliseconds;
                return new DefectDefectResult { hasDefect = hasDefects, result = _histogramImage, time = test.ElapsedMilliseconds };
            }
            else
            {
                _histogramImage = new Bitmap((maximumIntensity - minimumIntensity), height);

                if (isParalel)
                {
                    Color[][,] paralelResults = new Color[threadCount][,];
                    Thread[] threads = new Thread[threadCount];
                    int elemsForThread = height / threadCount;
                    for (int i = 0; i < threadCount; ++i)
                    {
                        int index = i;
                        Bitmap imgClone = (Bitmap)_image.Clone();
                        if (i == threadCount - 1)
                        {
                            threads[i] = new Thread(() =>
                            {
                                paralelResults[index] = calcPositionInterval(index * elemsForThread, height, imgClone, isVerticalScan);
                            });
                        }
                        else
                        {
                            threads[i] = new Thread(() =>
                            {
                                paralelResults[index] = calcPositionInterval(index * elemsForThread, (index + 1) * elemsForThread, imgClone, isVerticalScan);
                            });
                        }
                        threads[i].Start();
                    }
                    foreach (Thread thread in threads)
                    {
                        thread.Join();
                    }
                    mostFrequentIntensityThread.Join();
                    test.Stop();
                    //toolStripStatusLabel3.Text = "Час обчислення: " + test.ElapsedMilliseconds;
                    for (int i = 0; i < threadCount; i++)
                    {
                        for (int x = 0; x < paralelResults[i].GetLength(0) - 1; x++)
                        {
                            for (int y = 0; y < paralelResults[i].GetLength(1); y++)
                            {
                                if (x == mostFrequentIntensity + defectsThreshold || x == mostFrequentIntensity - defectsThreshold)
                                {
                                    _histogramImage.SetPixel(x, i * elemsForThread + y, Color.Aquamarine);
                                    continue;
                                }
                                else if (paralelResults[i][x, y] != Color.Black && (x > mostFrequentIntensity + defectsThreshold
                                    || x < mostFrequentIntensity - defectsThreshold))
                                {
                                    hasDefects = true;
                                }
                                _histogramImage.SetPixel(x, i * elemsForThread + y, paralelResults[i][x, y]);
                            }
                        }
                    }
                }
                //Useless sequental code
                else
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int i = 0; i < intensityIntervals; i++)
                        {
                            int[] columnHistogram = new int[intensityRange];
                            double[] columnCumulativeHistogram = new double[intensityRange];
                            int pixelCount = 0;
                            // Перебираємо рядки
                            for (int x = 0; x < width; x++)
                            {
                                // Отримуємо кольор пікселя на позиції (x, y)
                                Color pixelColor = _image.GetPixel(x, y);

                                // Обчислюємо яскравість пікселя (середнє значення кольорів)
                                int brightness = (int)(pixelColor.R + pixelColor.G + pixelColor.B) / 3;

                                if (brightness >= minimumIntensity && brightness <= maximumIntensity
                                    && brightness >= i * intensityRange && brightness < (i + 1) * intensityRange)
                                {
                                    columnHistogram[brightness % intensityRange]++;
                                    pixelCount++;
                                }
                                // Оновлюємо гістограму для відповідної яскравості

                            }
                            double accumulator = 0;
                            for (int brightness = 0; brightness < intensityRange; brightness++)
                            {
                                if (pixelCount == 0)
                                {
                                    _histogramImage.SetPixel(brightness + i * intensityRange, y, Color.FromArgb(0, 0, 0));
                                    continue;
                                }
                                accumulator += columnHistogram[brightness];
                                columnCumulativeHistogram[brightness] = (accumulator) / pixelCount;
                                int colorValue = (int)(columnCumulativeHistogram[brightness] * 255);
                                _histogramImage.SetPixel(brightness + i * intensityRange, y, Color.FromArgb(colorValue, colorValue, colorValue));
                            }
                        }
                    }
                }

                //pictureBox2.Image = _histogramImage;
                test.Stop();
                //toolStripStatusLabel3.Text = "Час обчислення: " + test.ElapsedMilliseconds;
                return new DefectDefectResult { hasDefect = hasDefects, result = _histogramImage, time = test.ElapsedMilliseconds };
            }
        }
    }
}
