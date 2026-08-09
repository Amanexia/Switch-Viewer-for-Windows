using NAudio.Wave;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SwitchViewer;

public partial class MainWindow : Window
{
    private VideoCapture? _capture;
    private readonly Mat _frame = new();
    private readonly DispatcherTimer _timer;
    private WaveInEvent? _waveIn;
    private WaveOutEvent? _waveOut;
    private BufferedWaveProvider? _audioBuffer;
    private bool _muted;
    private bool _fullscreen;
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private bool _topMost;

    public MainWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(15)
        };
        _timer.Tick += CaptureFrame;

        Loaded += (_, _) => Start();
        Closed += (_, _) => Stop();
    }

    private void Start()
    {
        // Generic UVC capture cards commonly appear as "USB3.0 Video".
        // Try several DirectShow indexes so this still works if another camera is index 0.
        for (int i = 0; i < 5 && _capture is null; i++)
        {
            var cap = new VideoCapture(i, VideoCaptureAPIs.DSHOW);
            if (cap.IsOpened())
            {
                _capture = cap;
                _capture.Set(VideoCaptureProperties.FrameWidth, 1920);
                _capture.Set(VideoCaptureProperties.FrameHeight, 1080);
                _capture.Set(VideoCaptureProperties.Fps, 60);
                StatusText.Text = $"Video: device {i}  |  low-latency preview";
            }
            else
            {
                cap.Dispose();
            }
        }

        if (_capture is null)
        {
            StatusText.Text = "No video capture device found.";
            return;
        }

        StartAudio();
        _timer.Start();
    }

    private void CaptureFrame(object? sender, EventArgs e)
    {
        if (_capture is null) return;

        try
        {
            if (_capture.Read(_frame) && !_frame.Empty())
            {
                BitmapSource bitmap = BitmapSourceConverter.ToBitmapSource(_frame);
                bitmap.Freeze();
                VideoImage.Source = bitmap;
            }
        }
        catch
        {
            // Keep the preview alive if a single frame fails.
        }
    }

    private void StartAudio()
    {
        try
        {
            // Pick a likely capture-card audio endpoint.
            // Generic cards often expose names containing USB, Capture, HDMI or Digital.
            int selected = -1;
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                string name = caps.ProductName ?? "";
                if (name.Contains("USB", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Capture", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("HDMI", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Digital", StringComparison.OrdinalIgnoreCase))
                {
                    selected = i;
                    break;
                }
            }

            if (selected < 0 && WaveIn.DeviceCount > 0)
                selected = 0;

            if (selected < 0)
            {
                StatusText.Text += "  |  no audio device";
                return;
            }

            _audioBuffer = new BufferedWaveProvider(new WaveFormat(48000, 16, 2))
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromMilliseconds(250)
            };

            _waveOut = new WaveOutEvent
            {
                DeviceNumber = -1,
                Volume = 1.0f
            };
            _waveOut.Init(_audioBuffer);
            _waveOut.Play();

            _waveIn = new WaveInEvent
            {
                DeviceNumber = selected,
                WaveFormat = new WaveFormat(48000, 16, 2),
                BufferMilliseconds = 30
            };
            _waveIn.DataAvailable += (_, e) =>
            {
                if (!_muted)
                    _audioBuffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };
            _waveIn.StartRecording();

            var audioName = WaveIn.GetCapabilities(selected).ProductName;
            StatusText.Text += $"  |  audio: {audioName}";
        }
        catch (Exception ex)
        {
            StatusText.Text += $"  |  audio unavailable: {ex.Message}";
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_waveOut != null)
            _waveOut.Volume = (float)(VolumeSlider.Value / 100.0);
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        _muted = !_muted;
        MuteButton.Content = _muted ? "Unmute" : "Mute";
    }

    private void TopButton_Click(object sender, RoutedEventArgs e)
    {
        _topMost = !_topMost;
        Topmost = _topMost;
    }

    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullscreen();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11 || (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Alt))
            ToggleFullscreen();

        if (e.Key == Key.Escape && _fullscreen)
            ToggleFullscreen();
    }

    private void ToggleFullscreen()
    {
        if (!_fullscreen)
        {
            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;

            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _fullscreen = true;
        }
        else
        {
            WindowStyle = _previousWindowStyle;
            WindowState = _previousWindowState;
            _fullscreen = false;
        }
    }

    private void Stop()
    {
        _timer.Stop();

        try { _waveIn?.StopRecording(); } catch { }
        _waveIn?.Dispose();
        _waveOut?.Stop();
        _waveOut?.Dispose();

        _capture?.Release();
        _capture?.Dispose();
        _frame.Dispose();
    }
}
