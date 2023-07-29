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

enum Flag {
    EXACT,
    LOWERBOUND,
    UPPERBOUND
}

struct TTEntry {
    public int depth;
    public Flag flag;
    public double value;
    public Move move;
    public TTEntry(int depth, Flag flag, Move move, double value) {
        this.depth = depth;
        this.flag = flag;
        this.move = move;
        this.value = value;
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

    Dictionary<ulong, TTEntry> TT = new();

    static double INF = 2147483646;
    public MyBot() {
        foreach ((PieceType type, sbyte[] a) in PST) {
            Console.WriteLine(type);
            Console.WriteLine(a.Max());
            Console.WriteLine(a.Min());
        }
    }
    public Move Think(Board board, Timer timer) {
        int sign = board.IsWhiteToMove ? 1 : -1;
        (double value, Move move) = negamax(board, 4, 4, -INF, INF, sign);
        Console.WriteLine($"TT: {TT.Count}");
        Console.WriteLine($"eval: {value}");
        return move;
    }

    private (double, Move) negamax(Board board, int depth, int origDepth, double alpha, double beta, int sign) {
        double alphaOrig = alpha;
        Move bestMove = Move.NullMove;
        TTEntry lookup = new();
        bool lookupSuccess = TT.TryGetValue(board.ZobristKey, out lookup);
        lookupSuccess = false;

        if (lookupSuccess) {
            if (lookup.depth >= depth) {
                if (lookup.flag == Flag.EXACT) {
                    if (depth == origDepth) {
                        bestMove = lookup.move;
                    }
                    return (lookup.value, bestMove);
                } else if (lookup.flag == Flag.LOWERBOUND) {
                    alpha = Math.Max(alpha, lookup.value);
                } else if (lookup.flag == Flag.UPPERBOUND) {
                    beta = Math.Min(beta, lookup.value);
                }
                if (alpha >= beta) {
                    if (depth == origDepth) {
                        bestMove = lookup.move;
                    }
                    return (lookup.value, bestMove);
                }
            }
        }

        if (depth == 0 || board.IsDraw() || board.IsInCheckmate()) {
            // TODO: Add in penalty for slow wins...
            if (depth != 0) {
                Console.WriteLine("AAA");
            }
            double score = sign * eval(board);
            return (score * (1 + 0.001 * depth),  Move.NullMove);
        }

        List<Move> possibleMoves = board.GetLegalMoves().ToList();
        if (lookupSuccess) {
            possibleMoves.Prepend(lookup.move);
        }

        double bestValue = -INF;
        foreach (Move move in possibleMoves) {
            board.MakeMove(move);
            (double moveAlpha, _) = negamax(board, depth - 1, origDepth, -beta, -alpha, sign);
            moveAlpha = -moveAlpha;
            board.UndoMove(move);

            if (bestValue < moveAlpha) {
                bestValue = moveAlpha;
                bestMove = move;
            }

            if (alpha < moveAlpha) {
                alpha = moveAlpha;
                if (depth == origDepth) {
                    bestMove = move;
                }
                if (alpha >= beta) {
                    break;
                }
            }
        }

        Flag flag;
        if (bestValue <= alphaOrig) {
            flag = Flag.UPPERBOUND;
        } else if (bestValue >= beta) {
            flag = Flag.LOWERBOUND;
        } else {
            flag = Flag.EXACT;
        }
        TT[board.ZobristKey] = new TTEntry(depth, flag, bestMove, bestValue);

        return (bestValue, bestMove);
    }

    private double eval(Board board) {
        if (board.IsDraw()) {
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
