using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ImageHistogram
{
    public partial class Form1 : Form
    {
        private Bitmap _image;
        private Bitmap _histogramImage;
        private bool isVerticalScan = true;
        // private int threadCount = 10; // Більше не потрібно тут, клас сам керує потоками
        // private bool isParalel = true; // Теж не потрібно тут

        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // Важливо: створюємо копію, щоб не блокувати файл
                using (var temp = new Bitmap(openFileDialog1.FileName))
                {
                    _image = new Bitmap(temp);
                }

                pictureBox1.Image = _image;
                int width = _image.Width;
                int height = _image.Height;

                // _histogramImage ініціалізується пізніше результатом обробки
                toolStripStatusLabel1.Text = openFileDialog1.FileName;
                toolStripStatusLabel2.Text = "Розширення: " + width + "х" + height;
            }
        }

        // --- ВИДАЛЕНО СТАРІ ПОВІЛЬНІ МЕТОДИ calcPositionInterval ТА calcMostFrequentIntensity ---
        // Тепер вся робота виконується у класі DetectDefectClass

        private void button1_Click(object sender, EventArgs e)
        {
            if (_image == null)
            {
                MessageBox.Show("Спочатку відкрийте зображення!");
                return;
            }

            int width = _image.Width;
            int height = _image.Height;
            int intensityIntervals = (int)numericUpDown1.Value;
            int minimumIntensity = (int)numericUpDown2.Value;
            int maximumIntensity = (int)numericUpDown3.Value;

            // Розрахунок порогу (як у твоєму коді)
            int defectsThreshold = ((int)numericUpDown4.Value * (maximumIntensity - minimumIntensity)) / 100;

            // === ВИКЛИК ШВИДКОГО КЛАСУ ===
            DetectDefectClass detector = new DetectDefectClass(
                width,
                height,
                intensityIntervals,
                minimumIntensity,
                maximumIntensity,
                defectsThreshold,
                isVerticalScan
            );

            // Запускаємо обробку
            // Передаємо null замість _histogramImage, бо клас створить нову картинку сам
            DefectDefectResult result = detector.DetectDefect(_image, null);

            // Оновлюємо UI
            _histogramImage = result.result; // Отримуємо готову швидку картинку
            pictureBox2.Image = _histogramImage;

            toolStripStatusLabel3.Text = "Час обчислення: " + result.time + " мс";

            if (result.hasDefect)
            {
                label5.Text = "ВИЯВЛЕНО ДЕФЕКТИ!";
                label5.ForeColor = Color.Red;
            }
            else
            {
                label5.Text = "Дефектів не виявлено";
                label5.ForeColor = Color.Green;
            }
        }

        private void grayscaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_image == null) return;

            // Це теж повільно, але це окрема функція. 
            // Якщо хочеш пришвидшити і це - скажи.
            for (int x = 0; x < _image.Width; x++)
            {
                for (int y = 0; y < _image.Height; y++)
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
            if (_image != null && saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _image.Save(saveFileDialog1.FileName);
            }
        }

        private void saveResultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_histogramImage != null && saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _histogramImage.Save(saveFileDialog1.FileName);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void verticalScanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            horizontalScanToolStripMenuItem.Checked = false;
            verticalScanToolStripMenuItem.Checked = true; // Додав візуальне перемикання
            isVerticalScan = true;
        }

        private void horizontalScanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            verticalScanToolStripMenuItem.Checked = false;
            horizontalScanToolStripMenuItem.Checked = true; // Додав візуальне перемикання
            isVerticalScan = false;
        }

        // Список для збереження результатів
        public class ImageAnalysisResult
        {
            public string FileName { get; set; }
            public bool IsDefectedInName { get; set; }
            public bool DetectedByAlgorithm { get; set; }
            public bool IsCorrect { get; set; }
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
                    var totalTime = Stopwatch.StartNew();

                    foreach (string filePath in ofd.FileNames)
                    {
                        bool isDefectedInName = filePath.IndexOf("Defected", StringComparison.OrdinalIgnoreCase) >= 0;

                        try
                        {
                            using (Bitmap bmp = new Bitmap(filePath))
                            {
                                // === ТУТ ВЖЕ БУЛО ПРАВИЛЬНО, ВИКОРИСТОВУВАВСЯ КЛАС ===
                                DetectDefectClass detect = new DetectDefectClass(
                                    bmp.Width,
                                    bmp.Height,
                                    (int)numericUpDown1.Value,
                                    minimumIntensity,
                                    maximumIntensity,
                                    ((int)numericUpDown4.Value * (maximumIntensity - minimumIntensity)) / 100,
                                    isVerticalScan
                                );

                                DefectDefectResult detectedByAlgorithm = detect.DetectDefect(bmp, null);

                                bool isCorrect = (isDefectedInName == detectedByAlgorithm.hasDefect);

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
                    totalTime.Stop();
                    toolStripStatusLabel3.Text = "Загальний час: " + totalTime.ElapsedMilliseconds + " мс";

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
                    writer.WriteLine("FileName,HasDefectInName,DetectedByAlgorithm,IsCorrect");
                    int correctCount = 0;

                    foreach (var res in _analysisResults)
                    {
                        writer.WriteLine($"{res.FileName},{res.IsDefectedInName},{res.DetectedByAlgorithm},{res.IsCorrect}");
                        if (res.IsCorrect) correctCount++;
                    }

                    double accuracy = 0;
                    if (_analysisResults.Count > 0)
                    {
                        accuracy = (double)correctCount / _analysisResults.Count * 100.0;
                    }

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