using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_Chess_Game
{
    public partial class MainForm : Form, UIBoard
    {
        private ToolStripMenuItem temp;
        TimeSpan m_whiteTime = new TimeSpan(0);
        TimeSpan m_blackTime = new TimeSpan(0);
        const int PADDING = 10;
        const string DATA_PATH = "../../data/";
        bool m_aigame = false;
        bool m_checkmate = false;
        bool m_manualBoard = false;
        bool m_finalizedBoard = false;

        Chess chess;

        private PictureBox[][] Board;
        Graphics graphics = new Graphics(DATA_PATH);
        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CreateBoard();
            txtLog.Enabled = true;
            txtLog.ReadOnly = true;

            picTurn.SizeMode = PictureBoxSizeMode.StretchImage;
            picTurn.Image = graphics.TurnIndicator[Player.WHITE];

            temp = mnuDif3;
            AI.DEPTH = 3;

            SetStatus(false, "Choose New Game");

            endCurrentGameToolStripMenuItem.Enabled = false;
        }

        private void windowClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }

        private void Shutdown(object sender, EventArgs e)
        {
            Stop();
            this.Close();
        }

        private void endGame(object sender, EventArgs e)
        {
            Stop();
        }

        private void NewGame(object sender, EventArgs e)
        {
            ToolStripMenuItem button = (ToolStripMenuItem)sender;
            if (button.Text.StartsWith("New AI vs AI"))
            {
                difficultyDepthToolStripMenuItem.Enabled = false;
                NewGame(0);
            }
            else if (button.Text.StartsWith("New AI vs Player"))
            {
                difficultyDepthToolStripMenuItem.Enabled = false;
                NewGame(1);
            }
            else if (button.Text.StartsWith("New Player"))
            {

                difficultyDepthToolStripMenuItem.Enabled = false;
                NewGame(2);
            }
        }

        private void Difficulty(object sender, EventArgs e)
        {
            if (temp != null)
            {
                temp.CheckState = CheckState.Unchecked;
            }

            bool was = AI.RUNNING;
            AI.STOP = true;

            temp = (ToolStripMenuItem)sender;
            temp.CheckState = CheckState.Checked;

            AI.DEPTH = Int32.Parse((String)temp.Tag) + 1;
            txtLog.Text = "";
            LogMove("AI Difficulty " + (string)temp.Tag + "\n\n");

            if (was)
            {
                LogMove("AI Replaying Move\n");
                new Thread(chess.AISelect).Start();
            }
        }

        
        private void tmrWhite_Tick(object sender, EventArgs e)
        {
            m_whiteTime = m_whiteTime.Add(new TimeSpan(0, 0, 0, 0, tmrWhite.Interval));
            lblWhiteTime.Text = string.Format("{1:d2}:{2:d2}.{3:d1}", m_whiteTime.Hours, m_whiteTime.Minutes, m_whiteTime.Seconds, m_whiteTime.Milliseconds / 100);
            if(m_whiteTime.TotalMinutes >= 5)
            {
                tmrWhite.Stop();
                tmrBlack.Stop();
                AI.STOP = true;
                LogMove("Time is over, Black wins");
                SetStatus(false, "Black wins, End current game");
                m_checkmate = true;
                for (int i = 0; i < 8; i++)
                    for (int j = 0; j < 8; j++)
                        Board[i][j].BackColor = ((i + j) % 2 == 0) ? Color.SaddleBrown : Color.Beige;
            }
        }

        private void tmrBlack_Tick(object sender, EventArgs e)
        {
            m_blackTime = m_blackTime.Add(new TimeSpan(0, 0, 0, 0, tmrBlack.Interval));
            lblBlackTime.Text = string.Format("{1:d2}:{2:d2}.{3:d1}", m_blackTime.Hours, m_blackTime.Minutes, m_blackTime.Seconds, m_blackTime.Milliseconds / 100);
            if (m_blackTime.TotalMinutes >= 5)
            {
                tmrBlack.Stop();
                tmrWhite.Stop();
                AI.STOP = true;
                LogMove("Time is over, White wins");
                SetStatus(false, "White wins, End current game");
                m_checkmate = true;
                for (int i = 0; i < 8; i++)
                    for (int j = 0; j < 8; j++)
                        Board[i][j].BackColor = ((i + j) % 2 == 0) ? Color.SaddleBrown : Color.Beige;
            }
        }
     

        private void CreateBoard()
        {
            Board = new PictureBox[8][];
            for (int i = 0; i < 8; i++)
            {
                Board[i] = new PictureBox[8];
                for (int j = 0; j < 8; j++)
                {
                    Board[i][j] = new PictureBox();
                    Board[i][j].Parent = this.splitView.Panel1;
                    Board[i][j].Click += BoardClick;
                    Board[i][j].BackColor = ((j + i) % 2 == 0) ? Color.SaddleBrown : Color.Beige;
                    Board[i][j].SizeMode = PictureBoxSizeMode.StretchImage;
                }
            }

            ResizeBoard(null, null);
        }

        private void ResizeBoard(object sender, EventArgs e)
        {
            if (Board == null || Board[0] == null || Board[0][0] == null) return;

            int val = Math.Min(this.splitView.Panel1.Height - PADDING * 2, this.splitView.Panel1.Width - PADDING * 2);

            int width = val / 8;
            int height = val / 8;

            int left = (this.splitView.Panel1.Width - val) / 2;
            int top = (this.splitView.Panel1.Height - val) / 2;

            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                {
                    Board[i][j].Left = j * width + left;
                    Board[i][j].Top = val - (i + 1) * height + top;
                    Board[i][j].Width = width;
                    Board[i][j].Height = height;
                }
        }

        private void SetPiece(Piece piece, Player player, int letter, int number)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetPiece(piece, player, letter, number)));
                return;
            }

            if (letter < 0 || letter > 7 || number < 0 || number > 7)
                return; 

            if (piece == Piece.NONE)
            {
                Board[number][letter].Image = null;
                Board[number][letter].Invalidate();
                return;
            }

            Board[number][letter].Image = graphics.Pieces[player][piece];
            Board[number][letter].Invalidate();
        }

        private void BoardClick(object sender, EventArgs e)
        {
            if (chess != null && !m_checkmate)
            {
                for (int i = 0; i < 8; i++)
                    for (int j = 0; j < 8; j++)
                        Board[i][j].BackColor = ((i + j) % 2 == 0) ? Color.SaddleBrown : Color.Beige;

                for (int i = 0; i < 8; i++)
                {
                    int k = Array.IndexOf(Board[i], sender);
                    if (k > -1)
                    {
                        if ((!m_manualBoard || m_finalizedBoard) && !m_aigame)
                        {
                            List<position_t> moves = chess.Select(new position_t(k, i), ref m_checkmate);
                            foreach (position_t move in moves)
                            {
                                if ((chess.Board.Grid[move.number][move.letter].player != chess.Turn
                                    && chess.Board.Grid[move.number][move.letter].piece != Piece.NONE)
                                    || LegalMoveSet.isEnPassant(chess.Board, new move_t(chess.Selection, move)))
                                {

                                    Board[move.number][move.letter].BackColor = Color.Red;
                                }
                                else
                                {
  
                                    Board[move.number][move.letter].BackColor = Color.Lime;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Stop()
        {
            SetStatus(false, "Choose New Game");

            AI.STOP = true;
            chess = null;

            SetTurn(Player.WHITE);

            tmrWhite.Stop();
            tmrBlack.Stop();
            m_whiteTime = new TimeSpan(0);
            m_blackTime = new TimeSpan(0);
            lblWhiteTime.Text = m_whiteTime.ToString("c");
            lblBlackTime.Text = m_blackTime.ToString("c");

            SetBoard(new ChessBoard());
            txtLog.Text = "";

            m_checkmate = false;
            m_aigame = false;
            m_finalizedBoard = false;

            endCurrentGameToolStripMenuItem.Enabled = false;
            difficultyDepthToolStripMenuItem.Enabled = true;
        }

        private void NewGame(int nPlayers)
        {
            if (!m_manualBoard) Stop();

            m_aigame = (nPlayers == 0);
            chess = new Chess(this, nPlayers, !m_manualBoard);

            SetTurn(Player.WHITE);
            SetStatus(false, "White's turn");

            m_whiteTime = new TimeSpan(0);
            m_blackTime = new TimeSpan(0);
            lblWhiteTime.Text = m_whiteTime.ToString("c");
            lblBlackTime.Text = m_blackTime.ToString("c");

            if (nPlayers < 2)
            {
                LogMove("AI Difficulty " + (string)temp.Tag + "\n");
            }

            SetStatus(false, "White's Turn");
            if (m_aigame)
            {
                new Thread(chess.AISelect).Start();
            }
            tmrWhite.Start();

            endCurrentGameToolStripMenuItem.Enabled = true;
            difficultyDepthToolStripMenuItem.Enabled = false;
        }

        public void SetTurn(Player p)
        {

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetTurn(p)));
                return;
            }

            if (chess != null)
            {
                picTurn.Image = graphics.TurnIndicator[chess.Turn];
            }
            else
            {
                picTurn.Image = graphics.TurnIndicator[Player.WHITE];
            }

            if (!m_manualBoard)
            {
                if (p == Player.WHITE)
                {
                    tmrBlack.Stop();
                    tmrWhite.Start();
                }
                else
                {
                    tmrWhite.Stop();
                    tmrBlack.Start();
                }

                if (chess != null && (m_checkmate || chess.detectCheckmate()))
                {
                    tmrWhite.Stop();
                    tmrBlack.Stop();
                }
            }
        }

        public void SetBoard(ChessBoard board)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetBoard(board)));
                return;
            }

            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    SetPiece(board.Grid[i][j].piece, board.Grid[i][j].player, j, i);
        }

        public void LogMove(string move)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => LogMove(move)));
                return;
            }

            lblWhiteCheck.Visible = false;
            lblBlackCheck.Visible = false;

            if (move.Contains("+"))
            {
                lblWhiteCheck.Visible = chess.Turn == Player.BLACK;
                lblBlackCheck.Visible = chess.Turn == Player.WHITE;
            }

            txtLog.AppendText(move);
            txtLog.AppendText(Environment.NewLine);
        }

        public void SetStatus(bool thinking, string message)
        {

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetStatus(thinking, message)));
                return;
            }

            lblStatus.Text = message;
            if (thinking)
            {
                prgThinking.MarqueeAnimationSpeed = 30;
                prgThinking.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                prgThinking.MarqueeAnimationSpeed = 0;
                prgThinking.Value = 0;
                prgThinking.Style = ProgressBarStyle.Continuous;
            }
        }
    }
}
