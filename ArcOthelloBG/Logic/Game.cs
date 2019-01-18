﻿using ArcOthelloBG.EventHandling;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcOthelloBG.Logic
{
    /// <summary>
    /// Class that implements the rules of the game
    /// </summary>
    class Game
    {
        // MEMBERS
        private int[,] board;
        private static Game instance = null;
        private int playerToPlay;
        private int whiteId;
        private int blackId;
        private List<Vector2> possibleMoves;
        private int turn;
        private int emptyId;
        private BoardState boardState;
        private int blackScore;
        private int whiteScore;

        public event EventHandler<SkipTurnEventArgs> TurnSkipped;
        public event EventHandler<WinEventArgs> Won;

        // GETTERS AND SETTERS

        /// <summary>
        /// Getters for the singleton instance
        /// </summary>
        public static Game Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Game();
                }

                return instance;
            }
        }

        /// <summary>
        /// singleton constructor, so private
        /// </summary>
        private Game()
        {
            this.boardState = null;
        }

        /// <summary>
        /// property for the white score, only getter, and it's a computed value
        /// </summary>
        public int WhiteScore
        {
            get
            {
                return this.whiteScore;
            }
        }

        /// <summary>
        /// property for the black score, only getter, and it's a computed value
        /// </summary>
        public int BlackScore
        {
            get
            {
                return this.blackScore;
            }
        }

        public int PlayToPlay
        {
            get
            {
                return this.playerToPlay;
            }
        }

        /// <summary>
        /// getter for the board
        /// </summary>
        public int[,] Board
        {
            get
            {
                if (instance.board == null)
                {
                    throw new InvalidOperationException("board not init");
                }

                return board;
            }
        }

        public BoardState BoardState
        {
            get 
            {
                return this.boardState; 
            }

        }

        /// <summary>
        /// Init the grid
        /// </summary>
        /// <param name="width">width of the grid</param>
        /// <param name="height">height of the grid</param>
        public void init(int columns, int rows, int whiteId, int blackId, int emptyId)
        {
            this.board = new int[columns, rows];
            this.playerToPlay = whiteId;
            this.whiteId = whiteId;
            this.blackId = blackId;
            this.emptyId = emptyId;
            this.buildPossibleDirections();
            this.blackScore = 0;
            this.whiteScore = 0;

            this.initBoard();

            this.turn = -1;
            this.nextTurn();
        }


        public void loadState(BoardState state)
        {
            this.board = (int[,])state.Board.Clone();
            this.emptyId = state.EmptyId;
            this.whiteScore = state.WhiteScore;
            this.blackScore = state.BlackScore;
            this.playerToPlay = state.PlayerId;
            this.boardState = state;

        }

        /// <summary>
        /// Play a move
        /// </summary>
        /// <param name="position">Position to put a pawn</param>
        /// <param name="isWhite">color of the pawn</param>
        /// <returns>positions that changed</returns>
        public List<Vector2> play(Vector2 position, int idToPlay)
        {
            if (this.isPlayable(position, idToPlay))
            {
                var initialPosition = new Vector2(position);
                var changedPositions = new List<Vector2>();

                var directions = this.boardState.getValidDirections(position);

                foreach (var direction in directions)
                {
                    position = initialPosition;
                    do
                    {
                        this.putPawn(position, idToPlay);
                        changedPositions.Add(position);
                        position = position.add(direction);
                    } while (this.boardState.isInBoundaries(position) && this.getColor(position) != idToPlay);
                }

                this.nextTurn();

                int lostPoint = changedPositions.Count - 1;

                if (idToPlay == this.whiteId)
                {
                    this.blackScore -= lostPoint;
                }
                else
                {
                    this.whiteScore -= lostPoint;
                }

                return changedPositions;
            }
            else
            {
                throw new ArgumentException("This move isn't possible");
            }

        }

        /// <summary>
        /// Check if a move is possible
        /// </summary>
        /// <param name="position">Position to put a pawn</param>
        /// <param name="isWhite">Color of the pawn</param>
        /// <returns>move is playable or not</returns>
        public bool isPlayable(Vector2 position, int idToPlay)
        {
            return this.playerToPlay == idToPlay && this.boardState.isPlayable(position);
        }

        public List<Vector2> getPositionsAvailable()
        {
            return this.boardState.AvailablePositions;
        }

        /// <summary>
        /// get the color of a position (used to shortened the code)
        /// </summary>
        /// <param name="position">position of the pawns</param>
        /// <returns>Color of the pawns</returns>
        public int getColor(Vector2 position)
        {
            return this.board[position.X, position.Y];
        }


        private void nextTurn(bool hasSkipped = false)
        {
            this.playerToPlay = this.getNextPlayer();

            this.turn++;
            this.boardState = new BoardState(this.board, this.playerToPlay, this.possibleMoves, this.emptyId, this.whiteScore, this.blackScore);

            if(this.getPositionsAvailable().Count == 0)
            {
                if (hasSkipped)
                {
                    Won?.Invoke(this, new WinEventArgs(this.getWinner()));
                    return; 
                }

                int previousPlayer = this.playerToPlay;
                this.nextTurn(true);
                TurnSkipped?.Invoke(this, new SkipTurnEventArgs(previousPlayer));
            }
        }

        private int getNextPlayer()
        {
            return this.playerToPlay == this.whiteId ? this.blackId : this.whiteId;
        }

        

        private int getWinner()
        {
            return this.whiteScore > this.blackScore ? this.whiteId : this.blackId;
        }


        private void putPawn(Vector2 position, int idColor)
        {
            this.board[position.X, position.Y] = idColor;
            this.incrementScore(idColor);
        }



        /// <summary>
        /// init the board with the right numbers
        /// </summary>
        private void initBoard()
        {
            int w = this.board.GetLength(0);
            int h = this.board.GetLength(1);

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    int color = this.emptyId;

                    if (i == w / 2 && j == h / 2 || i == w / 2 - 1 && j == h / 2 - 1)
                    {
                        color = this.whiteId;
                    }
                    else if (i == w / 2 - 1 && j == h / 2 || i == w / 2  && j == h / 2 - 1)
                    {
                        color = this.blackId;
                    }

                    this.putPawn(new Vector2(i, j), color);
                }
            }
        }

        /// <summary>
        /// build the list of Direction possible to play
        /// </summary>
        private void buildPossibleDirections()
        {
            this.possibleMoves = new List<Vector2>();

            //list is always the same, see if we can make it elsewhere
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i != 0 || j != 0)
                    {
                        this.possibleMoves.Add(new Vector2(i, j));
                    }
                }
            }
        }

        private void incrementScore(int playerId)
        {
            if (playerId == this.whiteId)
            {
                this.whiteScore++;
            }
            else if(playerId == this.blackId)
            {
                this.blackScore++;
            }
        }

    }
}