using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestStack.White;
using TestStack.White.UIItems;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.WindowItems;
using System.Windows.Automation;

namespace GroundPenetratingRadar
{
    class Program
    {

        //basically just to get the ball rolling at the moment
        static void Main(string[] args)
        {
            Process[] processes = Process.GetProcessesByName(@"Minesweeper");

            if (processes.Length == 1)
            {
                var application = Application.Attach(processes[0]);
                LetsAGo(application);
            }
            else
            {
                Console.WriteLine("Looks like she wasn't running, bud, sorry");
            }
            System.Threading.Thread.Sleep(5000);
        }

        //running some initiation functions then pointing to the main loop
        static void LetsAGo(Application application)
        {
            //Console.Write(application.GetWindows());
            Window window = application.GetWindow("Minesweeper");
            window.SetForeground();
            //let's check the difficulty (to write later)

            //define our quick access data representation of board and the reference to code version
            Boards boards = new Boards();


            //let's go through and populate our groupbox info with data references to save time, also 10ify the nodes array
            for (int i = 1; i < 17; i++)
            {
                SearchCriteria searchCriteria = SearchCriteria.ByText("Row " + i.ToString());
                boards.rows[i - 1] = (GroupBox)window.Get<GroupBox>(searchCriteria);
                for (int j = 0; j < 30; j++)
                {
                    boards.nodes[i - 1, j] = 10;
                }
            }

            Console.WriteLine("");

            //get that first point done. 8,15 is the middle, i.e. the spot the least likely to get corner stuck
            LeftClick(boards, 8, 15);
            UpdateGridByPoint(boards, 8, 15);

            // main loop, baby
            Boolean complete = false;
            int[,,] nextMoves = new int[10, 1, 1];
            while (complete == false)
            {

                //first do a check for obvious moves, and if any are found, save them as basicResults
                printBoard(boards);
                var basicResults = CheckBasics(boards);
                //if there are basic moves to make handle them, update board from them, move on
                if (basicResults.Item1.Count != 0 || basicResults.Item2.Count != 0)
                {
                    printBoard(boards);
                    while (basicResults.Item1.Count != 0)
                    {
                        int[] pos = basicResults.Item1.Pop();
                        DoubleLeftClick(boards, pos[0], pos[1]);
                    }
                    while (basicResults.Item2.Count != 0)
                    {
                        int[] pos = basicResults.Item2.Pop();
                        RightClick(boards, pos[0], pos[1]);
                    }
                } else { //if advanced

                    // check heuristics
                    // call basic inverted edge reading
                    Console.WriteLine();//go to advanced
                } 

                complete = true;
            }
        }

        //method to update grid, only updating values which are still unknown, to save processing power
        static void UpdateGrid(Boards boards)
        {
            for (int i = 1; i < 16; i++)
            {
                Console.WriteLine("Row: " + i.ToString());
                for (int j = 0; j < 30; j++)
                {
                    if (boards.nodes[i, j] == 10)
                    {
                        boards.nodes[i, j] = TileID(boards, i, j);
                    }
                }
            }
        }

        //expands a square grid outwards from a point to identify changes, comparing # of unkowns
        //this method should be replaced with a more efficient edge-huggin-expansion algorithm
        static Boolean UpdateGridByPoint(Boards boards, int r, int c)
        {
            // CURRENTLY: implicit that this is called ONLY after cicked a point, so check the given point
            int firstTileID = TileID(boards, r, c);
            if (boards.nodes[r, c] != firstTileID) {
                boards.nodes[r, c] = firstTileID;
            }

            //now all code following this is for the potentially changed surrounding points
            Boolean complete = false; //exit conditions
            int lc = c,
                rc = c,
                tr = r,
                br = r,
                tmpTileID = 0; //left column, right column, etc.
            int counter = 0;
            
            //loop through the continually expanding box checking for changes, exit when no further changes found
            while (!complete)
            {
                complete = true; // hypothesis that this is the first unchanged box, be proved wrong later
                // if the row/column is not at the limit, increment it outwards
                if (lc >= 0) { lc -= 1;}
                if (rc <= 29) { rc += 1;}
                if (tr >= 0) { tr -= 1;}
                if (br <= 15) { br += 1;}
                //first scan the edge columns
                //left column minus top and bottom row
                if (lc != -1) {
                    for (int k = tr + 1; k <= br - 1; k++) {
                        if (boards.nodes[k, lc] == 10) {
                            tmpTileID = TileID(boards, k, lc);
                            if (boards.nodes[k, lc] != tmpTileID) {
                                boards.nodes[k, lc] = tmpTileID;
                                complete = false;
                            }
                        }
                    }
                }
                //right column minus top and bottom row
                if (rc != 31) {
                    for (int k = tr + 1; k <= br - 1; k++) {
                        if (boards.nodes[k, rc] == 10) {
                            tmpTileID = TileID(boards, k, rc);
                            if (boards.nodes[k, rc] != tmpTileID) {
                                boards.nodes[k, rc] = tmpTileID;
                                complete = false;
                            }
                        }
                    }
                }
                //because the corners are going to be handled by the rows, we reset the lc and rc to 0 if -1
                //next round around they'd be set to -1 again before the columns are check so no wasted processing
                //top row full
                if (tr != -1) {
                    for (int k = lc; k <= rc; k++) {
                        if (boards.nodes[tr, k] == 10) {
                            tmpTileID = TileID(boards, tr, k);
                            if (boards.nodes[tr, k] != tmpTileID) {
                                boards.nodes[tr, k] = tmpTileID;
                                complete = false;
                            }
                        }
                    }
                }
                //bottom row full
                if (tr != -1) {
                    for (int k = lc; k <= rc; k++) {
                        if (boards.nodes[tr, k] == 10) {
                            tmpTileID = TileID(boards, tr, k);
                            if (boards.nodes[tr, k] != tmpTileID) {
                                boards.nodes[tr, k] = tmpTileID;
                                complete = false;
                            }
                        }
                    }
                }

                counter++;
                Console.WriteLine("Finished scanning box " + counter.ToString());
            }
            return true;
        }

