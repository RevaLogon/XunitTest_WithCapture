using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GetStartedApp.ViewModels;
using Xunit;

namespace GetStartedApp.Tests
{
    public class UITests : IDisposable
    {
        private Process? _ffmpegProcess;
        private IClassicDesktopStyleApplicationLifetime? _appLifetime;
        private MainWindow? _mainWindow;
        private string _videoFilePath;
        private bool _testPassed;

        public UITests()
        {
            _videoFilePath = $"/home/kuzu/Videos/CalculatorTest_{DateTime.Now:yyyyMMdd_HHmmss}.mov";

            StartScreenRecording();

            // Avalonia uygulamasını başlat
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());

            _mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };

            // Ana pencereyi bekle
            Task.Delay(2000).Wait();

            _appLifetime = (IClassicDesktopStyleApplicationLifetime?)Application.Current?.ApplicationLifetime;
        }

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                      .UsePlatformDetect()
                      .LogToTrace();

        [Fact]
        public void CalculatorTest()
        {
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    Console.WriteLine("Starting UI interactions...");

                    var firstNumberTextBox = _mainWindow?.FindControl<TextBox>("FirstNumberTextBox");
                    var secondNumberTextBox = _mainWindow?.FindControl<TextBox>("SecondNumberTextBox");
                    var addButton = _mainWindow?.FindControl<Button>("AddCommand");
                    var resultTextBlock = _mainWindow?.FindControl<TextBlock>("ResultTextBlock");

                    // UI elemanlarının bulunduğunu kontrol et
                    if (firstNumberTextBox == null)
                    {
                        Console.WriteLine("FirstNumberTextBox control not found.");
                        Assert.Fail("FirstNumberTextBox control not found.");
                    }
                    if (secondNumberTextBox == null)
                    {
                        Console.WriteLine("SecondNumberTextBox control not found.");
                        Assert.Fail("SecondNumberTextBox control not found.");
                    }
                    if (addButton == null)
                    {
                        Console.WriteLine("AddCommand button not found.");
                        Assert.Fail("AddCommand button not found.");
                    }
                    if (resultTextBlock == null)
                    {
                        Console.WriteLine("ResultTextBlock control not found.");
                        Assert.Fail("ResultTextBlock control not found.");
                    }

                    Console.WriteLine("Interacting with UI elements...");

                    firstNumberTextBox.Text = "6";
                    secondNumberTextBox.Text = "3";

                    var tcs = new TaskCompletionSource<bool>();
                    addButton.Click += (sender, e) => tcs.SetResult(true);

                    addButton?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                    tcs.Task.Wait();

                    Task.Delay(500).Wait();

                    Console.WriteLine("Checking result...");

                    var expectedResult = "9";
                    _testPassed = resultTextBlock.Text == expectedResult;

                    Assert.Equal(expectedResult, resultTextBlock.Text);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                _testPassed = false;
                throw;
            }
            finally
            {
                StopScreenRecording();

                if (File.Exists(_videoFilePath))
                {
                    try
                    {
                        if (_testPassed)
                        {
                            File.Delete(_videoFilePath);
                        }
                        else
                        {
                            Console.WriteLine($"Test failed. Video saved at: {_videoFilePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to handle video file: {ex.Message}");
                    }
                }

                Dispose();
            }
        }

        [Fact]
        public void FakeTest_Fail()
        {
            Assert.True(false, "This is a fake test that fails.");
        }

        private void StartScreenRecording()
        {
            var ffmpegPath = "/usr/bin/ffmpeg";
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-f x11grab -video_size 1366x768 -framerate 25 -i :0.0 -c:v libx264 -preset ultrafast -crf 18 \"{_videoFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _ffmpegProcess = new Process { StartInfo = startInfo };
            _ffmpegProcess.Start();
        }

        private void StopScreenRecording()
        {
            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
            {
                try
                {
                    _ffmpegProcess.Kill();
                    _ffmpegProcess.WaitForExit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to stop recording: {ex.Message}");
                }
                finally
                {
                    _ffmpegProcess.Close();
                }
            }
        }

        public void Dispose()
        {
            _mainWindow?.Close();
            Task.Delay(500).Wait();
        }
    }
}
