using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DzFile_encryption
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancelSource;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChooseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == true)
                filePathTextBox.Text = fileDialog.FileName;
        }

        private async void Execute_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(filePathTextBox.Text) || !File.Exists(filePathTextBox.Text))
            {
                MessageBox.Show("Выберите правильный файл!");
                return;
            }
            if (string.IsNullOrEmpty(passwordBox.Password))
            {
                MessageBox.Show("Укажите пароль!");
                return;
            }

            cancelSource = new CancellationTokenSource();
            cancelButton.IsEnabled = true;
            progressIndicator.Value = 0;
            executeButton.IsEnabled = false;

            string encryptionKey = passwordBox.Password;
            string selectedFile = filePathTextBox.Text;
          
            var progress = new Progress<int>(percent => progressIndicator.Value = percent);

            try
            {
                await ProcessFileAsync(selectedFile, encryptionKey, progress, cancelSource.Token);
                MessageBox.Show("Операция завершена!");
                ResetUI();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Операция была отменена");
                ResetUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка: " + ex.Message);
            }
            finally
            {
                executeButton.IsEnabled = true;
                cancelButton.IsEnabled = false;
                cancelSource?.Dispose();
                cancelSource = null;
            }
        }
         
        private void CancelOperation_Click(object sender, RoutedEventArgs e)
        {
            cancelSource?.Cancel();
        }

        private async Task ProcessFileAsync(
            string filePath,
            string key,
            IProgress<int> progress,
            CancellationToken token)
        {
            byte[] dataBuffer = await File.ReadAllBytesAsync(filePath, token);
            token.ThrowIfCancellationRequested();
            byte[] encryptionKey = Encoding.UTF8.GetBytes(key);
            int totalSize = dataBuffer.Length;
            int lastProgress = 0;
            Random rng = new Random();
            int processingTime = rng.Next(3000, 7001);
            int delayStep = 100;
            int iterations = processingTime / delayStep;

            await Task.Run(async () =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    token.ThrowIfCancellationRequested();
                    int startIdx = (i * totalSize) / iterations;
                    int endIdx = ((i + 1) * totalSize) / iterations;
                    for (int j = startIdx; j < endIdx; j++)
                    {
                        dataBuffer[j] ^= encryptionKey[j % encryptionKey.Length];
                    }
                    lastProgress = (i * 100) / iterations;
                    progress.Report(lastProgress);
                    await Task.Delay(delayStep, token);
                }
                progress.Report(0);
            }, token);
            token.ThrowIfCancellationRequested();
            await File.WriteAllBytesAsync(filePath, dataBuffer, token);
        }

        private void ResetUI()
        {
            filePathTextBox.Text = string.Empty;
            passwordBox.Password = string.Empty;
            progressIndicator.Value = 0;
        }
    }
}