﻿using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System;
using System.Drawing.Imaging;
using System.Windows.Forms;
using SimWinInput;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace GroundPenetratingRadar
{

    class Program
    {

        //SetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(int hWnd);

        // Define the SetWindowPos API function.
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd,
            IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        //get PID from thread
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        //This is a replacement for Cursor.Position in WinForms
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        //Mouse Event System Function Connection
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        public const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        public const int MOUSEEVENTF_MOVE = 0x0001;
        public const int MOUSEEVENTF_LEFTDOWN = 0x002;
        public const int MOUSEEVENTF_LEFTUP = 0x0004;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const int MOUSEEVENTF_RIGHTUP = 0x0010;

        //grab screenshot of entire screen
        static public void Screenie(Process proc)
        {
            var bmp = new Bitmap(540, 288, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.CopyFromScreen(100, 100, 0, 0, new Size(540, 288), CopyPixelOperation.SourceCopy);

            bmp.Save("c:\\Users\\Lou\\Desktop\\test.png", ImageFormat.Png);
        }

        //moves the mouse off screen after every move to avoid a particular bug with flags at r=0, c=1
        public static void moveOffBoard()
        {
            // add code to make this independant of screen size: Rectangle screenSize = Screen.PrimaryScreen.Bounds;
            int ypos = 15352;
            int xpos = 12937;

            mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
        }

        //send left click
        public static void LeftClick(int y, int x, StringBuilder log)
        {
            // add code to make this independant of screen size: Rectangle screenSize = Screen.PrimaryScreen.Bounds;
            int ypos = (int)(65536.0 / 1080 * (109 + y * 18));
            int xpos = (int)(65536.0 / 1920 * (109 + x * 18));

            mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);

            String tmp = "Left clicked (GUESSED) at x: " + xpos.ToString() + ", y: " + ypos.ToString();
            Debug.WriteLine(tmp);
            log.AppendLine(tmp);
        }

        //send left click
        public static void DoubleLeftClick(int r, int c, StringBuilder log)
        {
            int ypos = (int)(65536.0 / 1080 * (109 + r * 18));
            int xpos = (int)(65536.0 / 1920 * (109 + c * 18));

            mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            System.Threading.Thread.Sleep(10);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);

            String tmp = "Double left clicked at (r: " + r.ToString() + ", c: " + c.ToString() + "" + ") x: " + xpos.ToString() + ", y: " + ypos.ToString();
            Debug.WriteLine(tmp);
            log.AppendLine(tmp);
        }

        //send right click
        public static void RightClick(int y, int x, StringBuilder log)
        {
            // add code to make this independant of screen size: Rectangle screenSize = Screen.PrimaryScreen.Bounds;
            int ypos = (int)(65536.0 / 1080 * (109 + y * 18));
            int xpos = (int)(65536.0 / 1920 * (109 + x * 18));

            mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_RIGHTUP | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
        }

        //pop up an alert box for the user with provided message
        static void errorMessage(String message)
        {
            string caption = "Error Detected in Input";
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            MessageBox.Show(message, caption, buttons);
        }

        //let's see if we find mindsweeper running
        static void Main(string[] args)
        {
            Process[] processes = Process.GetProcessesByName(@"Minesweeper");

            if (processes.Length == 1)
            {
                LetsAGo(processes[0].Id);
            }
            else
            {
                Console.WriteLine("Listen, I WANT to help you, but you need to help yourself first. Try OPENING THE GAME before, idiot...");
            }
            System.Threading.Thread.Sleep(10000);
        }
      
        //running some initiation functions then pointing to the main loop
        static void LetsAGo(int procID)
        {

            Process proc = Process.GetProcessById(procID);
            Int32 proci32 = proc.MainWindowHandle.ToInt32();

            // window dimensions
            int tlx = 100;
            int tly = 100;
            int width = 480;
            int height = 280;
            //move and resize window because getting existing window dimensions and location didn't work out
            SetWindowPos(proc.MainWindowHandle, (IntPtr)0, tlx - 52, tly - 95, width, height, 0);

            SetForegroundWindow(proc.MainWindowHandle.ToInt32()); // make it the active window

            //define our quick access data representation of board and the reference to code version
            Boards boards = new Boards();
            boards.InitializeNodes(); //set all squares to unknown tile

            int threadCount = 8;
            Images[] images = new Images[threadCount];
            int[] block = new int[threadCount];
            for (int p=0; p < threadCount; p++) { images[p] = new Images(); block[p]=0; }
        
            System.Threading.Thread.Sleep(50); //why is this here? **re-evaluate later**

            Boolean complete = false;

            //get the log going
            StringBuilder log = new StringBuilder();

            //Make the first move. 8,15 is the middle, i.e. the spot the least likely to get corner stuck
            LeftClick(8, 15, log);
            //UpdateGridByPoint(boards, 8, 15); -- currently useless given the structural changes accompanying the parallel recognition changes

            // main loop, baby
            while (complete == false)
            {
                moveOffBoard();
                System.Threading.Thread.Sleep(50);

                //update the screencapture used to identify tiles (for each thread)
                for (int p = 0; p < threadCount; p++) { images[p].updateScreenie(); }

                /*
                String tmp2 = "new cycle, pre update grid";
                Console.WriteLine(tmp2);
                log.AppendLine(tmp2);
                printBoard(boards.nodes, log, false);
                */

                //Bring the program's internal board representations up to date
                UpdateGrid(boards, images, block, log); //Analyze screenshots to identify tiles using new parallel processing method
                printBoard(boards.nodes, log, false); //for console debugging
                UpdateReduced(boards); // create the reduced values copy of nodes

                //Start collecting moves to complete using the basic and heuristic methods
                var results = CheckBasics(boards); // double left / right
                var reduced121Results = Check121(boards, log); // double left / right
                var reduced1221Results = Check1221(boards, log); // double left / right
                Stack<int[]> edgePatternResults = CheckEdge(boards, log); // only single left

                //merge varying results (moves to do) stacks into results
                Stack<int[]> doubleClicks = new Stack<int[]>();
                Stack<int[]> rightClicks = new Stack<int[]>();
                Stack<int[]> leftClicks = new Stack<int[]>();

                //merge double clicks
                doubleClicks = mergeStacks(doubleClicks, results.Item1); //merge with empty to filter dupes
                doubleClicks = mergeStacks(doubleClicks, reduced121Results.Item1);
                doubleClicks = mergeStacks(doubleClicks, reduced1221Results.Item1);

                //merge right clicks
                rightClicks = mergeStacks(rightClicks, results.Item2); //merge with empty to filter dupes
                rightClicks = mergeStacks(rightClicks, reduced121Results.Item2);
                rightClicks = mergeStacks(rightClicks, reduced1221Results.Item2);

                //transfer left clicks to better named stack
                leftClicks = edgePatternResults;

                // interpretation of results from the basic check goes here
                if (doubleClicks.Count != 0 || rightClicks.Count != 0 || leftClicks.Count != 0)
                {

                    //printBoard(boards.nodes);
                    while (rightClicks.Count != 0) // right clicks
                    {
                        int[] pos = rightClicks.Pop();
                        System.Threading.Thread.Sleep(27);
                        if (boards.nodes[pos[0], pos[1]] != 9)
                        {
                            RightClick(pos[0], pos[1], log);
                        } else
                        {
                            Console.WriteLine("What in tarnation....?");
                        }
                        String tmp = "Right clicked at r=" + pos[0].ToString() + ", c=" + pos[1].ToString() + " previous val=" + boards.nodes[pos[0], pos[1]].ToString();
                        boards.nodes[pos[0], pos[1]] = 9; // critical
                        Console.WriteLine(tmp);
                        log.AppendLine(tmp);
                    }
                    while (leftClicks.Count != 0) // double left clicks
                    {
                        int[] pos = leftClicks.Pop();
                        System.Threading.Thread.Sleep(27);
                        LeftClick(pos[0], pos[1], log);
                    }
                    while (doubleClicks.Count != 0) // double left clicks
                    {
                        int[] pos = doubleClicks.Pop();
                        System.Threading.Thread.Sleep(27);
                        DoubleLeftClick(pos[0], pos[1], log);
                        isGameOver(images[0], log); // check this every iteration to catch a game lost
                    }

                } else {

                    randomClick(boards, log);
                    System.Threading.Thread.Sleep(50);
                    isGameOver(images[0], log);
                    // consider expansion of advanced method set by adding a further way to determine moves

                } 

            }
        }

        // merges two "moves" stacks while ensuring no duplicates exist in the combined stack
        static Stack<int[]> mergeStacks(Stack<int[]> baseStack, Stack<int[]> addedStack)
        {
            while (addedStack.Count != 0)
            {
                
                int[] pos = new int[2];
                pos = addedStack.Pop();

                if (!baseStack.Any(item => item[0] == pos[0] && item[1] == pos[1]))
                {
                    baseStack.Push(pos);
                } else
                {
                    Console.WriteLine("Caught a duplicate trying to sneak in! at: r=" + pos[0].ToString() + ", c=" + pos[1].ToString());
                }

                /*
                if (!baseStack.Contains(pos))
                {
                    baseStack.Push(pos);
                }
                */
            }

            return baseStack;
        }

        // threaded selective image comparisons of board to have the internal representation reflect new board reality
        static void UpdateGrid(Boards boards, Images[] images, int[] block, StringBuilder log) {

            List<int[]> unknownPoints = new List<int[]>();
            int[] tmpTileIDs = new int[8];
            Boolean badGuessLock = false;

            for (int r = 0; r < 16; r++)
            {
                for (int c = 0; c < 30; c++)
                {
                    if (boards.nodes[r, c] == 10)
                    {
                        int[] tmp = new int[2];
                        tmp[0] = r; tmp[1] = c; // co-ords
                        unknownPoints.Add(tmp);
                    }
                }
            }

            Console.WriteLine("non-flag count: " + unknownPoints.Count.ToString());

            if (!images[0].checkLost()) // a low power (direct image comparison, no tolerance consideration) lost game check
            {
                // add unkown tiles to an available images object (i.e. give a tile to an available worker)
                Parallel.ForEach(unknownPoints, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (pt) =>
                {
                    for (int i = 0; i < block.Length; i++) //block on image class instance
                    {
                        if (block[i] == 0) // is worker available?
                        {
                            block[i] = 1; //lock this thread and images class instance

                            //String blockInformation = "Blocks: ";
                            //for (int k=0;k<block.Length; k++) { blockInformation += block[k].ToString(); }
                            //Console.WriteLine(blockInformation);

                            lock (images[i])
                            {
                                tmpTileIDs[i] = images[i].TileID(pt[0], pt[1]);

                                if (tmpTileIDs[i] != -1) // -1 being the ID program didn't know wtf it saw
                                {
                                    boards.nodes[pt[0], pt[1]] = tmpTileIDs[i];
                                    pt[1] = 1; // no need to wait for a semaphore to come free
                                }
                                else // making the thus-far-in-testing valid assumption that that means game over
                                {
                                    if (!badGuessLock)
                                    {
                                        badGuessLock = true;
                                        badGuess(log);
                                    }
                                }
                            }

                            block[i] = 0; //unlock
                        }
                    }
                });
            } else {
                badGuess(log);
            }
        }

        // save a couple lines of code elsewhere by simplifying this code into this
        static void isGameOver(Images images, StringBuilder log)
        {
            images.updateScreenie();
            if (images.checkLost())
            {
                badGuess(log);
            }
        }

        // bad guess made, quit and tell the user
        static void badGuess (StringBuilder log)
        {
            File.WriteAllText("C:\\Users\\Lou\\Desktop\\log.txt", log.ToString());
            MessageBox.Show("The bot was forced to make a random guess which was, unfortunately, a mine.", "Failed",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(0);
        }

        /*expands a square grid outwards from a point to identify changes, comparing # of unkowns
        this method should be replaced with a more efficient edge-huggin-expansion algorithm
             SHOULD I ERASE? WOULD NEED MAJOR REWRITE TO BE COMPATIBLE WITH THE PARALLEL PROCESSING CHANGES
        static Boolean UpdateGridByPoint(Images images, Boards boards, int r, int c)
        {
            // CURRENTLY: implicit that this is called ONLY after having clicked a point, so check the given point
            int firstTileID = images.TileID(r, c);
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
                            tmpTileID = images.TileID(k, lc);
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
                            tmpTileID = images.TileID(k, rc);
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
                            tmpTileID = images.TileID(tr, k);
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
                            tmpTileID = images.TileID(tr, k);
                            if (boards.nodes[tr, k] != tmpTileID) {
                                boards.nodes[tr, k] = tmpTileID;
                                complete = false;
                            }
                        }
                    }
                }

                counter++;
                Debug.WriteLine("Finished scanning box " + counter.ToString());
            }
            return true;
        }*/

        // look for basic moves using the # touching and tile counts
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
                            var results = basicMoves(boards, i, j);
                            //var results = CheckTouching(boards, i, j);

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

        // when the simple solves are exhausted, check if solves are possible using more advanced techniques
        // return a int value corresponding to the tile of at a grid coordinate
        // 0 is empty or already solved tile
        // 1-8 are straightforward
        // 9 is a mine
        // 10 is an unclicked tile
        static (Stack<int[]>, Stack<int[]>) Check121(Boards boards, StringBuilder log)
        {
            Stack<int[]> doubleClicks = new Stack<int[]>();
            Stack<int[]> rightClicks = new Stack<int[]>();
            //Boolean moveFound = false;

            //printBoard(boards.nodesReduced, log, true);
            //printBoard(boards.nodesReduced);

            // horizontal 1-2-1 adaptive
            for (int r = 0; r < 15; r++)
            {
                for (int c = 0; c < 28; c++)
                {
                    //if (!moveFound) // sole move already found
                    //{
                        if (boards.nodesReduced[r, c] == 1 && boards.nodesReduced[r, c + 1] == 2 &&
                            boards.nodesReduced[r, c + 2] == 1 && boards.nodesReduced[r + 1, c] == 10 &&
                            boards.nodesReduced[r + 1, c + 1] == 10 && boards.nodesReduced[r + 1, c + 2] == 10)
                        {
                            int[] right1 = new int[2]; int[] right2 = new int[2]; // right clicks
                            right1[0] = r + 1; right1[1] = c; right2[0] = r + 1; right2[1] = c + 2;
                            rightClicks.Push(right1); rightClicks.Push(right2);
                            int[] double1 = new int[2]; int[] double2 = new int[2]; // double clicks
                            double1[0] = r; double1[1] = c; double2[0] = r; double2[1] = c + 2;
                            doubleClicks.Push(double1); doubleClicks.Push(double2);
                            //moveFound = true;
                        }
                        else
                        if (boards.nodesReduced[r, c] == 10 && boards.nodesReduced[r, c + 1] == 10 &&
                            boards.nodesReduced[r, c + 2] == 10 && boards.nodesReduced[r + 1, c] == 1 &&
                            boards.nodesReduced[r + 1, c + 1] == 2 && boards.nodesReduced[r + 1, c + 2] == 1)
                        {
                            int[] right1 = new int[2]; int[] right2 = new int[2]; // right clicks
                            right1[0] = r; right1[1] = c; right2[0] = r; right2[1] = c + 2;
                            rightClicks.Push(right1); rightClicks.Push(right2);
                            int[] double1 = new int[2]; int[] double2 = new int[2]; // double clicks
                            double1[0] = r + 1; double1[1] = c; double2[0] = r + 1; double2[1] = c + 2;
                            doubleClicks.Push(double1); doubleClicks.Push(double2);
                            //moveFound = true;
                        }
                    //}
                }
            }
            //if (!moveFound) // because im lazy we're gonna find one advanced move at a time
            //{
                //vertical 1-2-1 adaptive
                for (int r = 0; r < 14; r++)
                {
                    for (int c = 0; c < 29; c++)
                    {
                        //if (!moveFound) // sole move already found
                        //{
                            if (boards.nodesReduced[r, c] == 1 && boards.nodesReduced[r + 1, c] == 2 &&
                                boards.nodesReduced[r + 2, c] == 1 && boards.nodesReduced[r, c + 1] == 10 &&
                                boards.nodesReduced[r + 1, c + 1] == 10 && boards.nodesReduced[r + 2, c + 1] == 10)
                            {
                                int[] right1 = new int[2]; int[] right2 = new int[2]; // right clicks
                                right1[0] = r; right1[1] = c + 1; right2[0] = r + 2; right2[1] = c + 1;
                                rightClicks.Push(right1); rightClicks.Push(right2);
                                int[] double1 = new int[2]; int[] double2 = new int[2]; // double clicks
                                double1[0] = r; double1[1] = c; double2[0] = r + 2; double2[1] = c;
                                doubleClicks.Push(double1); doubleClicks.Push(double2);
                                //moveFound = true;
                            }
                            
                            if (boards.nodesReduced[r, c] == 10 && boards.nodesReduced[r + 1, c] == 10 &&
                                boards.nodesReduced[r + 2, c] == 10 && boards.nodesReduced[r, c + 1] == 1 &&
                                boards.nodesReduced[r + 1, c + 1] == 2 && boards.nodesReduced[r + 2, c + 1] == 1)
                            {
                                int[] right1 = new int[2]; int[] right2 = new int[2]; // right clicks 
                                right1[0] = r; right1[1] = c; right2[0] = r + 2; right2[1] = c;
                                rightClicks.Push(right1); rightClicks.Push(right2);
                                int[] double1 = new int[2]; int[] double2 = new int[2]; // double clicks
                                double1[0] = r; double1[1] = c + 1; double2[0] = r + 2; double2[1] = c + 1;
                                doubleClicks.Push(double1); doubleClicks.Push(double2);
                                //moveFound = true;
                            }
                        //}
                    }
                }
            //}
            return (doubleClicks, rightClicks);
        }

        static (Stack<int[]>, Stack<int[]>) Check1221(Boards boards, StringBuilder log)
        {
            Stack<int[]> doubleClicks = new Stack<int[]>();
            Stack<int[]> rightClicks = new Stack<int[]>();

            printBoard(boards.nodesReduced, log, true);
            //printBoard(boards.nodesReduced);

            // horizontal 1-2-2-1 adaptive
            for (int r = 0; r < 15; r++) // (16 - 1)
            {
                for (int c = 0; c < 27; c++) // (30 - 3)
                {
                    if (boards.nodesReduced[r, c] == 1 && boards.nodesReduced[r, c+1] == 2
                        && boards.nodesReduced[r, c+2] == 2 && boards.nodesReduced[r, c+3] == 1
                        && boards.nodesReduced[r+1, c] == 10 && boards.nodesReduced[r+1, c+1] == 10
                        && boards.nodesReduced[r+1, c+2] == 10 && boards.nodesReduced[r+1, c+3] == 10)
                    { //unknowns above
                        int[] right1 = new int[2]; int[] right2 = new int[2]; int[] double1 = new int[2]; int[] double2 = new int[2];
                        right1[0] = r+1; right1[1] = c + 1; right2[0] = r+1; right2[1] = c + 2;                        
                        rightClicks.Push(right1); rightClicks.Push(right2);
                        double1[0] = r; double1[1] = c + 1; double2[0] = r; double2[1] = c + 2;
                        doubleClicks.Push(double1); doubleClicks.Push(double2);
                    }
                    if (boards.nodesReduced[r, c] == 10 && boards.nodesReduced[r, c + 1] == 10
                        && boards.nodesReduced[r, c + 2] == 10 && boards.nodesReduced[r, c + 3] == 10
                        && boards.nodesReduced[r+1, c] == 1 && boards.nodesReduced[r+1, c + 1] == 2
                        && boards.nodesReduced[r+1, c + 2] == 2 && boards.nodesReduced[r+1, c + 3] == 1)
                    { //unknowns below
                        int[] right1 = new int[2]; int[] right2 = new int[2]; int[] double1 = new int[2]; int[] double2 = new int[2];
                        right1[0] = r; right1[1] = c + 1; right2[0] = r; right2[1] = c + 2;
                        rightClicks.Push(right1); rightClicks.Push(right2);
                        double1[0] = r+1; double1[1] = c + 1; double2[0] = r+1; double2[1] = c + 2;
                        doubleClicks.Push(double1); doubleClicks.Push(double2);
                    }
                }
            }

            // vertical 1-2-2-1 adaptive
            for (int r = 0; r < 13; r++) // (16 - 3)
            {
                for (int c = 0; c < 29; c++) // (30 - 1)
                {
                    if (boards.nodesReduced[r, c] == 1 && boards.nodesReduced[r+1, c] == 2
                        && boards.nodesReduced[r+2, c] == 2 && boards.nodesReduced[r+3, c] == 1
                        && boards.nodesReduced[r, c+1] == 10 && boards.nodesReduced[r+1, c+1] == 10
                        && boards.nodesReduced[r+2, c+1] == 10 && boards.nodesReduced[r+3, c+1] == 10)
                    { //unknowns right
                        int[] right1 = new int[2]; int[] right2 = new int[2]; int[] double1 = new int[2]; int[] double2 = new int[2];
                        right1[0] = r + 1; right1[1] = c+1; right2[0] = r + 2; right2[1] = c+1;
                        rightClicks.Push(right1); rightClicks.Push(right2);
                        double1[0] = r+1; double1[1] = c; double2[0] = r+2; double2[1] = c;
                        doubleClicks.Push(double1); doubleClicks.Push(double2);
                    }
                    if (boards.nodesReduced[r, c] == 10 && boards.nodesReduced[r + 1, c] == 10
                        && boards.nodesReduced[r + 2, c] == 10 && boards.nodesReduced[r + 3, c] == 10
                        && boards.nodesReduced[r, c + 1] == 1 && boards.nodesReduced[r + 1, c + 1] == 2
                        && boards.nodesReduced[r + 2, c + 1] == 2 && boards.nodesReduced[r + 3, c + 1] == 1)
                    { //unknowns left
                        int[] right1 = new int[2]; int[] right2 = new int[2]; int[] double1 = new int[2]; int[] double2 = new int[2];
                        right1[0] = r + 1; right1[1] = c; right2[0] = r + 2; right2[1] = c;
                        rightClicks.Push(right1); rightClicks.Push(right2);
                        double1[0] = r + 1; double1[1] = c+1; double2[0] = r + 2; double2[1] = c+1;
                        doubleClicks.Push(double1); doubleClicks.Push(double2);
                    }
                }
            }
            return (doubleClicks, rightClicks);
        }

        //scans the rim of the game board for a 1-1-1 or 1-1-2 pattern move 
        static Stack<int[]> CheckEdge(Boards boards, StringBuilder log)
        {

            Stack<int[]> leftClicks = new Stack<int[]>();

            //vertical pattern on top and bottom edges
            for (int i = 0; i < 28; i++) // (30 - 2)
            {
                //top with pattern on left
                if (   boards.nodes[0, i] == 10  && boards.nodes[1, i] == 10  && boards.nodes[2, i] == 10
                    && boards.nodes[0, i+1] == 1 && boards.nodes[1, i+1] == 1 && (boards.nodes[2, i+1] == 2 || boards.nodes[2, i + 1] == 1)
                    && boards.nodes[0, i+2] == 0 && boards.nodes[1, i+2] == 0 && boards.nodes[2, i+2] == 0)
                {
                    int[] coords = new int[2]; //why redeclare locally? see note:redeclaration at the bottom
                    coords[0] = 2; coords[1] = i;
                    leftClicks.Push(coords);
                }
                //top with pattern on right
                if (   boards.nodes[0, i+2] == 10 && boards.nodes[1, i+2] == 10 && boards.nodes[2, i+2] == 10
                    && boards.nodes[0, i+1] == 1  && boards.nodes[1, i+1] == 1  && (boards.nodes[2, i + 1] == 2 || boards.nodes[2, i + 1] == 1)
                    && boards.nodes[0, i] == 0    && boards.nodes[1, i] == 0    && boards.nodes[2, i] == 0)
                {
                    int[] coords = new int[2];
                    coords[0] = 2; coords[1] = i+2;
                    leftClicks.Push(coords);
                }
                //bottom with pattern on left
                if (   boards.nodes[15, i] == 10  && boards.nodes[14, i] == 10  && boards.nodes[13, i] == 10
                    && boards.nodes[15, i+1] == 1 && boards.nodes[14, i+1] == 1 && (boards.nodes[13, i+1] == 2 || boards.nodes[13, i + 1] == 1)
                    && boards.nodes[15, i+2] == 0 && boards.nodes[14, i+2] == 0 && boards.nodes[13, i+2] == 0)
                {
                    int[] coords = new int[2];
                    coords[0] = 13; coords[1] = i;
                    leftClicks.Push(coords);
                }
                //bottom with pattern on right
                if (   boards.nodes[15, i+2] == 10 && boards.nodes[14, i+2] == 10 && boards.nodes[13, i+2] == 10
                    && boards.nodes[15, i+1] == 1  && boards.nodes[14, i+1] == 1  && (boards.nodes[13, i + 1] == 2 || boards.nodes[13, i + 1] == 1)
                    && boards.nodes[15, i] == 0    && boards.nodes[14, i] == 0    && boards.nodes[13, i] == 0)
                {
                    int[] coords = new int[2];
                    coords[0] = 13; coords[1] = i+2;
                    leftClicks.Push(coords);
                }
            }
            //horizontal pattern on left and ride edges
            for (int i = 0; i < 14; i++)
            {
                //left with pattern on top
                if (   boards.nodes[i, 0] == 10  && boards.nodes[i, 1] == 10  && boards.nodes[i, 2] == 10
                    && boards.nodes[i+1, 0] == 1 && boards.nodes[i+1, 1] == 1 && (boards.nodes[i+1, 2] == 2 || boards.nodes[i + 1, 2] == 1)
                    && boards.nodes[i+2, 0] == 0 && boards.nodes[i+2, 1] == 0 && boards.nodes[i+2, 2] == 0)
                {
                    int[] coords = new int[2]; //why redeclare locally? see note:redeclaration at the bottom
                    coords[0] = i; coords[1] = 2;
                    leftClicks.Push(coords);
                }
                //left with pattern on bottom
                if (   boards.nodes[i+2, 0] == 10 && boards.nodes[i+2, 1] == 10 && boards.nodes[i+2, 2] == 10
                    && boards.nodes[i+1, 0] == 1  && boards.nodes[i+1, 1] == 1  && (boards.nodes[i + 1, 2] == 2 || boards.nodes[i + 1, 2] == 1)
                    && boards.nodes[i, 0] == 0    && boards.nodes[i, 1] == 0    && boards.nodes[i, 2] == 0)
                {
                    int[] coords = new int[2]; 
                    coords[0] = i+2; coords[1] = 2;
                    leftClicks.Push(coords);
                }
                //right with pattern on top
                if (   boards.nodes[i, 29] == 10    && boards.nodes[i, 28] == 10    && boards.nodes[i, 27] == 10
                    && boards.nodes[i + 1, 29] == 1 && boards.nodes[i + 1, 28] == 1 && (boards.nodes[i + 1, 27] == 2 || boards.nodes[i + 1, 27] == 1)
                    && boards.nodes[i + 2, 29] == 0 && boards.nodes[i + 2, 28] == 0 && boards.nodes[i + 2, 27] == 0)
                {
                    int[] coords = new int[2]; 
                    coords[0] = i; coords[1] = 27;
                    leftClicks.Push(coords);
                }
                //right with pattern on bottom
                if (   boards.nodes[i+2, 29] == 10 && boards.nodes[i+2, 28] == 10 && boards.nodes[i+2, 27] == 10
                    && boards.nodes[i+1, 29] == 1  && boards.nodes[i+1, 28] == 1  && (boards.nodes[i + 1, 27] == 2 || boards.nodes[i + 1, 27] == 1)
                    && boards.nodes[i, 29] == 0    && boards.nodes[i, 28] == 0    && boards.nodes[i, 27] == 0)
                {
                    int[] coords = new int[2]; 
                    coords[0] = i+2; coords[1] = 27;
                    leftClicks.Push(coords);
                }
            }

            return leftClicks;
        }

        // get a simple count of the neighbouring mines
        static int TouchingMines (Boards boards, int r, int c)
        {
            int num = boards.nodes[r, c];
            int mines = 0;
            int[,] tiles = new int[8, 3]; // one for each side + corner, 3 for x, y, state?

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

            // count mines
            for (int i = 0; i < 8; i++)
            {
                if (tiles[i, 2] == 1)
                {
                    if (boards.nodes[tiles[i, 0], tiles[i, 1]] == 9)
                    {
                        mines++; 
                    }
                }
            }

            return mines;
        }

        // given a parameter-passed position in nodes, determine basic move viability via neighbouring mine and empty counts
        static (Boolean, Stack<int[]>) basicMoves(Boards boards, int r, int c)
        {

            int num = boards.nodes[r, c];
            int mines = 0, unknowns = 0;
            int[,] tiles = new int[8, 3]; // one for each side + corner, 3 for x, y, state?
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
            else if (num == mines)
            {
                doubleClick = true;
                boards.nodes[r, c] = 0;
            }
            else if (num == (mines + unknowns)) // set all sides to done
            {
                boards.nodes[r, c] = 0;
                // look through side tiles for those which must be actioned
                for (int i = 0; i < 8; i++)
                {
                    // don't reclick flagged tiles (it'll unflag them)
                    if (tiles[i, 2] == 1 && boards.nodes[tiles[i, 0], tiles[i, 1]] == 10)
                    {
                        int[] tmp = new int[2];
                        tmp[0] = tiles[i, 0]; tmp[1] = tiles[i, 1];
                        //boards.nodes[tiles[i, 0], tiles[i, 1]] = 9; // second instance of this, should delete if bug not fixed
                        rightClicks.Push(tmp); 
                    }
                }
            }


            //Debug.WriteLine("Checking tile r=" + r.ToString() + " c=" + c.ToString());

            //tuple with info
            return (doubleClick, rightClicks);
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

        //prints a copy of the board as understood by the nodes array in the console for debugging purposes
        static void printBoard(int[,] nodes, StringBuilder log, Boolean reduced)
        {
            String line;
            if (reduced)
            {
                Debug.WriteLine("Beginning reduced board print");
                log.AppendLine("Beginning reduced board print");
            } else
            {
                Debug.WriteLine("Beginning real board print");
                log.AppendLine("Beginning real board print");
            }

            line = "row ";
            for (int i=0; i<30; i++) { if (i < 10) { line += "c" + i.ToString() + " "; } else { line += "c" + i.ToString(); } }
            Debug.WriteLine(line);
            log.AppendLine(line);

            for (int r = 0; r < 16; r++)
            {
                if (r < 10) {
                    line = "r" + r.ToString() + ":  ";
                } else {
                    line = "r" + r.ToString() + ": ";
                }
                
                for (int c = 0; c < 30; c++)
                { 
                    //uniformity of spacing for all values < 10 and > -1
                    if (nodes[r, c] == 10 || nodes[r, c] < 0)
                    {
                        line += nodes[r, c].ToString() + " ";
                    } else
                    {
                        line += nodes[r, c].ToString() + "  ";
                    }
                }
                Debug.WriteLine(line);
                log.AppendLine(line);
            }
        }

        //....
        static void randomClick(Boards boards, StringBuilder log)
        {
            List<int[]> unknownPoints = new List<int[]>();

            for (int r = 0; r<16; r++)
            {
                for (int c = 0; c<30; c++)
                {
                    if (boards.nodes[r,c] == 10)
                    {
                        int[] pts = new int[2];
                        pts[0] = r; pts[1] = c;
                        unknownPoints.Add(pts);
                    }
                }
            }

            Random random = new Random();
            int selection = random.Next(0,unknownPoints.Count);

            int[] ptSelected = new int[2];
            ptSelected = unknownPoints[selection];
            LeftClick(ptSelected[0],ptSelected[1], log);

        }

        // when advanced strategy is required a reduced localized mine count is required
        // this function makes a copy of nodes then reduces
        public static void UpdateReduced(Boards boards)
        {
            
            boards.nodesReduced = (int[,])boards.nodes.Clone();
            for (int r = 0; r < 16; r++)
            {
                for (int c = 0; c < 30; c++)
                {
                    if (boards.nodes[r, c] > 0 && boards.nodes[r, c] < 9)
                    {
                        boards.nodesReduced[r, c] = boards.nodesReduced[r, c] - TouchingMines(boards, r, c);
                    }
                }
            }
        }

    }

    // Data structure containing the bot's internal representation of the game board
    public class Boards
    {
        public int[,] nodes = new int[16, 30];
        public int[,] nodesReduced = new int[16, 30];

        public void InitializeNodes()
        {
            for (int r = 0; r < 16; r++)
            {
                for (int c = 0; c < 30; c++)
                {
                    nodes[r, c] = 10;
                }
            }
        }

    }

    //this class holds the template images of the buttons (used for comparison
    //it also contains all functions related to manipulating the images
    public class Images
    {

        static Bitmap initializeScreenie(int width, int height)
        {
            return new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        //dimensions of the gameboard
        public int toplefty = 100;
        public int topleftx = 100;

        public Bitmap scr = initializeScreenie(540, 288); // holds the current game board screenshot
        public Bitmap i1 = new Bitmap(@"../../images/1.bmp");
        public Bitmap i2_0 = new Bitmap(@"../../images/2_0.bmp");
        public Bitmap i2_1 = new Bitmap(@"../../images/2_1.bmp");
        public Bitmap i2_2 = new Bitmap(@"../../images/2_2.bmp");
        public Bitmap i3_1 = new Bitmap(@"../../images/3_1.bmp");
        public Bitmap i3_2 = new Bitmap(@"../../images/3_2.bmp");
        public Bitmap i4_1 = new Bitmap(@"../../images/4_1.bmp");
        public Bitmap i4_2 = new Bitmap(@"../../images/4_2.bmp");
        public Bitmap i5_1 = new Bitmap(@"../../images/5_1.bmp");
        public Bitmap i5_2 = new Bitmap(@"../../images/5_2.bmp");
        public Bitmap i6_1 = new Bitmap(@"../../images/6_1.bmp");
        public Bitmap i6_2 = new Bitmap(@"../../images/6_2.bmp");
        public Bitmap i7 =   new Bitmap(@"../../images/7.bmp");
        //public Bitmap i8 = new Bitmap(@"../../images/8.bmp");
        public Bitmap i0_1 = new Bitmap(@"../../images/e_1.bmp"); // empty lighter
        public Bitmap i0_2 = new Bitmap(@"../../images/e_2.bmp"); // empty mid
        public Bitmap i0_3 = new Bitmap(@"../../images/e_3.bmp"); // empty darker
        public Bitmap i9_1 = new Bitmap(@"../../images/f_1.bmp"); // flag lightest variant
        public Bitmap i9_2 = new Bitmap(@"../../images/f_2.bmp"); // flag light variant
        public Bitmap i9_3 = new Bitmap(@"../../images/f_3.bmp"); // flag dark variant
        public Bitmap i9_4 = new Bitmap(@"../../images/f_4.bmp"); // flag darker variant
        public Bitmap i10_0 = new Bitmap(@"../../images/u_0.bmp"); // unknown lightest variant
        public Bitmap i10_1 = new Bitmap(@"../../images/u_1.bmp"); // unknown very light variant
        public Bitmap i10_2 = new Bitmap(@"../../images/u_2.bmp"); // unknown light variant
        public Bitmap i10_3 = new Bitmap(@"../../images/u_3.bmp"); // unknown dark variant
        public Bitmap i10_4 = new Bitmap(@"../../images/u_4.bmp"); // unknown darker variant
        //public Bitmap iBomb = new Bitmap(@"../../images/bomb.bmp"); // an exploded bomb (game is done)
        //public Bitmap iBomb2 = new Bitmap(@"../../images/bomb2.bmp"); // an exploded bomb (game is done) lighter variant
        public Bitmap iLost = new Bitmap(@"../../images/lost.bmp"); // image of the lost text to recognize failed game

        public void updateScreenie()
        {
            Graphics graphics = Graphics.FromImage(scr);
            graphics.CopyFromScreen(topleftx, toplefty, 0, 0, new Size(540, 288));
            graphics.Dispose();
        }

        public Boolean checkLost()
        {
            if (imgCom(tileImage(4, 4), iLost, 5))
            {
                return true;
            } else {
                return false;
            }
        }
        // return a int value corresponding to the tile of at a grid coordinate
        // 0 is empty or already solved tile
        // 1-8 are straightforward
        // 9 is a mine
        // 10 is an unclicked tile
        public int TileID(int r, int c)
        {

            Bitmap actual = tileImage(r, c);

            /*
            if ((r == 0 && c==0) || (r==15&&c==29))
            {
                //actual.Save("c:\\Users\\Lou\\Desktop\\tile.png", ImageFormat.Png);
                Console.WriteLine((actual.GetPixel(5, 5).R - actual.GetPixel(5, 5).B).ToString());
                Console.WriteLine("");
            }
            */

            //quick check for tile (note this does not confuse with flags b/c they're premarked (= 9) in nodes)
            if (isItATile(actual)) { return 10; }

            if (imgCom(actual, i9_1, 7) ||
                imgCom(actual, i9_2, 7) ||
                imgCom(actual, i9_3, 7) ||
                imgCom(actual, i9_4, 7)) { return 9; } // before for pragmatic purposes
            if (imgCom(actual, i0_1, 3) ||
                imgCom(actual, i0_2, 3) ||
                imgCom(actual, i0_3, 3)) { return 0; } // before for pragmatic purposes
            if (imgCom(actual, i10_0, 5) ||
                imgCom(actual, i10_1, 5) || 
                imgCom(actual, i10_2, 5) || 
                imgCom(actual, i10_3, 5) || 
                imgCom(actual, i10_4, 5)) { return 10; }
            if (imgCom(actual, i1, 5)) { return 1; }
            if (imgCom(actual, i2_0, 5) ||
                imgCom(actual, i2_1, 5) ||
                imgCom(actual, i2_2, 5)) { return 2; }
            if (imgCom(actual, i3_1, 5) ||
                imgCom(actual, i3_2, 5)) { return 3; }
            if (imgCom(actual, i4_1, 5) ||
                imgCom(actual, i4_2, 5)) { return 4; }
            if (imgCom(actual, i5_1, 5) ||
                imgCom(actual, i5_2, 5)) { return 5; }
            if (imgCom(actual, i6_1, 5) ||
                imgCom(actual, i6_2, 5)) { return 6; }
            if (imgCom(actual, i7, 5)) {  return 7; }

            //imgSim(actual, i5)
            //actual.Save("c:\\Users\\Lou\\Desktop\\tile.png", ImageFormat.Png);
            //scr.Save("c:\\Users\\Lou\\Desktop\\board.png", ImageFormat.Png);
            //if all else fails

            return -1; // couldn't ID
            //errorMessage("Could not identify tile ID at row: " + r.ToString() + ", column: " + c.ToString());
            //Environment.Exit(0);
        }

        public Bitmap tileImage(int r, int c)
        {
            int x = c * 18;
            int y = r * 18;
            // Clone a portion of the Bitmap object.
            Rectangle cloneRect = new Rectangle(x, y, 18, 18);
            //Boolean alwaysQuit = true;

            return this.scr.Clone(cloneRect, scr.PixelFormat);
        }

        //compare two images for similarity
        //returns 1 for within 20% tolerance
        //returns 0 for out of tolerance, -1 for size mismatch
        static int imgSim(Bitmap im1, Bitmap im2)
        {

            float diff = 0;

            for (int y = 3; y < im1.Height - 3; y++)
            {
                for (int x = 3; x < im1.Width - 3; x++)
                {
                    diff += (float)Math.Abs(im1.GetPixel(x, y).R - im2.GetPixel(x, y).R) / 255;
                    diff += (float)Math.Abs(im1.GetPixel(x, y).G - im2.GetPixel(x, y).G) / 255;
                    diff += (float)Math.Abs(im1.GetPixel(x, y).B - im2.GetPixel(x, y).B) / 255;
                }
            }
            return (int) (100 * diff / (im1.Width * im1.Height * 3));

        }

        //does a quick single pixel check for an unclicked tile to save processing power
        static Boolean isItATile(Bitmap im1)
        {
            // B (blue) is typically 70 larger than R or G for that pixel, should be a reliable guage
            if ((im1.GetPixel(15, 14).B - im1.GetPixel(15, 14).R) > 50 && (im1.GetPixel(15, 14).B - im1.GetPixel(15, 14).G) > 50 
                && (im1.GetPixel(5, 5).R - im1.GetPixel(5, 5).B) < 200 && (im1.GetPixel(5, 5).R - im1.GetPixel(5, 5).G) < 200)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //checks the pixels of the images cropping out the 3 pixels on all sides
        static Boolean imgCom(Bitmap im1, Bitmap im2, int tol)
        {

            float diff = 0;

            for (int y = 3; y < im1.Height - 3; y++)
            {
                for (int x = 3; x < im1.Width - 3; x++)
                {
                    diff += (float)Math.Abs(im1.GetPixel(x, y).R - im2.GetPixel(x, y).R) / 255;
                    diff += (float)Math.Abs(im1.GetPixel(x, y).G - im2.GetPixel(x, y).G) / 255;
                    diff += (float)Math.Abs(im1.GetPixel(x, y).B - im2.GetPixel(x, y).B) / 255;
                }
            }

            if (tol > 100 * diff / (im1.Width * im1.Height * 3)) { return true; } else { return false; }

        }

        static void errorMessage (String message)
        {
            string caption = "Error Detected in Input";
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            MessageBox.Show(message, caption, buttons);
        }
    }
}


// 0 is empty or already solved tile
// 1-8 are straightforward
// 9 is a mine
// 10 is an unclicked tile

/* NOTE:REDECLARATION
 * The stack function works in a counter-intuitive (at least, to me) way. Pushing a varaible to 
 * the stack does not create a copy of the data but adds a reference. This inhibits using any sort of looping for
 * stack creation because the 
*/