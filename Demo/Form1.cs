using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Demo
{
    public partial class Form1 : Form
    {
        // Khai báo biến game 
        private Game game;
        private List<Button> cardButtons;
        private int firstIndex = -1;
        private List<bool> matchedCards;
        private Timer messageTimer;
        private Timer flipBackTimer;
        private int wrongFirstIndex = -1;
        private int wrongSecondIndex = -1;
        // Khởi tạo game
        private int remainingSeconds = 0;
        private int totalTimePlayed = 0;
        private bool isSoundOn = true;
        private bool isPaused = false;
        private int highScore = 0;
        private string historyFilePath = Path.Combine(Application.StartupPath, "history.txt");
        public Form1()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;
            this.Shown += Form1_Shown;

            // Khởi tạo các thành phần
            game = new Game(1);
            cardButtons = new List<Button>();
            matchedCards = new List<bool>();

            // Timer thông báo
            messageTimer = new Timer();
            messageTimer.Interval = 700;
            messageTimer.Tick += MessageTimer_Tick;

            // Timer lật lại ô sai
            flipBackTimer = new Timer();
            flipBackTimer.Interval = 700;
            flipBackTimer.Tick += FlipBackTimer_Tick;

            

            remainingSeconds = game.TimeLimit;
            totalTimePlayed = 0;
            LoadHighScore();
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            CreateMenuButtons();

            pnlResult.Left = (this.ClientSize.Width - pnlResult.Width) / 2;
            pnlResult.Top = (this.ClientSize.Height - pnlResult.Height) / 2;
        }
        private void CreateMenuButtons()
        {
            // Tạo hình tròn cho các nút chức năng
            btnSound.Region = CreateCircleRegion(btnSound.Width);
            btnPause.Region = CreateCircleRegion(btnPause.Width);
            btnHelp.Region = CreateCircleRegion(btnHelp.Width);
            btnRestart.Region = CreateCircleRegion(btnRestart.Width);
        }
        private Region CreateCircleRegion(int size)
        {
            GraphicsPath path = new GraphicsPath();

            path.AddEllipse(0, 0, size, size);

            return new Region(path);
        }
        private void gameTimerDesign_Tick(object sender, EventArgs e)
        {
            remainingSeconds--;
            totalTimePlayed++;

            int minutes = remainingSeconds / 60;
            int seconds = remainingSeconds % 60;

            lblTime.Text = "Thời gian: "
                + minutes.ToString("00")
                + ":"
                + seconds.ToString("00");

            if (remainingSeconds <= 0)
            {
                gameTimerDesign.Stop();

                ShowResult(false);
            }
        }
        // Tạo các ô bài
        private void CreateCards()
        {
            // Hiển thị Level hiện tại
            lblLevel.Text = "Level: " + game.Level;

            int columns;

            // Xác định số cột theo từng level
            if (game.Level == 1)
            {
                columns = 2;
            }
            else if (game.Level <= 8)
            {
                columns = 4;
            }
            else
            {
                columns = 5;
            }

            // Tạo các ô bài
            for (int i = 0; i < game.Cards.Count; i++)
            {
                Button button = new Button();

                int rows = (game.Cards.Count + columns - 1) / columns;

                int margin = 40;
                int gap = 20;

                // Chừa khoảng trống bên phải để đặt các nút chức năng
                int sidePanelWidth = 150;

                int availableWidth = this.ClientSize.Width - margin * 2 - sidePanelWidth;
                int availableHeight = this.ClientSize.Height - 100 - margin;

                int cardSize = Math.Min(
                    (availableWidth - gap * (columns - 1)) / columns,
                    (availableHeight - gap * (rows - 1)) / rows
                );

                button.Width = cardSize;
                button.Height = cardSize;
                button.Font = new Font("Times New Roman", 32, FontStyle.Bold);

                int totalGridWidth = columns * cardSize + (columns - 1) * gap;
                int startX = margin + (availableWidth - totalGridWidth) / 2;

                button.Left = startX + (i % columns) * (cardSize + gap);
                button.Top = 80 + (i / columns) * (cardSize + gap);

                // Hiển thị mặt sau
                button.Text = "?";

                // Gán sự kiện click
                button.Click += Card_Click;

                cardButtons.Add(button);
                matchedCards.Add(false);
                this.Controls.Add(button);
            }
        }
        // Chuyển sang Level tiếp theo
        private void NextLevel()
        {
            // Nếu đã hoàn thành Level 10 thì dừng game
            if (game.Level >= 10)
            {
                ShowMessage("Chúc mừng bạn đã hoàn thành tất cả level!");
                return;
            }

            // Xóa các ô cũ
            foreach (Button button in cardButtons)
            {
                this.Controls.Remove(button);
            }

            // Tạo level tiếp theo
            game = new Game(game.Level + 1, game.Score);

            if (game.Level == 6)
            {
                gameTimerDesign.Stop();

                MessageBox.Show(
                    "Từ Level 6 trở đi:\n\n" +
                    "• Đúng liên tiếp: điểm tăng theo Combo.\n" +
                    "• Cặp đúng đầu tiên: +1 điểm.\n" +
                    "• Cặp tiếp theo: +2 điểm.\n" +
                    "• Cặp tiếp theo nữa: +3 điểm...\n" +
                    "• Nếu sai: -1 điểm và Combo trở về 0.",
                    "Luật mới - Level 6",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }

            remainingSeconds = game.TimeLimit;

            lblTime.Text = "Thời gian: "
                + (remainingSeconds / 60).ToString("00")
                + ":"
                + (remainingSeconds % 60).ToString("00");

            gameTimerDesign.Start();

            // Xóa danh sách cũ
            cardButtons.Clear();
            matchedCards.Clear();

            // Reset ô đầu tiên
            firstIndex = -1;

            // Tạo bàn chơi mới
            CreateCards();
        }
        // Hiển thị thông báo
        private void ShowMessage(string message)
        {
            lblMessage.Text = message;

            if (message == "Chúc mừng bạn đã hoàn thành tất cả level!")
            {
                messageTimer.Interval = 5000; // Hiện 5 giây
            }
            else
            {
                messageTimer.Interval = 700; // Các thông báo khác hiện 0,7 giây
            }

            messageTimer.Stop();
            messageTimer.Start();
        }
        private void ShowResult(bool isWin)
        {
            lblResult.Text = "KẾT QUẢ";

            lblScoreResult.Text = "Tổng điểm: " + game.Score;

            int minutes = totalTimePlayed / 60;
            int seconds = totalTimePlayed % 60;

            lblTimeResult.Text = "Thời gian chơi: "
                + minutes.ToString("00")
                + ":"
                + seconds.ToString("00");

            if (game.Score > highScore)
            {
                highScore = game.Score;

                string filePath = Path.Combine(Application.StartupPath, "highscore.txt");
                File.WriteAllText(filePath, highScore.ToString());
            }

            SaveHistory();

            lblHighScore.Text = "Kỷ lục: " + highScore;

            pnlResult.Visible = true;
        }
        private void SaveHistory()
        {
            List<string> history = new List<string>();
            if (File.Exists(historyFilePath))
            {
                history.AddRange(File.ReadAllLines(historyFilePath));
            }
            string newRecord = "Điểm: "
                + game.Score
                + " | Thời gian: "
                + (totalTimePlayed / 60).ToString("00")
                + ":"
                + (totalTimePlayed % 60).ToString("00");

            history.Add(newRecord);
            history = history
        .OrderByDescending(x =>
        {
            int scoreStart = x.IndexOf(": ") + 2;
            int scoreEnd = x.IndexOf(" |");
            return int.Parse(x.Substring(scoreStart, scoreEnd - scoreStart));
        })
        .ThenBy(x =>
        {
            int timeStart = x.LastIndexOf(": ") + 2;
            string[] time = x.Substring(timeStart).Split(':');

            int minutes = int.Parse(time[0]);
            int seconds = int.Parse(time[1]);

            return minutes * 60 + seconds;
        })
        .ToList();

            File.WriteAllLines(historyFilePath, history);
        }

        // Xóa thông báo sau 700ms
        private void MessageTimer_Tick(object sender, EventArgs e)
        {
            lblMessage.Text = "";
            messageTimer.Stop();
        }

        // Lật lại hai ô sai
        private void FlipBackTimer_Tick(object sender, EventArgs e)
        {
            if (wrongFirstIndex != -1 && wrongSecondIndex != -1)
            {
                cardButtons[wrongFirstIndex].Text = "?";
                cardButtons[wrongSecondIndex].Text = "?";
            }

            wrongFirstIndex = -1;
            wrongSecondIndex = -1;

            flipBackTimer.Stop();
        }
        // Xử lý khi người chơi chọn ô
        private void Card_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;

            // Lấy vị trí ô được chọn
            int index = cardButtons.IndexOf(button);

            // Bỏ qua ô đã tìm đúng
            if (matchedCards[index])
            {
                return;
            }

            // Không chọn cùng một ô hai lần
            if (index == firstIndex)
            {
                return;
            }

            // Lật ô thứ nhất
            if (firstIndex == -1)
            {
                firstIndex = index;
                button.Text = game.Cards[index].ToString();
                return;
            }

            // Lật ô thứ hai
            button.Text = game.Cards[index].ToString();

            // Kiểm tra 2 ô có giống nhau không
            bool isMatch = game.CheckMatch(firstIndex, index);

            if (isMatch)
            {
                // Cập nhật điểm
                lblScore.Text = "Điểm: " + game.Score;

                if (game.Level >= 6)
                {
                    lblCombo.Text = "Combo: " + game.Combo;
                }

                // Đánh dấu hai ô đã tìm đúng
                matchedCards[firstIndex] = true;
                matchedCards[index] = true;

                // Kiểm tra hoàn thành Level
                if (game.IsLevelComplete())
                {
                    lblCombo.Text = "";

                    lblMessage.ForeColor = Color.Red;
                    ShowMessage("Hoàn thành màn " + game.Level + "!");

                    if (game.Level < 10)
                    {
                        NextLevel();
                    }
                    else
                    {
                        gameTimerDesign.Stop();

                        lblMessage.ForeColor = Color.Red;

                        ShowResult(true);
                    }
                }
            }
            else
            {
                // Cập nhật điểm
                lblScore.Text = "Điểm: " + game.Score;

                lblCombo.Text = "";

                // Lưu hai ô sai
                wrongFirstIndex = firstIndex;
                wrongSecondIndex = index;

                // Bắt đầu Timer lật lại
                flipBackTimer.Stop();
                flipBackTimer.Start();
            }

            // Reset để chọn cặp tiếp theo
            firstIndex = -1;
        }


        private void BtnHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "HƯỚNG DẪN CHƠI\n\n" +
                "• Bấm vào 2 thẻ để lật thẻ.\n" +
                "• Nếu 2 thẻ giống nhau → ghép thành công.\n" +
                "• Nếu 2 thẻ khác nhau → thẻ sẽ úp lại.\n\n" +

                "LUẬT TÍNH ĐIỂM\n\n" +
                "• Level 1–5: Mỗi cặp đúng được +1 điểm.\n" +
                "• Level 1–5: Cặp sai không bị trừ điểm.\n" +
                "• Level 6–10: Bắt đầu tính Combo.\n" +
                "• Đúng liên tiếp: +1, +2, +3... theo Combo.\n" +
                "• Sai ở Level 6–10: -1 điểm và Combo về 0.\n\n" +

                "CÁC LEVEL\n\n" +
                "• Game gồm 10 Level.\n" +
                "• Level 1: 2 cặp.\n" +
                "• Level 2: 4 cặp.\n" +
                "• Level 3: 6 cặp.\n" +
                "• Level 4: 8 cặp.\n" +
                "• Level 5: 8 cặp.\n" +
                "• Level 6: 4 cặp và bắt đầu tính Combo.\n" +
                "• Level 7: 6 cặp.\n" +
                "• Level 8: 8 cặp.\n" +
                "• Level 9: 10 cặp.\n" +
                "• Level 10: 10 cặp.\n\n" +

                "• Hoàn thành tất cả các cặp để chuyển sang Level tiếp theo.\n" +
                "• Sau Level 5, Level 6 bắt đầu với 4 cặp và áp dụng luật Combo.\n" +
                "• Bấm nút Ⅱ để tạm dừng và ▶ để tiếp tục.\n" +
                "• Bấm ↻ để chơi lại từ đầu.",
                "Hướng dẫn chơi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        private void BtnSound_Click(object sender, EventArgs e)
        {
            Button btnSound = (Button)sender;

            isSoundOn = !isSoundOn;

            if (isSoundOn)
            {
                btnSound.Text = "🔊";
            }
            else
            {
                btnSound.Text = "🔇";
            }
        }
        private void BtnPause_Click(object sender, EventArgs e)
        {
            Button btnPause = (Button)sender;

            isPaused = !isPaused;

            if (isPaused)
            {
                gameTimerDesign.Stop();
                btnPause.Text = "▶";
            }
            else
            {
                gameTimerDesign.Start();
                btnPause.Text = "Ⅱ";
            }
        }
        private void BtnRestart_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Bạn chắc chắn muốn chơi lại từ đầu?",
                "Xác nhận chơi lại",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question
            );

            // Nếu bấm Cancel thì tiếp tục chơi bình thường
            if (result == DialogResult.Cancel)
            {
                return;
            }

            // Nếu bấm OK thì chơi lại từ đầu
            gameTimerDesign.Stop();
            flipBackTimer.Stop();
            messageTimer.Stop();

            // Tạo lại game từ Level 1, điểm 0
            game = new Game(1, 0);

            // Reset thời gian
            remainingSeconds = game.TimeLimit;
            totalTimePlayed = 0;

            // Reset trạng thái
            firstIndex = -1;
            wrongFirstIndex = -1;
            wrongSecondIndex = -1;
            isPaused = false;

            // Xóa các ô cũ
            foreach (Button button in cardButtons)
            {
                this.Controls.Remove(button);
                button.Dispose();
            }
            // Xóa danh sách cũ
            cardButtons.Clear();
            matchedCards.Clear();

            // Xóa thông báo
            lblMessage.Text = "";

            // Cập nhật điểm và thời gian
            lblScore.Text = "Điểm: 0";
            lblTime.Text = "Thời gian: "
                + (remainingSeconds / 60).ToString("00")
                + ":"
                + (remainingSeconds % 60).ToString("00");

            // Tạo lại Level 1
            CreateCards();

            // Đưa nút Dừng về trạng thái ban đầu
            Button btnPause = this.Controls
                .OfType<Button>()
                .FirstOrDefault(b => b.Text == "▶");

            if (btnPause != null)
            {
                btnPause.Text = "Ⅱ";
            }

            // Bắt đầu chơi lại
            gameTimerDesign.Start();
        }
        private void RestartGame()
        {
            gameTimerDesign.Stop();
            flipBackTimer.Stop();
            messageTimer.Stop();

            game = new Game(1, 0);

            remainingSeconds = game.TimeLimit;
            totalTimePlayed = 0;

            firstIndex = -1;
            wrongFirstIndex = -1;
            wrongSecondIndex = -1;
            isPaused = false;

            foreach (Button button in cardButtons)
            {
                this.Controls.Remove(button);
                button.Dispose();
            }

            cardButtons.Clear();
            matchedCards.Clear();

            lblMessage.Text = "";
            lblCombo.Text = "";
            lblScore.Text = "Điểm: 0";

            lblTime.Text = "Thời gian: "
                + (remainingSeconds / 60).ToString("00")
                + ":"
                + (remainingSeconds % 60).ToString("00");

            btnPause.Text = "Ⅱ";

            CreateCards();

            gameTimerDesign.Start();
        }

        private void btnPlayAgain_Click(object sender, EventArgs e)
        {
            pnlResult.Visible = false;
            RestartGame();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            gameTimerDesign.Stop();

            pnlResult.Visible = false;

            lblLevel.Visible = false;
            lblScore.Visible = false;
            lblTime.Visible = false;
            btnSound.Visible = false;
            btnPause.Visible = false;
            btnHelp.Visible = false;
            btnRestart.Visible = false;

            pnlMenu.Visible = true;
        }

        private void LoadHighScore()
        {
            string filePath = Path.Combine(Application.StartupPath, "highscore.txt");

            if (File.Exists(filePath))
            {
                string text = File.ReadAllText(filePath);

                int.TryParse(text, out highScore);
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            pnlMenu.Visible = false;
            pnlHistory.Visible = false;

            lblLevel.Visible = true;
            lblScore.Visible = true;
            lblTime.Visible = true;
            btnSound.Visible = true;
            btnPause.Visible = true;
            btnHelp.Visible = true;
            btnRestart.Visible = true;

            CreateCards();
            gameTimerDesign.Start();
        }

        private void btnHistory_Click(object sender, EventArgs e)
        {
            gameTimerDesign.Stop();
            flipBackTimer.Stop();
            messageTimer.Stop();

            foreach (Button button in cardButtons)
            {
                this.Controls.Remove(button);
                button.Dispose();
            }

            cardButtons.Clear();
            matchedCards.Clear();

            lstHistory.Items.Clear();

            if (File.Exists(historyFilePath))
            {
                string[] lines = File.ReadAllLines(historyFilePath);

                foreach (string line in lines)
                {
                    lstHistory.Items.Add(line);
                }
            }

            pnlMenu.Visible = false;

            lblLevel.Visible = false;
            lblScore.Visible = false;
            lblTime.Visible = false;
            btnSound.Visible = false;
            btnPause.Visible = false;
            btnHelp.Visible = false;
            btnRestart.Visible = false;

            pnlHistory.Left = (this.ClientSize.Width - pnlHistory.Width) / 2;
            pnlHistory.Top = (this.ClientSize.Height - pnlHistory.Height) / 2;

            pnlHistory.Visible = true;
        }

        private void btnCloseHistory_Click(object sender, EventArgs e)
        {
            pnlHistory.Visible = false;
            pnlMenu.Visible = true;
        }
    }
}
