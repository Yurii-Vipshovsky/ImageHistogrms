using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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

        //ідея брати частини зображення і позиція буде непотрібна
        private Color[,] calcPositionInterval(int startPos, int endPos, Bitmap imageCopy )
        {
            int width = imageCopy.Width;
            int height = imageCopy.Height;
            int intensityIntervals = (int)numericUpDown1.Value;
            int minimumIntensity = (int)numericUpDown2.Value;
            int maximumIntensity = (int)numericUpDown3.Value;
            int intensityRange = (maximumIntensity - minimumIntensity) / intensityIntervals;
            Color[,] res = new Color[endPos-startPos,256];
            for(int x = 0; x < endPos - startPos; ++x)
            {
                for (int y=0; y < 256; ++y)
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
            //all black on create
            //
            for (int x = startPos; x < endPos; x++)
            {
                if (intensityIntervals == 255)
                {
                    //from 0 to 256 thinck about min-max Intensity
                    for (int i = minimumIntensity; i < maximumIntensity; ++i)
                    {
                        bool intensityAvailable = false;
                        for (int y = 0; y < height; ++y)
                        {
                            Color pixelColor = imageCopy.GetPixel(x, y);
                            if ((int)(pixelColor.R + pixelColor.G + pixelColor.B) / 3 == i)
                            {
                                intensityAvailable = true;
                                res[x - startPos, i] = Color.White;
                                break;
                            }
                        }
                        if (!intensityAvailable)
                        {
                            res[x - startPos, i] = Color.Black;
                        }
                    }

                }
                
            }
            return res;
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
                            res[x-startPos, brightness + i * intensityRange] = Color.Black;
                            //_histogramImage.SetPixel(x, brightness + i * intensityRange, Color.FromArgb(0, 0, 0));
                            continue;
                        }
                        accumulator += columnHistogram[brightness];
                        columnCumulativeHistogram[brightness] = (accumulator) / pixelCount;
                        int colorValue = (int)(columnCumulativeHistogram[brightness] * 255);
                        res[x-startPos, brightness + i * intensityRange] = Color.FromArgb(colorValue, colorValue, colorValue);
                        //_histogramImage.SetPixel(x, brightness + i * intensityRange, Color.FromArgb(colorValue, colorValue, colorValue));
                    }
                }
            }
            return res;
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
                                paralelResults[index] = calcPositionInterval(index * elemsForThread, width, imgClone);
                            });
                        }
                        else
                        {
                            threads[i] = new Thread(() =>
                            {
                                paralelResults[index] = calcPositionInterval(index * elemsForThread, (index + 1) * elemsForThread, imgClone);
                            });
                        }
                        threads[i].Start();
                    }
                    foreach (Thread thread in threads)
                    {
                        thread.Join();
                    }
                    test.Stop();
                    toolStripStatusLabel3.Text = "Час обчислення: " + test.ElapsedMilliseconds;
                    for (int i = 0; i < threadCount; i++)
                    {
                        for(int x = 0; x < paralelResults[i].GetLength(0); x++)
                        {
                            //check -1
                            for(int y= 0; y< paralelResults[i].GetLength(1)-1; y++)
                            {
                                _histogramImage.SetPixel(i* elemsForThread + x, y, paralelResults[i][x, y]);
                            }
                        }
                    }
                }
                else
                {
                    Color[,] Results;
                    Results = calcPositionInterval(0, width, _image);
                    
                    for (int x = 0; x < Results.GetLength(0); x++)
                    {
                        //check -1
                        for (int y = 0; y < Results.GetLength(1) - 1; y++)
                        {
                            _histogramImage.SetPixel(x, y, Results[x, y]);
                        }
                    }

                    pictureBox2.Image = _histogramImage;
                    test.Stop();
                    toolStripStatusLabel3.Text = "Час обчислення: " + test.ElapsedMilliseconds;

                    return;
                    for (int x = 0; x < width; x++)
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
                                    _histogramImage.SetPixel(x, brightness + i * intensityRange, Color.FromArgb(0, 0, 0));
                                    continue;
                                }
                                accumulator += columnHistogram[brightness];
                                columnCumulativeHistogram[brightness] = (accumulator) / pixelCount;
                                int colorValue = (int)(columnCumulativeHistogram[brightness] * 255);
                                _histogramImage.SetPixel(x, brightness + i * intensityRange, Color.FromArgb(colorValue, colorValue, colorValue));
                            }
                        }

                        toolStripProgressBar1.Value = (int)((double)x / width) * 100;
                    }
                }
            }
            else
            {
                _histogramImage = new Bitmap((maximumIntensity - minimumIntensity), height);

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

                    toolStripProgressBar1.Value = (int)((double)y / height) * 100;
                }
            }

            pictureBox2.Image = _histogramImage;
            test.Stop();
            toolStripStatusLabel3.Text = "Час обчислення: "+test.ElapsedMilliseconds;

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
    }
}
