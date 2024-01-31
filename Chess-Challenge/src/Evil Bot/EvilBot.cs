using ChessChallenge.API;
using System;

namespace ChessChallenge.Example
{

    public class EvilBot : IChessBot
    {
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        const int minusInfinity = -1000000;

        Board board;
        int myColor;
        bool firstTime = true;
        Move bestMove;
        const int maxDepth = 5;

        int TotalMaterial()
        {
            PieceList[] allPieces = board.GetAllPieceLists();
            int totalMaterial = 0;
            foreach (PieceList pieceList in allPieces)
            {
                PieceType pieceType = pieceList.TypeOfPieceInList;
                int sign = pieceList.IsWhitePieceList ? 1 : -1;
                totalMaterial += pieceList.Count * pieceValues[(int)pieceType] * sign;
            }
            return totalMaterial;
        }

        private int moveScore(Move a)
        {
            return Convert.ToInt32(a.IsCapture);
        }

        private int CompareTo(Move a, Move b)
        {
            return moveScore(b) - moveScore(a);
        }

        private Move[] OrderMoves()
        {
            Move[] moves = board.GetLegalMoves();
            Array.Sort(moves, (a, b) => CompareTo(a, b));
            return moves;
        }

        private Move[] OrderCaptures()
        {
            Move[] moves = board.GetLegalMoves(true);
            Array.Sort(moves, (a, b) => (pieceValues[(int)b.CapturePieceType] - pieceValues[(int)b.MovePieceType]).CompareTo(pieceValues[(int)a.CapturePieceType] - pieceValues[(int)a.MovePieceType]));
            return moves;
        }

        private int Evaluate(int turn)
        {
            return TotalMaterial() * turn;
        }

        private int Quiesce(int turn, int alpha, int beta)
        {
            int staticEval = Evaluate(turn);
            if (staticEval >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, staticEval);
            Move[] captures = OrderCaptures();
            foreach (Move capture in captures)
            {
                board.MakeMove(capture);
                int score = -Quiesce(turn * -1, -beta, -alpha);
                board.UndoMove(capture);
                if (score >= beta)
                {
                    return beta;
                }
                alpha = Math.Max(alpha, score);
            }
            return alpha;
        }

        private int Search(int depth, int turn, bool start = true, int alpha = -100000000, int beta = 100000000)
        {
            if (board.IsDraw())
            {
                return 0;
            }
            Move[] moves = OrderMoves();
            if (moves.Length == 0)
            {
                if (board.IsInCheckmate())
                {
                    return minusInfinity - depth; // If there are many mates, go for the fastest.
                }
                else
                {
                    return 0;
                }
            }
            if (depth == 0)
            {
                return Quiesce(turn, alpha, beta);
            }
            int score = alpha - 1;
            foreach (Move move in moves)
            {

                board.MakeMove(move);
                score = Math.Max(score, -Search(depth - 1, turn * -1, false, -beta, -alpha));
                board.UndoMove(move);
                //alpha = Math.Max(alpha, score);
                if (score >= beta)
                {
                    return beta;
                }
                if (score > alpha)
                {
                    alpha = score;
                    if (start)
                    {
                        bestMove = move;
                    }
                }
            }
            return alpha;
        }

        public Move Think(Board board, Timer timer)
        {
            if (firstTime)
            {
                // Initialize stuff for the first time
                firstTime = false;
                myColor = board.IsWhiteToMove ? 1 : -1;
            }

            this.board = board;
            Search(maxDepth, myColor);
            return bestMove;
        }

        // Test if this move gives checkmate
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }
    }
}