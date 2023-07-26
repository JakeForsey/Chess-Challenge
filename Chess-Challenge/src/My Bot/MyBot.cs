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
    static sbyte[] whitePawnPST = PSTManager.DecodePST(new long[] {0, 3617008641903833650, 723412809732590090, 361706447983740165, 86234890240, 431208669220633349, 363114732645386757, 0});
    static sbyte[] blackPawnPST = whitePawnPST.Reverse().ToArray();
    static sbyte[] whiteKnightPST = PSTManager.DecodePST(new long[] {-3541831642829891378, -2815875667013341992, -2161716761344737054, -2160303867343993374, -2161711242227547934, -2160309386461182494, -2815875645454619432, -3541831642829891378});
    static sbyte[] blackKnightPST = whiteKnightPST.Reverse().ToArray();
    static sbyte[] whiteBishopPST = PSTManager.DecodePST(new long[] {-1371637495921969428, -720575940379279114, -720570399703367434, -719163024819812874, -720564902144900874, -717750152377791754, -719168565495724554, -1371637495921969428});
    static sbyte[] blackBishopPST = whiteBishopPST.Reverse().ToArray();
    static sbyte[] whiteRookPST = PSTManager.DecodePST(new long[] {0, 363113758191127045, -360287970189639429, -360287970189639429, -360287970189639429, -360287970189639429, -360287970189639429, 21558722560});
    static sbyte[] blackRookPST = whiteRookPST.Reverse().ToArray();
    static sbyte[] whiteQueenPST = PSTManager.DecodePST(new long[] {-1371637474363246868, -720575940379279114, -720570421262089994, -360282451072450309, -360282451072450560, -720570421262088714, -720575940378951434, -1371637474363246868});
    static sbyte[] blackQueenPST = whiteQueenPST.Reverse().ToArray();
    static sbyte[] whiteKingPST = PSTManager.DecodePST(new long[] {-2100690843423155998, -2100690843423155998, -2100690843423155998, -2100690843423155998, -1377289115042389268, -653887343544177418, 1446781380292776980, 1449607125176819220});
    static sbyte[] blackKingPST = whiteKingPST.Reverse().ToArray();

    Dictionary<PieceType, sbyte[]> whitePST = new Dictionary<PieceType, sbyte[]> {
        {PieceType.Pawn, whitePawnPST},
        {PieceType.Knight, whiteKnightPST},
        {PieceType.Bishop, whiteBishopPST},
        {PieceType.Rook, whiteRookPST},
        {PieceType.Queen, whiteQueenPST},
        {PieceType.King, whiteKingPST}
    };

    Dictionary<PieceType, sbyte[]> blackPST = new Dictionary<PieceType, sbyte[]> {
        {PieceType.Pawn, blackPawnPST},
        {PieceType.Knight, blackKnightPST},
        {PieceType.Bishop, blackBishopPST},
        {PieceType.Rook, blackRookPST},
        {PieceType.Queen, blackQueenPST},
        {PieceType.King, blackKingPST}
    };

    public Move Think(Board board, Timer timer) {
        return minmax(board, 4, int.MinValue, int.MaxValue, board.IsWhiteToMove).Item2;
    }

    private (int, Move) minmax(Board board, int depth, int alpha, int beta, bool maximize) {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate()) {
            return (eval(board), Move.NullMove);
        }
        Span<Move> moves = stackalloc Move[218];
        board.GetLegalMovesNonAlloc(ref moves);
        if (maximize) {
            int value = int.MinValue;
            Move bestMove = Move.NullMove;
            foreach (Move move in moves) {
                board.MakeMove(move);
                (int childValue, _) = minmax(board, depth - 1, alpha, beta, false);
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
            foreach (Move move in moves) {
                board.MakeMove(move);
                (int childValue, _) = minmax(board, depth - 1, alpha, beta, true);
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

    private int eval(Board board) {
        // Terminal states
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
        // Scoring
        int score = 0;
        foreach ((PieceType type, int value) in pieceValues) {
            PieceList whitePieces = board.GetPieceList(type, true);
            PieceList blackPieces = board.GetPieceList(type, false);

            // Material count
            score += (whitePieces.Count - blackPieces.Count) * value;
            if (type.Equals(PieceType.Rook) || type.Equals(PieceType.King)) {
                continue;
            }
            // Development / positioning
            foreach (Piece whitePiece in whitePieces) {
                score += whitePST[type][(whitePiece.Square.Rank * 8) + whitePiece.Square.File];
            }
            foreach (Piece blackPiece in blackPieces) {
                score -= blackPST[type][(blackPiece.Square.Rank * 8) + blackPiece.Square.File];
            }
        }
        return score;
    }
}
