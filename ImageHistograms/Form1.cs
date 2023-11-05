namespace ImageHistograms
{
    public partial class Form1 : Form
    {
        private Bitmap _image;
        public Form1()
        {
            InitializeComponent();
        }

        private void �������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _image = new Bitmap(openFileDialog1.FileName);
                pictureBox1.Image = _image;
                int width = _image.Width;
                int height = _image.Height;

                // ���������� �������
                for (int x = 0; x < width; x++)
                {
                    // ���������� �����
                    for (int y = 0; y < height; y++)
                    {
                        // �������� ������ ������ �� ������� (x, y)
                        Color pixelColor = _image.GetPixel(x, y);

                        // ����� �� ������ ��������������� pixelColor ��� ��������� �������� RGB ����������
                        int red = pixelColor.R;
                        int green = pixelColor.G;
                        int blue = pixelColor.B;

                        // ��� �� ������ �������� ������ �������� � �������� ������
                        // ���������, ����� �������� �� �������� ��� �������� �����.

                        // � ������ ������� ������ �������� ���������� ��� ����� ������:
                        Console.WriteLine($"Pixel at ({x}, {y}): R = {red}, G = {green}, B = {blue}");
                    }
                }
            }
        }
    }
}