        //******** try to loop this until no more moves are seen ************
        // look for basic moves using the # touching and tile count method
        static (Stack<int[]>, Stack<int[]>) CheckBasics(Boards boards)
        {
            Stack<int[]> doubleClicks = new Stack<int[]>();
            Stack<int[]> rightClicks = new Stack<int[]>();
            Boolean exhausted = false;

            //move through the entire memory copy of the node locations checking for mines using the game's basic logic
            //as some moves may influence others using this thinking, restart the search after every change
            while (!exhausted)
            {
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 30; j++)
                    {
                        // 0 being satisfied or empty, 9 being mine, and 10 being unknown means we don't need to do this
                        if (boards.nodes[i, j] < 9 && boards.nodes[i, j] > 0)
                        {
                            //get the results of determining if a play is possible
                            var results = CheckTouching(boards, i, j);

                            if (results.Item1 || results.Item2.Count != 0)
                            {
                                //are there any 
                                if (results.Item1)
                                {
                                    int[] tmpDubs = new int[2];
                                    tmpDubs[0] = i; tmpDubs[1] = j;
                                    doubleClicks.Push(tmpDubs);
                                }
                                while (results.Item2.Count != 0)
                                {
                                    rightClicks.Push(results.Item2.Pop());
                                }
                                //break off this search since there have obviously been changes, start search again
                                continue;
                            }


                        }
                    }
                }
                //must've finished the entire board now which means no more moves left, finish the function
                exhausted = true;
            }

