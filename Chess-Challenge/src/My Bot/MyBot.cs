using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

class PSTManager {
    private static sbyte[] LongTo8Sbytes(long input) {
        sbyte[] ret = new sbyte[8];
        foreach (int i in Enumerable.Range(0, 8)) {
            ret[i] = (sbyte) input;
            input = (input >> 8);
        };
        return ret.Reverse().ToArray();
    }

    public static sbyte[] DecodePST(long[] encoded) {
        sbyte[] ret = new sbyte[64];
        foreach (int i in Enumerable.Range(0, 8)) {
            sbyte[] decodedRow = LongTo8Sbytes(encoded[i]);
            foreach (int j in Enumerable.Range(0, 8)) {
                ret[(i * 8) + j] = decodedRow[j];
            }
        }
        return ret;
    }
}

public class MyBot : IChessBot {
    // https://www.chessprogramming.org/Simplified_Evaluation_Function
    Dictionary<PieceType, int> pieceValues = new Dictionary<PieceType, int>{
        {PieceType.Pawn, 100},
        {PieceType.Knight, 320},
        {PieceType.Bishop, 330},
        {PieceType.Rook, 500},
        {PieceType.Queen, 900},
    };

    // These PSTs are created using encode_pst.py
    Dictionary<PieceType, sbyte[]> PST = new Dictionary<PieceType, sbyte[]> {
        {PieceType.Pawn, PSTManager.DecodePST(new long[] {0, 3617008641903833650, 723412809732590090, 361706447983740165, 86234890240, 431208669220633349, 363114732645386757, 0})},
        {PieceType.Knight, PSTManager.DecodePST(new long[] {-3541831642829891378, -2815875667013341992, -2161716761344737054, -2160303867343993374, -2161711242227547934, -2160309386461182494, -2815875645454619432, -3541831642829891378})},
        {PieceType.Bishop, PSTManager.DecodePST(new long[] {-1371637495921969428, -720575940379279114, -720570399703367434, -719163024819812874, -720564902144900874, -717750152377791754, -719168565495724554, -1371637495921969428})},
        {PieceType.Rook, PSTManager.DecodePST(new long[] {0, 363113758191127045, -360287970189639429, -360287970189639429, -360287970189639429, -360287970189639429, -360287970189639429, 21558722560})},
        {PieceType.Queen, PSTManager.DecodePST(new long[] {-1371637474363246868, -720575940379279114, -720570421262089994, -360282451072450309, -360282451072450560, -720570421262088714, -720575940378951434, -1371637474363246868})},
        {PieceType.King, PSTManager.DecodePST(new long[] {-2100690843423155998, -2100690843423155998, -2100690843423155998, -2100690843423155998, -1377289115042389268, -653887343544177418, 1446781380292776980, 1449607125176819220})}
    };

    static double INF = 2147483646;

    public Move Think(Board board, Timer timer) {
        int sign = board.IsWhiteToMove ? 1 : -1;
        (double value, Move move) = negamax(board, 4, -INF, INF, sign);
        Console.WriteLine($"eval: {value}");
        return move;
    }

    private (double, Move) negamax(Board board, int depth, double alpha, double beta, int sign) {

        if (depth == 0 || board.IsDraw() || board.IsInCheckmate()) {
            double score = sign * eval(board);
            return (score * (1 + 0.001 * depth),  Move.NullMove);
        }

        Span<Move> moves = new Move[218];
        board.GetLegalMovesNonAlloc(ref moves);

        Move move = Move.NullMove;
        double value = -INF;
        foreach (Move childMove in moves) {

            board.MakeMove(childMove);
            (double childValue, _) = negamax(board, depth - 1, -beta, -alpha, -sign);
            childValue = -childValue;
            board.UndoMove(childMove);

            if (childValue > value) {
                value = childValue;
                move = childMove;
            }
            alpha = Math.Max(alpha, value);
            if (alpha >= beta) {
                break;
            }
        }
        return (value, move);
    }

    private double eval(Board board) {
        var segment = new ArraySegment<ulong>(board.GameRepetitionHistory, 0, board.GameRepetitionHistory.Length - 1);
        if (segment.Contains(board.ZobristKey) || board.IsDraw()) {
            return 0;
        }
        double score = 0;
        if (board.IsInCheckmate()) {
            if (board.IsWhiteToMove) {
                score -= 999999;
            } else {
                score += 999999;
            }
        }
        foreach ((PieceType type, int value) in pieceValues) {
            if (type == PieceType.None) {
                continue;
            }
            PieceList whitePieces = board.GetPieceList(type, true);
            PieceList blackPieces = board.GetPieceList(type, false);

            // Material count
            score += (whitePieces.Count - blackPieces.Count) * value;

            // Development / positioning
            foreach (Piece whitePiece in whitePieces) {
                score += PST[type][whitePiece.Square.Index];
            }
            foreach (Piece blackPiece in blackPieces) {
                score -= PST[type][63 - blackPiece.Square.Index];
            }
        }
        return score;
    }
}
