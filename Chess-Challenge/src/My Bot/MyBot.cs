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

    Dictionary<ulong, int> valueCache = new Dictionary<ulong, int>();

    public Move Think(Board board, Timer timer) {
        Console.WriteLine($"Thinking... ");
        (int value, Move move) = minmax(board, 4, int.MinValue, int.MaxValue, board.IsWhiteToMove);
        return move;
    }

    private (int, Move) minmax(Board board, int depth, int alpha, int beta, bool maximize) {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate()) {
            return (eval(board), Move.NullMove);
        }
        if (maximize) {
            int value = int.MinValue;
            Move bestMove = Move.NullMove;
            foreach (Move move in OrderedMoves(board, false)) {
                board.MakeMove(move);
                (int childValue, _) = minmax(board, depth - 1, alpha, beta, false);
                valueCache.Remove(board.ZobristKey);
                valueCache.Add(board.ZobristKey, childValue);
                board.UndoMove(move);
                if (childValue > value) {
                    value = childValue;
                    bestMove = move;
                }
                value = Math.Max(value, childValue);
                if (value > beta) {
                    break;
                }
                alpha = Math.Max(alpha, value);
            }
            return (value, bestMove);
        } else {
            int value = int.MaxValue;
            Move bestMove = Move.NullMove;
            foreach (Move move in OrderedMoves(board, true)) {
                board.MakeMove(move);
                (int childValue, _) = minmax(board, depth - 1, alpha, beta, true);
                valueCache.Remove(board.ZobristKey);
                valueCache.Add(board.ZobristKey, childValue);
                board.UndoMove(move);
                if (childValue < value) {
                    value = childValue;
                    bestMove = move;
                }
                if (value < alpha) {
                    break;
                }
                beta = Math.Min(beta, value);
            }
            return (value, bestMove);
        }
    }

    private List<Move> OrderedMoves(Board board, bool reverse) {
        List<Tuple<Move, int>> results = new List<Tuple<Move, int>>();
        foreach (Move move in board.GetLegalMoves()) {
            board.MakeMove(move);
            results.Add(Tuple.Create(move, valueCache.GetValueOrDefault(board.ZobristKey)));
            board.UndoMove(move);
        }
        if (reverse) {
            results.Sort((x, y) => x.Item2 - y.Item2);
        } else {
            results.Sort((x, y) => y.Item2 - x.Item2);
        }
        List<Move> ret = new List<Move>();
        foreach ((Move move, _) in results) {
            ret.Add(move);
        }
        return ret;
    }

    private int eval(Board board) {
        if (board.IsInCheckmate()) {
            if (board.IsWhiteToMove) {
                return -99999;
            } else {
                return 99999;
            }
        }
        if (board.IsDraw()) {
            return 0;
        }
        int score = 0;
        foreach ((PieceType type, int value) in pieceValues) {
            PieceList whitePieces = board.GetPieceList(type, true);
            PieceList blackPieces= board.GetPieceList(type, false);
            score += (whitePieces.Count * value) - (blackPieces.Count * value);
        }
        return score;
    }
}
