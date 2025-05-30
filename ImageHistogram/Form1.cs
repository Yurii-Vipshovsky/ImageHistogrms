﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Net.Mime.MediaTypeNames;

namespace ImageHistogram
{
    public partial class Form1 : Form
    {
        private Bitmap _image;
        private Bitmap _histogramImage;
        private bool isVerticalScan = true;
        private int threadCount = 10;
        private bool isParalel = true;
        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _image = new Bitmap(openFileDialog1.FileName);
                pictureBox1.Image = _image;
                int width = _image.Width;
                int height = _image.Height;

                _histogramImage = new Bitmap(width, 256);

                toolStripStatusLabel1.Text = openFileDialog1.FileName;

                toolStripStatusLabel2.Text = "Розширення: " + width + "х" + height;

            }
        }

        private Color[,] calcPositionInterval(int startPos, int endPos, Bitmap imageCopy, bool isVertical)
        {
            int width = imageCopy.Width;
            int height = imageCopy.Height;
            int intensityIntervals = (int)numericUpDown1.Value;
            int minimumIntensity = (int)numericUpDown2.Value;
            int maximumIntensity = (int)numericUpDown3.Value;
            int intensityRange = (maximumIntensity - minimumIntensity) / intensityIntervals;
            if (isVertical)
            {
                Color[,] res = new Color[endPos - startPos, (maximumIntensity - minimumIntensity)+1];
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
                Color[,] res = new Color[(maximumIntensity - minimumIntensity)+1, endPos - startPos];
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
                            res[(int)(pixelColor.R + pixelColor.G + pixelColor.B) / 3, y-startPos] = Color.White;

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
                            res[brightness + i * intensityRange, y-startPos] = Color.FromArgb(colorValue, colorValue, colorValue);
                        }
                    }
                }
                return res;
            }
        }

        private int calcMostFrequentIntensity(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;
            int minimumIntensity = (int)numericUpDown2.Value;
            int maximumIntensity = (int)numericUpDown3.Value;
            int[] pixelCount = new int[maximumIntensity-minimumIntensity+1];
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

        private void button1_Click(object sender, EventArgs e)
        {
            var test = Stopwatch.StartNew();
            int width = _image.Width;
            int height = _image.Height;
            int intensityIntervals = (int)numericUpDown1.Value;
            int minimumIntensity = (int)numericUpDown2.Value;
            int maximumIntensity = (int)numericUpDown3.Value;
            int intensityRange = (maximumIntensity-minimumIntensity) / intensityIntervals;
            int defectsThreshold = ((int)numericUpDown4.Value * (maximumIntensity - minimumIntensity))/100;
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
                    toolStripStatusLabel3.Text = "Час обчислення: " + test.ElapsedMilliseconds;
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
                                else if (paralelResults[i][x,y]!=Color.Black && (y > mostFrequentIntensity + defectsThreshold 
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
                toolStripStatusLabel3.Text = "Час обчислення: " + test.ElapsedMilliseconds;
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
                    toolStripStatusLabel3.Text = "Час обчислення: " + test.ElapsedMilliseconds;
                    for (int i = 0; i < threadCount; i++)
                    {
                        for (int x = 0; x < paralelResults[i].GetLength(0)-1; x++)
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

                pictureBox2.Image = _histogramImage;
                test.Stop();
                toolStripStatusLabel3.Text = "Час обчислення: " + test.ElapsedMilliseconds;
            }
            if (hasDefects)
            {
                label5.Text = "ВИЯВЛЕНО ДЕФЕКТИ!";
                label5.ForeColor = Color.Red;
            }
            else
            {
                label5.Text = "Дефектів не виявлено";
                label5.ForeColor = Color.Green;
            }
            pictureBox2.Image = _histogramImage;

        }

        private void grayscaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for(int x=0; x < _image.Width; x++)
            {
                for(int y=0; y < _image.Height; y++)
                {
                    Color oc = _image.GetPixel(x, y);
                    int grayScale = (int)((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11));
                    Color nc = Color.FromArgb(oc.A, grayScale, grayScale, grayScale);
                    _image.SetPixel(x, y, nc);
                }
            }
            pictureBox1.Image = _image;
        }

        private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                    // Code to write the stream goes here.
                string filePath = saveFileDialog1.FileName;
                _image.Save(filePath); // Збереження зображення
                Console.WriteLine($"Зображення збережено в {filePath}");
            }
        }

        private void saveResultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // Code to write the stream goes here.
                string filePath = saveFileDialog1.FileName;
                _histogramImage.Save(filePath); // Збереження зображення
                Console.WriteLine($"Зображення збережено в {filePath}");
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void verticalScanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            horizontalScanToolStripMenuItem.Checked = false;
            isVerticalScan = true;
        }

        private void horizontalScanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            verticalScanToolStripMenuItem.Checked = false;
            isVerticalScan = false;
        }

        private List<ImageAnalysisResult> _analysisResults = new List<ImageAnalysisResult>();

        private void openManyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _analysisResults.Clear();
            int minimumIntensity = (int)numericUpDown2.Value;
            int maximumIntensity = (int)numericUpDown3.Value;
            

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var test = Stopwatch.StartNew();
                    foreach (string filePath in ofd.FileNames)
                    {
                        // Істинна наявність дефекту визначається з назви файлу
                        bool isDefectedInName = filePath.IndexOf("Defected", StringComparison.OrdinalIgnoreCase) >= 0;

                        // Завантажуємо зображення (Bitmap) і проганяємо через алгоритм
                        try
                        {
                            using (Bitmap bmp = new Bitmap(filePath))
                            {
                                DetectDefectClass detect = new DetectDefectClass(
                                bmp.Width,
                                bmp.Height,
                                (int)numericUpDown1.Value,
                                minimumIntensity,
                                maximumIntensity,
                                ((int)numericUpDown4.Value * (maximumIntensity - minimumIntensity)) / 100,
                                isVerticalScan
                                );

                                DefectDefectResult detectedByAlgorithm = detect.DetectDefect(bmp, _histogramImage);

                                // Порівняння справжньої наявності і передбачення алгоритму
                                bool isCorrect = (isDefectedInName == detectedByAlgorithm.hasDefect);

                                // Зберігаємо результат
                                _analysisResults.Add(new ImageAnalysisResult
                                {
                                    FileName = Path.GetFileName(filePath),
                                    IsDefectedInName = isDefectedInName,
                                    DetectedByAlgorithm = detectedByAlgorithm.hasDefect,
                                    IsCorrect = isCorrect
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Помилка читання файлу {Path.GetFileName(filePath)}: {ex.Message}");
                        }
                    }
                    test.Stop();
                    toolStripStatusLabel3.Text = "Час обчислення: " + test.ElapsedMilliseconds;
                    // Після завантаження всіх зображень — можна зберегти результати у файл і/або показати на формі
                    SaveAnalysisResultsToFile("AnalysisResults.csv");
                    MessageBox.Show("Аналіз завершено. Результати записано у AnalysisResults.csv");
                }
            }
        }

        private void SaveAnalysisResultsToFile(string outputFilePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(outputFilePath))
                {
                    // Заголовок CSV
                    writer.WriteLine("FileName,HasDefectInName,DetectedByAlgorithm,IsCorrect");

                    int correctCount = 0;

                    // Проходимося по результатах
                    foreach (var res in _analysisResults)
                    {
                        writer.WriteLine($"{res.FileName},{res.IsDefectedInName},{res.DetectedByAlgorithm},{res.IsCorrect}");
                        if (res.IsCorrect) correctCount++;
                    }

                    // Обчислюємо точність
                    double accuracy = 0;
                    if (_analysisResults.Count > 0)
                    {
                        accuracy = (double)correctCount / _analysisResults.Count * 100.0;
                    }

                    // Додаємо в кінець файлу загальну точність
                    writer.WriteLine();
                    writer.WriteLine($"Accuracy: {accuracy:F2}%");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка запису файлу: {ex.Message}");
            }
        }

    }
}
