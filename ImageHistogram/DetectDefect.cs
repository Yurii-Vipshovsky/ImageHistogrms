using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

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
        private int _width;
        private int _height;
        private int _intensityIntervals;
        private int _minimumIntensity;
        private int _maximumIntensity;
        private int _intensityRange;
        private int _defectsThreshold;
        private bool _isVerticalScan;

        public DetectDefectClass(int width, int height, int intensityIntervals, int minimumIntensity, int maximumIntensity, int defectsThreshold, bool isVerticalScan)
        {
            _width = width;
            _height = height;
            _intensityIntervals = intensityIntervals;
            _minimumIntensity = minimumIntensity;
            _maximumIntensity = maximumIntensity;
            _intensityRange = (_maximumIntensity - _minimumIntensity) / (intensityIntervals > 0 ? intensityIntervals : 1);
            if (_intensityRange == 0) _intensityRange = 1;
            _defectsThreshold = defectsThreshold;
            _isVerticalScan = isVerticalScan;
        }

        // Швидкий розрахунок піку
        private unsafe int CalcMostFrequentIntensity(BitmapData data)
        {
            int[] histogram = new int[256];
            byte* scan0 = (byte*)data.Scan0;
            int stride = data.Stride;
            int bpp = 3; // bpp - bytes per pixel - 24 bits ber pixel assumed (8bit per chanel)

            Parallel.For(0, _height, y =>
            {
                // for each thread lockal Histogram
                int[] localHist = new int[256];
                // move to y row using pointers
                byte* row = scan0 + (y * stride);
                for (int x = 0; x < _width; x++)
                {
                    // BGR - Blue Green Red - Windows save image data in this way
                    // Color image analys
                    //int val = (row[x * bpp] + row[x * bpp + 1] + row[x * bpp + 2]) / 3;

                    //Grey image analys 
                    int val = row[x * bpp];
                    localHist[val]++;
                }
                lock (histogram)
                {
                    for (int i = 0; i < 256; i++) histogram[i] += localHist[i];
                }
            });

            int maxVal = -1, maxIdx = 0;
            int end = Math.Min(_maximumIntensity, 255);
            int start = Math.Max(_minimumIntensity, 0);

            for (int i = start; i <= end; i++)
            {
                if (histogram[i] > maxVal) { maxVal = histogram[i]; maxIdx = i; }
            }
            return maxIdx;
        }

        public unsafe DefectDefectResult DetectDefect(Bitmap sourceImage, Bitmap _unusedHistogram)
        {
            Stopwatch sw = Stopwatch.StartNew();
            bool hasDefects = false;

            // Розміри результату
            int resWidth = _isVerticalScan ? _width : (_maximumIntensity - _minimumIntensity);
            int resHeight = _isVerticalScan ? (_maximumIntensity - _minimumIntensity) : _height;
            if (resWidth <= 0) resWidth = 1;
            if (resHeight <= 0) resHeight = 1;

            Bitmap resultBmp = new Bitmap(resWidth, resHeight, PixelFormat.Format24bppRgb);

            // Блокування пам'яті (LockBits)
            BitmapData srcData = sourceImage.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData resData = resultBmp.LockBits(new Rectangle(0, 0, resWidth, resHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            try
            {
                int mostFreq = CalcMostFrequentIntensity(srcData);

                byte* srcPtr = (byte*)srcData.Scan0;
                byte* resPtr = (byte*)resData.Scan0;
                int srcStride = srcData.Stride;
                int resStride = resData.Stride;

                // Константи для циклу
                int minI = _minimumIntensity;
                int maxI = _maximumIntensity;
                int range = _intensityRange;
                int intervals = _intensityIntervals;
                int threshold = _defectsThreshold;
                int lowBound = mostFreq - threshold;
                int highBound = mostFreq + threshold;

                // Використовуємо byte для прапорця, щоб уникнути lock у циклі (Interlocked повільний)
                // 0 = false, 1 = true
                int defectFlag = 0;

                if (_isVerticalScan)
                {
                    // === ВЕРТИКАЛЬНИЙ СКАН ===
                    // Проходимо по X (ширині)
                    Parallel.For(0, _width, x =>
                    {
                        // 1. Збираємо гістограму ВСЬОГО стовпця за ОДИН прохід
                        int* colHist = stackalloc int[256]; // Швидкий масив на стеку
                        // (stackalloc потребує unsafe, але це супершвидко і не навантажує GC)
                        // Треба занулити, бо stackalloc може мати сміття
                        for (int k = 0; k < 256; k++) colHist[k] = 0;

                        byte* pixelAddr;
                        for (int y = 0; y < _height; y++)
                        {
                            pixelAddr = srcPtr + (y * srcStride) + (x * 3);
                            int val = (pixelAddr[0] + pixelAddr[1] + pixelAddr[2]) / 3;
                            colHist[val]++;
                        }

                        // 2. Тепер проходимо лише по інтервалах і малюємо результат
                        // Тут немає циклу по _height, тому це миттєво
                        for (int i = 0; i < intervals; i++)
                        {
                            int startRange = i * range;
                            int endRange = (i + 1) * range;

                            int pixelCountInInterval = 0;
                            // Рахуємо суму пікселів у цьому діапазоні (швидко, бо діапазон малий)
                            for (int val = startRange; val < endRange; val++)
                            {
                                if (val >= minI && val <= maxI)
                                    pixelCountInInterval += colHist[val];
                            }

                            if (pixelCountInInterval == 0) continue; // Залишаємо чорним

                            double accumulator = 0;
                            for (int b = 0; b < range; b++)
                            {
                                int intensity = b + startRange; // Це координата Y на картинці результату
                                if (intensity >= resHeight) continue; // Захист

                                byte* resPixel = resPtr + (intensity * resStride) + (x * 3);

                                // Малюємо межі
                                if (intensity == highBound || intensity == lowBound)
                                {
                                    resPixel[0] = 212; resPixel[1] = 255; resPixel[2] = 127;
                                    continue;
                                }

                                // Ваша логіка накопичення
                                if (intensity >= minI && intensity <= maxI)
                                {
                                    accumulator += colHist[intensity];
                                    byte colorVal = (byte)((accumulator / pixelCountInInterval) * 255);

                                    resPixel[0] = colorVal;
                                    resPixel[1] = colorVal;
                                    resPixel[2] = colorVal;

                                    // Перевірка дефекту
                                    if (colorVal > 0 && (intensity > highBound || intensity < lowBound))
                                    {
                                        defectFlag = 1;
                                    }
                                }
                            }
                        }
                    });
                }
                else
                {
                    // === ГОРИЗОНТАЛЬНИЙ СКАН ===
                    // Проходимо по Y (висоті) джерела
                    Parallel.For(0, _height, ySrc =>
                    {
                        int* rowHist = stackalloc int[256];
                        for (int k = 0; k < 256; k++) rowHist[k] = 0;

                        // 1. Гістограма рядка за один прохід
                        byte* rowPtr = srcPtr + (ySrc * srcStride);
                        for (int x = 0; x < _width; x++)
                        {
                            int val = (rowPtr[x * 3] + rowPtr[x * 3 + 1] + rowPtr[x * 3 + 2]) / 3;
                            rowHist[val]++;
                        }

                        // 2. Малюємо результат
                        for (int i = 0; i < intervals; i++)
                        {
                            int startRange = i * range;
                            int endRange = (i + 1) * range;

                            int pixelCountInInterval = 0;
                            for (int val = startRange; val < endRange; val++)
                            {
                                if (val >= minI && val <= maxI)
                                    pixelCountInInterval += rowHist[val];
                            }

                            if (pixelCountInInterval == 0) continue;

                            double accumulator = 0;
                            for (int b = 0; b < range; b++)
                            {
                                int intensity = b + startRange; // Це координата X на картинці результату
                                if (intensity >= resWidth) continue;

                                byte* resPixel = resPtr + (ySrc * resStride) + (intensity * 3);

                                if (intensity == highBound || intensity == lowBound)
                                {
                                    resPixel[0] = 212; resPixel[1] = 255; resPixel[2] = 127;
                                    continue;
                                }

                                if (intensity >= minI && intensity <= maxI)
                                {
                                    accumulator += rowHist[intensity];
                                    byte colorVal = (byte)((accumulator / pixelCountInInterval) * 255);

                                    resPixel[0] = colorVal;
                                    resPixel[1] = colorVal;
                                    resPixel[2] = colorVal;

                                    if (colorVal > 0 && (intensity > highBound || intensity < lowBound))
                                    {
                                        defectFlag = 1;
                                    }
                                }
                            }
                        }
                    });
                }

                if (defectFlag == 1) hasDefects = true;

            }
            finally
            {
                sourceImage.UnlockBits(srcData);
                resultBmp.UnlockBits(resData);
            }

            sw.Stop();
            return new DefectDefectResult { hasDefect = hasDefects, result = resultBmp, time = sw.ElapsedMilliseconds };
        }
    }
}