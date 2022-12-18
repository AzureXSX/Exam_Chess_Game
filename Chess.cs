using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Exam_Chess_Game
{
    public class Chess
    {
        public ChessBoard Board { get; private set; }
        public Player Turn { get; private set; }
        public position_t Selection { get; private set; }

        private UIBoard m_UI;
        public int m_nPlayers;

        public bool time_win_black = false;
        public bool time_win_white = false;
        public bool time_win = false;

        public Chess(UIBoard ui, int nPlayers, bool setupBoard = true)
        {
            this.m_UI = ui;
            this.m_UI.SetStatus(true, "Generating...");


            this.m_nPlayers = nPlayers;

            this.Turn = Player.WHITE;

            this.Board = new ChessBoard();
            if (setupBoard)
            {
                this.Board.SetInitialPlacement();
            }

            this.m_UI.SetBoard(Board);
            this.m_UI.SetStatus(false, "White's turn.");
        }

        public void AISelect()
        {
            while (AI.RUNNING)
            {
                Thread.Sleep(100);
            }

            this.m_UI.SetStatus(true, "Thinking...");

            move_t move = AI.MiniMaxAB(this.Board, this.Turn);

            if (move.to.letter >= 0 && move.to.number >= 0)
            {
                MakeMove(move);
            }
            else 
            {
                if (!AI.STOP)
                {
                    this.m_UI.LogMove("Null Move\n");
                }
            }

            bool checkmate = false;

            if (!AI.STOP)
            {
                switchPlayer();
                checkmate = detectCheckmate();
            }

            AI.RUNNING = false;

            if (!AI.STOP && this.m_nPlayers == 0 && !checkmate)
            {
                new Thread(AISelect).Start();
            }
        }

        public List<position_t> Select(position_t pos, ref bool m_checkmate)
        {
            if (this.Board.Grid[this.Selection.number][this.Selection.letter].piece != Piece.NONE
                && this.Turn == this.Board.Grid[this.Selection.number][this.Selection.letter].player
                && (this.m_nPlayers == 2
                || this.Turn == Player.WHITE))
            {
                List<position_t> moves = LegalMoveSet.getLegalMove(this.Board, this.Selection);
                foreach (position_t move in moves)
                {
                    if (move.Equals(pos))
                    {
                        MakeMove(new move_t(this.Selection, pos));

                        if (this.Board.Grid[pos.number][pos.letter].piece == Piece.KING && Math.Abs(pos.letter - this.Selection.letter) == 2)
                        {
                            int row = (this.Turn == Player.WHITE) ? 0 : 7;

                            if (pos.letter < 4)
                            {
                                LegalMoveSet.move(this.Board, new move_t(new position_t(0, row), new position_t(3, row)));
                            }
                            else
                            {
                                LegalMoveSet.move(this.Board, new move_t(new position_t(7, row), new position_t(5, row)));
                            }
                        }

                        switchPlayer();
                        if (detectCheckmate()) {
                            m_checkmate = true;
                            return new List<position_t>(); }

                        if (this.m_nPlayers == 1) 
                        {
                            new Thread(AISelect).Start(); 
                        }
                        return new List<position_t>();
                    }
                }
            }

            if (this.Board.Grid[pos.number][pos.letter].player == this.Turn && (this.m_nPlayers == 2 || this.Turn == Player.WHITE))
            {
                List<position_t> moves = LegalMoveSet.getLegalMove(this.Board, pos);
                this.Selection = pos;
                return moves;
            }

            this.Selection = new position_t();
            return new List<position_t>();
        }

        private void MakeMove(move_t m)
        {
            string move = (this.Turn == Player.WHITE) ? "W" : "B";

            move += ":\t";

            switch (this.Board.Grid[m.from.number][m.from.letter].piece)
            {
                case Piece.PAWN:
                    move += "P";
                    break;
                case Piece.ROOK:
                    move += "R";
                    break;
                case Piece.KNIGHT:
                    move += "k";
                    break;
                case Piece.BISHOP:
                    move += "B";
                    break;
                case Piece.QUEEN:
                    move += "Q";
                    break;
                case Piece.KING:
                    move += "K";
                    break;
            }

            if (this.Board.Grid[m.to.number][m.to.letter].piece != Piece.NONE || LegalMoveSet.isEnPassant(this.Board, m))
            {
                move += "x";
            }

            switch (m.to.letter)
            {
                case 0: move += "a"; break;
                case 1: move += "b"; break;
                case 2: move += "c"; break;
                case 3: move += "d"; break;
                case 4: move += "e"; break;
                case 5: move += "f"; break;
                case 6: move += "g"; break;
                case 7: move += "h"; break;
            }

            move += (m.to.number + 1).ToString();

            this.Board = LegalMoveSet.move(this.Board, m);

            if (LegalMoveSet.isCheck(this.Board, (Turn == Player.WHITE) ? Player.BLACK : Player.WHITE))
            {
                move += "+";
            }

            this.m_UI.LogMove("\n" + move + "\n");
        }

        private void switchPlayer()
        {
            this.Turn = (this.Turn == Player.WHITE) ? Player.BLACK : Player.WHITE;
            this.m_UI.SetTurn(this.Turn);
            this.m_UI.SetStatus(false, ((this.Turn == Player.WHITE) ? "White" : "Black") + "'s Turn.");
            this.m_UI.SetBoard(this.Board);
        }

        public bool detectCheckmate()
        {
            bool wkingonly = this.Board.Pieces[Player.WHITE].Count == 1 && this.Board.Grid[this.Board.Pieces[Player.WHITE][0].number][this.Board.Pieces[Player.WHITE][0].letter].piece == Piece.KING;
            bool bkingonly = this.Board.Pieces[Player.BLACK].Count == 1 && this.Board.Grid[this.Board.Pieces[Player.BLACK][0].number][this.Board.Pieces[Player.BLACK][0].letter].piece == Piece.KING;
            if (!LegalMoveSet.hasMoves(this.Board, this.Turn))
            {
                if (LegalMoveSet.isCheck(this.Board, this.Turn))
                {
                    this.m_UI.LogMove("Checkmate!\n");
                    this.m_UI.SetStatus(false, ((this.Turn == Player.WHITE) ? "Black" : "White") + " wins!");
                }
                else
                {
                    this.m_UI.LogMove("Stalemate!\n");
                }
                return true;
            }
            else if (wkingonly && bkingonly)
            {
                this.m_UI.LogMove("Draw.\n");
                this.m_UI.SetStatus(false, "Draw");
                return true;
            }
            return false;
        }


        public bool time_win_check()
        {
            if (time_win_black || time_win_white)
            {
                if (time_win_white)
                {
                    this.m_UI.SetStatus(false, "Black wins!");

                }
                else
                {
                    this.m_UI.SetStatus(false, "White wins!");
                }
                return true;
            }
            else
                return false;

        }
    }
}
