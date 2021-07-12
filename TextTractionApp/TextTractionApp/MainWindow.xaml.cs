using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Tesseract;
using System.Threading;
using System.Drawing;
using AForge;
using AForge.Controls;
using AForge.Imaging;
using AForge.Vision;
using System.IO;
using AForge.Imaging.Filters;

namespace TextTractionApp
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage biImage, biEdit, biFinal;

        public MainWindow()
        {
            InitializeComponent();
            textBoxOCR.IsEnabled = false;
            textBoxOCRImageUpgrade.IsEnabled = false;
        }

        #region UI Elements
        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            UploadImage();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal) this.WindowState = WindowState.Maximized;
            else this.WindowState = WindowState.Normal;
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        #endregion

        private void UploadImage()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Imagenes (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";
            if (ofd.ShowDialog() == true)
            {
                string fileName = ofd.FileName;
                imgImage.Source = biImage = new BitmapImage(new Uri(fileName));
                textBoxOCRImageUpgrade.Clear();
                textBoxOCR.Clear();
            }
        }

        public BitmapImage upgradeImage()
        {
            // using a Gray Scale filter
            using (var grayscaleFilter = Grayscale.CommonAlgorithms.BT709.Apply(BitmapImageToBitmap(biImage)))
            {
                // create instance of skew checker
                DocumentSkewChecker skewChecker = new DocumentSkewChecker();
                // get documents skew angle
                double angle = skewChecker.GetSkewAngle(grayscaleFilter);
                // create rotation filter
                RotateBilinear rotationFilter = new RotateBilinear(-angle);
                rotationFilter.FillColor = System.Drawing.Color.White;
                // rotate image applying the filter
                Bitmap rotatedImage = rotationFilter.Apply(grayscaleFilter);

                // aplicatting a Threshold filter
                BradleyLocalThresholding thresholdF = new BradleyLocalThresholding();
                Bitmap thresholdImage = thresholdF.Apply(rotatedImage);

                // Reducing brightness
                BrightnessCorrection filterBright = new BrightnessCorrection(-40);
                Bitmap brightnessImage = filterBright.Apply(thresholdImage);

                // add Sharpen filter
                Sharpen filterSharpen = new Sharpen();
                Bitmap sharpenImage = filterSharpen.Apply(brightnessImage);

                biEdit = ToBitmapImage(sharpenImage);
            }
            return biEdit;
        }

        private void btnExtractionImageText_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(new ThreadStart(ThreadProc));
            t.Start();
        }

        public void ThreadProc()
        {
            
            biFinal = upgradeImage();
            var img = new Bitmap(BitmapImageToBitmap(biFinal)); //Pix.LoadFromFile
            var ocrengine = new TesseractEngine(@".\tessdata", "spa", EngineMode.Default);
            var res = ocrengine.Process(img);
            this.Dispatcher.Invoke(() =>
            {
                textBoxOCRImageUpgrade.Clear();
                textBoxOCRImageUpgrade.IsEnabled = true;
                string resText = res.GetText();
                textBoxOCRImageUpgrade.Text = resText.Trim();
            });
        }

        #region BitmapsHelpers
        private BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        private void btnExtractionImageWithoutEdit_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(new ThreadStart(ThreadProcWithoutEdit));
            t.Start();
        }

        public void ThreadProcWithoutEdit()
        {
            
            var img = new Bitmap(BitmapImageToBitmap(biImage)); //Pix.LoadFromFile
            var ocrengine = new TesseractEngine(@".\tessdata", "spa", EngineMode.Default);
            var res = ocrengine.Process(img);
            this.Dispatcher.Invoke(() =>
            {
                textBoxOCR.Clear();
                textBoxOCR.IsEnabled = true;
                string resText = res.GetText();
                textBoxOCR.Text = resText;
            });
        }

        private Bitmap BitmapImageToBitmap(BitmapImage bitmapImg)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImg));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);
                return new Bitmap(bitmap);
            }
        }
        #endregion

    }
}