using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exam_Chess_Game
{
    public interface UIBoard
    {
        void SetBoard(ChessBoard board);
        void LogMove(string line);
        void SetStatus(bool thinking, string message);
        void SetTurn(Player p);
    }
}
