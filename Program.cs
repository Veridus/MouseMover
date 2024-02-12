using System.Runtime.InteropServices;

namespace WinFormsApp1
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        private static Random random = new Random();
        private static Rectangle primaryScreen = Screen.PrimaryScreen.Bounds;
        private Thread mouseMoverThread;
        private volatile bool stopRequested = false;
        private Label statusLabel;
        private NumericUpDown sleepTimeInput;
        private Label sleepTimeLabel;
        private Label moveIntervalLabel;

        public MainForm()
        {
            InitializeFormComponents();
            this.Size = new Size(300, 250); // Adjust window size if necessary
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.KeyPreview = true; // Enable form to receive key events before they reach any controls
            this.KeyDown += MainForm_KeyDown; // Subscribe to the KeyDown event
        }

        private void InitializeFormComponents()
        {
            moveIntervalLabel = new Label
            {
                Text = "Move Interval (seconds):",
                Location = new Point(10, 10),
                Size = new Size(180, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(moveIntervalLabel);

            sleepTimeInput = new NumericUpDown
            {
                Location = new Point(190, 10),
                Size = new Size(60, 20),
                Minimum = 1, // Minimum sleep time in seconds
                Maximum = 60, // Maximum, adjust as needed
                Value = 10, // Default sleep time in seconds
                Increment = 1, // Step in seconds
                TextAlign = HorizontalAlignment.Right
            };
            Controls.Add(sleepTimeInput);

            sleepTimeLabel = new Label
            {
                Text = "sec",
                Location = new Point(255, 10),
                Size = new Size(30, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(sleepTimeLabel);

            Button startButton = new Button
            {
                Text = "Start",
                Location = new Point(10, 70),
                Size = new Size(120, 40),
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            startButton.Click += StartButton_Click;
            Controls.Add(startButton);

            Button stopButton = new Button
            {
                Text = "Stop(ESC)",
                Location = new Point(150, 70),
                Size = new Size(120, 40),
                BackColor = Color.LightCoral,
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            stopButton.Click += StopButton_Click;
            Controls.Add(stopButton);

            statusLabel = new Label
            {
                Text = "Status: Stopped",
                Location = new Point(10, 120),
                Size = new Size(280, 40),
                Font = new Font("Arial", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(statusLabel);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if the Escape key was pressed
            if (e.KeyCode == Keys.Escape)
            {
                StopMouseMovement(); // Call the method to stop the mouse movement
            }
        }

        private void StopMouseMovement()
        {
            if (stopRequested != true)
            {
                stopRequested = true; // Set stopRequested flag to true
                UpdateStatusLabel("Status: Stopping"); // Optionally update status label immediately
                                                       // If mouseMoverThread is active, you might want to join it or ensure it completes
                                                       // This is conceptual; actual implementation may vary based on the rest of your application logic
            }
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (mouseMoverThread == null || !mouseMoverThread.IsAlive)
            {
                stopRequested = false;
                mouseMoverThread = new Thread(MoveMouse)
                {
                    IsBackground = true
                };
                mouseMoverThread.Start();
                UpdateStatusLabel("Status: Running");
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            stopRequested = true;
            UpdateStatusLabel("Status: Stopping");
        }

        private void MoveMouse()
        {
            while (!stopRequested)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    Point cursorPosition = Cursor.Position;
                    int mouseX = cursorPosition.X;
                    int mouseY = cursorPosition.Y;
                    MoveMouseEaseInOutCubic(mouseX, mouseY);
                });
                Thread.Sleep((int)sleepTimeInput.Value * 1000); // Convert seconds to milliseconds
            }
            this.Invoke((MethodInvoker)delegate
            {
                UpdateStatusLabel("Status: Stopped");
            });
        }

        private void UpdateStatusLabel(string text)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke(new Action(() => statusLabel.Text = text));
            }
            else
            {
                statusLabel.Text = text;
            }
        }

        private static void MoveMouseEaseInOutCubic(int currentX, int currentY)
        {
            int targetX = random.Next(primaryScreen.Left, primaryScreen.Right);
            int targetY = random.Next(primaryScreen.Top, primaryScreen.Bottom);
            int duration = 1000;
            int steps = 200;
            int sleepTime = duration / steps;

            for (int i = 1; i <= steps && !Application.OpenForms[0].IsDisposed; i++)
            {
                float t = (float)i / steps;
                float easedT = EaseInOutCubic(t);
                int x = (int)Math.Round(Lerp(currentX, targetX, easedT));
                int y = (int)Math.Round(Lerp(currentY, targetY, easedT));
                int absoluteX = x * 65536 / primaryScreen.Width;
                int absoluteY = y * 65536 / primaryScreen.Height;
                mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, absoluteX, absoluteY, 0, 0);
                Thread.Sleep(sleepTime);
            }
        }

        private static float Lerp(float start, float end, float t)
        {
            return start + ((end - start) * t);
        }

        private static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4 * t * t * t : 0.5f * ((2 * t) - 2) * ((2 * t) - 2) * ((2 * t) - 2) + 1;
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
