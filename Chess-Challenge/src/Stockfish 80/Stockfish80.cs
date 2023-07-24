using ChessChallenge.API;
using System;
using Stockfish.NET;

namespace ChessChallenge.Example
{
    public class Stockfish80 : IChessBot
    {
        IStockfish stockfish = new Stockfish.NET.Stockfish(@"C:\Users\jakef\OneDrive\Desktop\stockfish_20090216_x64.exe");
        Random random = new Random();

        public Move Think(Board board, Timer timer)
        {
            if (random.Next(1, 100) > 20) {
                Console.WriteLine("Stockfish");
                stockfish.SetFenPosition(board.GetFenString());
                string stringMove = stockfish.GetBestMove();
                Move move = new Move(stringMove, board);
                return move;
            } else {
                Console.WriteLine("Random!");
                Move[] moves = board.GetLegalMoves();
                return moves[random.Next(0, moves.Length)];
            }
        }
    }
}