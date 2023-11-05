using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
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

                toolStripStatusLabel2.Text = "Розширення: "+ width+"х"+height;

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            int width = _image.Width;
            int height = _image.Height;
            int intensityIntervals = (int)numericUpDown1.Value;
            int minimumIntensity = (int)numericUpDown2.Value;
            int maximumIntensity = (int)numericUpDown3.Value;
            int intensityRange = (maximumIntensity-minimumIntensity) / intensityIntervals;

            _histogramImage = new Bitmap(width, (maximumIntensity - minimumIntensity));

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
                        int brightness = (int)(pixelColor.R + pixelColor.G + pixelColor.B)/3;

                        if(brightness>= minimumIntensity && brightness<=maximumIntensity 
                            && brightness >= i*intensityRange && brightness <(i+1)*intensityRange)
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
                            _histogramImage.SetPixel(x, brightness+i* intensityRange, Color.FromArgb(0, 0, 0));
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
    }
}
