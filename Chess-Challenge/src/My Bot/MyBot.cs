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

    // (board z, move hash): minmax(board.MakeMove(move), ...)
    Dictionary<(ulong, int), int> valueCache = new Dictionary<(ulong, int), int>();

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
            foreach (Move move in PrioritisedMoves(board)) {
                board.MakeMove(move);
                (int childValue, _) = minmax(board, depth - 1, alpha, beta, false);
                board.UndoMove(move);
                UpdateValueCache(board, move, childValue);
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
            foreach (Move move in PrioritisedMoves(board)) {
                board.MakeMove(move);
                (int childValue, _) = minmax(board, depth - 1, alpha, beta, true);
                board.UndoMove(move);
                UpdateValueCache(board, move, childValue);
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

    private void UpdateValueCache(Board board, Move move, int value) {
        var key = (board.ZobristKey, move.GetHashCode());
        valueCache.Remove(key);
        valueCache.Add(key, value);
    }

    private List<Move> PrioritisedMoves(Board board) {
        return board.GetLegalMoves()
            .OrderByDescending(move => valueCache.GetValueOrDefault((board.ZobristKey, move.GetHashCode())))
            .ToList();
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
