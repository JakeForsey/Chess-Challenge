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
        (int value, Move move) = minmax(board, 3, int.MinValue, int.MaxValue, board.IsWhiteToMove);
        return move;
    }

    private (int, Move) minmax(Board board, int depth, int alpha, int beta, bool maximize) {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate()) {
            return (eval(board), Move.NullMove);
        }
        if (maximize) {
            int value = int.MinValue;
            Move bestMove = Move.NullMove;
            foreach ((Move move, Board child) in OrderedChildren(board)) {
                (int childValue, _) = minmax(child, depth - 1, alpha, beta, false);
                valueCache.Remove(child.ZobristKey);
                valueCache.Add(child.ZobristKey, childValue);
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
            foreach ((Move move, Board child) in OrderedChildren(board)) {
                (int childValue, _) = minmax(child, depth - 1, alpha, beta, true);
                valueCache.Remove(child.ZobristKey);
                valueCache.Add(child.ZobristKey, childValue);
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

    private Board branch(Board board, Move move) {
        Board next = Board.CreateBoardFromFEN(board.GetFenString());
        next.MakeMove(move);
        return next;
    }

    private List<Tuple<Move, Board>> OrderedChildren(Board board) {
        List<Tuple<Move, Board>> results = new List<Tuple<Move, Board>>();
        foreach (Move move in board.GetLegalMoves()) {
            Board nextBoard = branch(board, move);
            results.Add(Tuple.Create(move, nextBoard));
        }
        results.Sort((x, y) => valueCache.GetValueOrDefault(y.Item2.ZobristKey) - valueCache.GetValueOrDefault(x.Item2.ZobristKey));
        return results;
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
