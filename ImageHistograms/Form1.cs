namespace ImageHistograms
{
    public partial class Form1 : Form
    {
        private Bitmap _image;
        public Form1()
        {
            InitializeComponent();
        }

        private void відкритиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _image = new Bitmap(openFileDialog1.FileName);
                pictureBox1.Image = _image;
                int width = _image.Width;
                int height = _image.Height;

                // Перебираємо стовбці
                for (int x = 0; x < width; x++)
                {
                    // Перебираємо рядки
                    for (int y = 0; y < height; y++)
                    {
                        // Отримуємо кольор пікселя на позиції (x, y)
                        Color pixelColor = _image.GetPixel(x, y);

                        // Тепер ви можете використовувати pixelColor для отримання значення RGB інформації
                        int red = pixelColor.R;
                        int green = pixelColor.G;
                        int blue = pixelColor.B;

                        // Тут ви можете виконати потрібні операції з кольором пікселя
                        // Наприклад, можна зберегти ці значення або виконати аналіз.

                        // В даному прикладі просто виведемо інформацію про кожен піксель:
                        Console.WriteLine($"Pixel at ({x}, {y}): R = {red}, G = {green}, B = {blue}");
                    }
                }
            }
        }
    }
}