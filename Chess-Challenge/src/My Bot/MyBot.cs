using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;


public class MyBot : IChessBot {

    Dictionary<PieceType, int> pieceValues = new Dictionary<PieceType, int>{
        {PieceType.Pawn, 1},
        {PieceType.Knight, 3},
        {PieceType.Bishop, 3},
        {PieceType.Rook, 5},
        {PieceType.Queen, 8},
    };
    Dictionary<Tuple<ulong, bool>, Tuple<int, Move, int>> cache = new Dictionary<Tuple<ulong, bool>, Tuple<int, Move, int>>();

    public Move Think(Board board, Timer timer) {
        Console.WriteLine($"Thinking... [cache: {cache.Count}]");
        (int value, Move move, int depth) = minmax(board, 3, board.IsWhiteToMove);
        Console.WriteLine($"Eval: {value}, {depth}");
        return move;
    }

    // private Tuple<int, Move, int> cachedMinmax(Board board, int depth, bool maximize) {
    //     Tuple<ulong, bool> key = Tuple.Create(board.ZobristKey, maximize);
    //     if (cache.ContainsKey(key)) {
    //         return cache[key];
    //     }
    //     Tuple<int, Move, int> result = minmax(board, depth, maximize);
    //     cache[key] = result;
    //     return result;
    // }

    private Tuple<int, Move, int> minmax(Board board, int depth, bool maximize) {
        if (depth == 0) {
            return Tuple.Create(eval(board), Move.NullMove, depth);
        }
        Func<int, int, bool> func = maximize ? gt : lt;
        int bestValue = maximize ? int.MinValue : int.MaxValue;
        Move bestMove = Move.NullMove;
        foreach (Move move in board.GetLegalMoves()) {
            (int value, Move _, int __) = minmax(branch(board, move), depth - 1, !maximize);
            if (func(value, bestValue)) {
                bestValue = value;
                bestMove = move;
            }
        }
        return Tuple.Create(bestValue, bestMove, depth);
    }

    private bool gt(int value, int bestValue) {
        return value > bestValue;
    }

    private bool lt(int value, int bestValue) {
        return value < bestValue;
    }

    private Board branch(Board board, Move move) {
        Board next = Board.CreateBoardFromFEN(board.GetFenString());
        next.MakeMove(move);
        return next;
    }

    private int eval(Board board) {
        int score = 0;
        foreach ((PieceType type, int value) in pieceValues) {
            PieceList whitePieces = board.GetPieceList(type, true);
            PieceList blackPieces= board.GetPieceList(type, false);
            score += (whitePieces.Count * value) - (blackPieces.Count * value);
        }
        if (board.IsInCheckmate()) {
            if (board.IsWhiteToMove) {
                score -= 99999;
            } else {
                score += 99999;
            }
        }
        return score;
    }
}
