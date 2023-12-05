using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PainterDam
{
    
    public partial class MainWindow : Window
    {

        private Stack<StrokeCollection> listaDeshacer = new Stack<StrokeCollection>();
        private Stack<StrokeCollection> listaRehacer = new Stack<StrokeCollection>();
        private StrokeCollection currentStrokes = new StrokeCollection();

        private Ellipse elipseSeleccionada;
        private ScaleTransform selectedScaleTransform;
        private TranslateTransform selectedTranslateTransform;

        public MainWindow()
        {
            InitializeComponent();

            PanelDibujo.StrokeCollected += InkCanvas_StrokeCollected;

            Storyboard PanelColores = Resources["HideWrapPanel"] as Storyboard;
            PanelColores?.Begin();

        }

        private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var ellipse = (Ellipse)sender;
            SolidColorBrush brush = (SolidColorBrush)ellipse.Fill;
            Color color = brush.Color;

            PanelDibujo.DefaultDrawingAttributes.Color = color;

            AnimacionAgrandar(ellipse);
        }

        private void AnimacionAgrandar(Ellipse elipsePulsada)
        {

            if (elipseSeleccionada != null)
            {
                ResetEllipse(elipseSeleccionada);
            }

            elipseSeleccionada = elipsePulsada;

            var newTransformGroup = new TransformGroup();
            var scaleTransform = new ScaleTransform(1, 1);
            var translateTransform = new TranslateTransform(-2, -2); 
            newTransformGroup.Children.Add(scaleTransform);
            newTransformGroup.Children.Add(translateTransform);

            elipseSeleccionada.RenderTransform = newTransformGroup;

            var scaleAnimation = new DoubleAnimation
            {
                To = 1.1,
                Duration = TimeSpan.FromSeconds(0.0) 
            };

            var translateAnimation = new DoubleAnimation
            {
                To = -1,
                Duration = TimeSpan.FromSeconds(0.2) 
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

            translateTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, translateAnimation);


        }

        private void ResetEllipse(Ellipse ellipse)
        {
            // Detener todas las animaciones en la elipse anterior
            ellipse.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            ellipse.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            ellipse.BeginAnimation(TranslateTransform.XProperty, null);
            ellipse.BeginAnimation(TranslateTransform.YProperty, null);

            // Restaurar el tamaño y la posición original
            var originalTransform = new TransformGroup();
            originalTransform.Children.Add(new ScaleTransform());
            originalTransform.Children.Add(new TranslateTransform());
            ellipse.RenderTransform = originalTransform;
        }

        private void CambiarModoSeleccion_Click(object sender, RoutedEventArgs e)
        {
            PanelDibujo.EditingMode = InkCanvasEditingMode.Select;
            SeleccionMenu.IsEnabled = false;
            TrazoMenu.IsEnabled = true;

        }

        private void CambiarModoPintar_Click(object sender, RoutedEventArgs e)
        {
            PanelDibujo.EditingMode = InkCanvasEditingMode.Ink;
            SeleccionMenu.IsEnabled = true;
            TrazoMenu.IsEnabled = false;
        }

        private void GuardarImagen_Click(object sender, RoutedEventArgs e) => GuardarImagen();
        

        private bool GuardarImagen()
        {
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)PanelDibujo.ActualWidth, (int)PanelDibujo.ActualHeight, 96, 96, PixelFormats.Default);

            renderTargetBitmap.Render(PanelDibujo);

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Imagen";
            dlg.DefaultExt = ".png";
            dlg.Filter = "Archivos de imagen|*.png;*.jpg;*.bmp|PNG (.png)|*.png|JPEG (.jpg)|*.jpg|Bitmap (.bmp)|*.bmp";

            if (dlg.ShowDialog() == true)
            {
                string extension = System.IO.Path.GetExtension(dlg.FileName);

                BitmapEncoder encoder;
                if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    encoder = new PngBitmapEncoder();
                }
                else if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    encoder = new JpegBitmapEncoder();
                }
                else if (extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
                {
                    encoder = new BmpBitmapEncoder();
                }
                else
                {
                    MessageBox.Show("Formato de archivo no compatible.");
                    return false;
                }

                encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                using (FileStream fs = new FileStream(dlg.FileName, FileMode.Create))
                {
                    encoder.Save(fs);
                }
                Title = dlg.FileName;
                return true;
            }
            return false;
        }

        private void AbrirImagen_Click(object sender, RoutedEventArgs e)
         {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Seleccionar una imagen";
                dlg.Filter = "Archivos de imagen|*.png;*.jpg;*.jpeg;*.bmp|Todos los archivos|*.*";

                if (dlg.ShowDialog() == true)
                {
                    try
                    {

                        PanelDibujo.Strokes.Clear();
                        listaDeshacer.Push(currentStrokes);
                        currentStrokes = PanelDibujo.Strokes.Clone();
                        listaDeshacer.Clear();

                        BitmapImage bitmap = new BitmapImage(new Uri(dlg.FileName));

                        Image nuevaImagen = new Image();
                        nuevaImagen.Source = bitmap;

                        PanelDibujo.Children.Add(nuevaImagen);

                        Title = dlg.SafeFileName;
                }   
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al abrir la imagen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
         }

        private void DeshacerMenu_Click(object sender, RoutedEventArgs e)
        {
            if (listaDeshacer.Count > 0)
            {
                listaRehacer.Push(currentStrokes);
                currentStrokes = listaDeshacer.Pop();
                PanelDibujo.Strokes = currentStrokes.Clone();
            }
        }

        private void RehacerMenu_Click(object sender, RoutedEventArgs e)
        {
            if (listaRehacer.Count > 0)
            {
                listaDeshacer.Push(currentStrokes);
                currentStrokes = listaRehacer.Pop();
                PanelDibujo.Strokes = currentStrokes.Clone();
            }
        }

        private void InkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            listaDeshacer.Push(currentStrokes.Clone());
            currentStrokes = PanelDibujo.Strokes.Clone();
            listaRehacer.Clear(); 
        }

        private void LimpiarMenu_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("¿Quieres guardar antes de limpiar?", "Confirmación", MessageBoxButton.YesNoCancel);

            // Procesar la respuesta
            if (result == MessageBoxResult.Yes)
            {
                bool res = GuardarImagen();
                if (res)
                {
                    PanelDibujo.Children.Clear();
                    PanelDibujo.Strokes.Clear();
                    currentStrokes = new StrokeCollection();
                    Title = "Nuevo";
                }
            }
            else if (result == MessageBoxResult.No)
            {
                PanelDibujo.Children.Clear();
                PanelDibujo.Strokes.Clear();
                currentStrokes = new StrokeCollection();
                Title = "Nuevo";
            }
        }

        private void SalirMenu_Click (object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (PanelDibujo.Strokes.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show("¿Quieres guardar antes de cerrar?", "Confirmación",
                    MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                {
                    bool res = GuardarImagen();
                    if (!res)
                    {
                        e.Cancel = true;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private void ColoresMenu_Click (object sender , RoutedEventArgs e)
        {
            if (ColoresMenu.IsChecked)
            {
                Storyboard PanelColores = Resources["ShowWrapPanel"] as Storyboard;
                PanelColores?.Begin();
            }
            else {
                Storyboard PanelColores = Resources["HideWrapPanel"] as Storyboard;
                PanelColores?.Begin();
            }
        }
    }
}