            return (doubleClicks, rightClicks);
        }

        /*
        static (Stack<int[]>, Stack<int[]>) CheckInversion(Boards boards)
        {

            return 
        }
        */

        // given a passed position in nodes, determine neighbouring mine and empty count and do something about it
        static (Boolean, Stack<int[]>) CheckTouching(Boards boards, int r, int c)
        {
            if (r == 9 && c == 14)
            {
                Console.WriteLine("WE HIT THE PROBLEMED CASE HERE");
            }
            int num = boards.nodes[r, c];
            int mines = 0, unknowns = 0;
            int[,] tiles = new int[8, 3];
            Boolean doubleClick = false;
            Stack<int[]> rightClicks = new Stack<int[]>();

            tiles[0, 0] = r - 1; tiles[0, 1] = c - 1; // top left
            tiles[1, 0] = r - 1; tiles[1, 1] = c + 1; // top right
            tiles[2, 0] = r + 1; tiles[2, 1] = c + 1; // bottom right
            tiles[3, 0] = r + 1; tiles[3, 1] = c - 1; // bottom left
            tiles[4, 0] = r - 1; tiles[4, 1] = c;     // top
            tiles[5, 0] = r; tiles[5, 1] = c + 1;     // right
            tiles[6, 0] = r + 1; tiles[6, 1] = c;     // bottom
            tiles[7, 0] = r; tiles[7, 1] = c - 1;     // left

            //POSSIBLY WRONG, 0 and 15 and 29 inclusive!!
            // if in bounds - 1 if out of bounds - 2
            if (r > 0 && c > 0) { tiles[0, 2] = 1; } else { tiles[0, 2] = 2; }  // top left
            if (r > 0 && c < 29) { tiles[1, 2] = 1; } else { tiles[1, 2] = 2; } // top right
            if (r < 15 && c < 29) { tiles[2, 2] = 1; } else { tiles[2, 2] = 2; }// bottom right
            if (r < 15 && c > 0) { tiles[3, 2] = 1; } else { tiles[3, 2] = 2; } // bottom left
            if (r > 0) { tiles[4, 2] = 1; } else { tiles[4, 2] = 2; }           //top
            if (c < 29) { tiles[5, 2] = 1; } else { tiles[5, 2] = 2; }          // right
            if (r < 15) { tiles[6, 2] = 1; } else { tiles[6, 2] = 2; }          // bottom
            if (c > 0) { tiles[7, 2] = 1; } else { tiles[7, 2] = 2; }           // left

            // count unknown and mines
            for (int i = 0; i < 8; i++)
            {
                if (tiles[i, 2] == 1)
                {
                    switch (boards.nodes[tiles[i, 0], tiles[i, 1]])
                    {
                        case 9: mines++; break;
                        case 10: unknowns++; break;
                    }
                }
            }

            // logic to respond to counts of mines and unknowns
            if (num == mines && unknowns == 0) // touchs enough, no clicking just prevent future searches
            {
                boards.nodes[r, c] = 0;
            }
            else if (num == unknowns || num == (mines + unknowns)) // set all sides to done
            {
                boards.nodes[r, c] = 0;
                for (int i = 0; i < 8; i++)
                {
                    int[] tmp = new int[2];
                    tmp[0] = tiles[i, 0]; tmp[1] = tiles[i, 1];
                    if (tiles[i, 2] == 1 && boards.nodes[tiles[i, 0], tiles[i, 1]] == 10)
                    {
                        boards.nodes[tiles[i, 0], tiles[i, 1]] = 9;
                        rightClicks.Push(tmp);
                    }
                }
            }
            else if (num == mines)
            {
                doubleClick = true;
                boards.nodes[r, c] = 0;
            }

            Console.WriteLine("Checking tile r=" + r.ToString() + " c=" + c.ToString());

            //tuple with info
            return (doubleClick, rightClicks);
        }

        //grab the tile name 
        static int TileID(Boards boards, int i, int j)
        {
            String name = boards.rows[i].Items[j].Name;
            if (name.Contains("No M")) { return 0; } // empty space
            else if (name.Contains("1 M")) { return 1; } // touching 1
            else if (name.Contains("2 M")) { return 2; }
            else if (name.Contains("3 M")) { return 3; }
            else if (name.Contains("4 M")) { return 4; }
            else if (name.Contains("5 M")) { return 5; }
            else if (name.Contains("6 M")) { return 6; }
            else if (name.Contains("7 M")) { return 7; }
            else if (name.Contains("8 M")) { return 8; } //surrounded by mines, incredibly unlikely to surface
            else if (name.Contains("(Con")) { return 10; } //surrounded by mines, incredibly unlikely to surface
            else { Console.WriteLine(name); return 9; } //mine (this should never ever happen)
        }

        //when optimising, fill this out!!
        //method to update grid, only updating values which are still unknown, to save processing power
        //just use all sides of the empty tiles as a basis, ezpz
        static void UpdateGridFromPoint(Boards boards, int r, int c)
        {
            
            //implement an A* approach here to pathfind which nodes to update/
            //because there is a significant processing time cost associated with each read of a tile

            //dont check previously checked in cycle
            //dont check anything marked as a mine or as a tile

            //perhaps take advantage of #s and geometry to simplify this further
            //on second though, 100% do this

            //take advantage of the middle being always empty, never disjointed

        }

        static void LeftClick(Boards boards, int r, int c) { boards.rows[r].Items[c].Click(); }
        static void DoubleLeftClick(Boards boards, int r, int c) { boards.rows[r].Items[c].DoubleClick(); }
        static void RightClick(Boards boards, int r, int c) { boards.rows[r].Items[c].RightClick(); }

        //prints a copy of the board as understood by the nodes array in the console for debugging purposes
        static void printBoard(Boards boards)
        {
            Console.WriteLine("Beginning board debug print");
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 30; j++)
                {
                    //uniformity of spacing for all values < 10
                    if (boards.nodes[i, j] == 10)
                    {
                        Console.Write(boards.nodes[i, j].ToString() + " ");
                    } else
                    {
                        Console.Write(boards.nodes[i, j].ToString() + "  ");
                    }
                }
                Console.WriteLine();
            }
        }

        static void randomClick(Boards boards)
        {
            Boolean done = false;
            while (!done)
            {
                Random r1 = new Random();
                Random r2 = new Random();
                int rInt1 = r1.Next(0, 16);
                int rInt2 = r2.Next(0, 16);
                if (boards.nodes[rInt1,rInt2] == 10)
                {

                }
            }
        }

        //do some fancy shit on the matrix and try to find where some moves are

    }

    public class Boards
    {
        public int[,] nodes = new int[16, 30];
        public GroupBox[] rows = new GroupBox[16];
    }
}


// 0 is empty or already solved tile
// 1-8 are straightforward
// 9 is a mine
// 10 is an unclicked tile