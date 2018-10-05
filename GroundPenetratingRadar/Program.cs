using System.Drawing;
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

        //send left click
        public static void LeftClick(int y, int x)
        {
            // add code to make this independant of screen size: Rectangle screenSize = Screen.PrimaryScreen.Bounds;
            int ypos = (int)(65536.0 / 1080 * (109 + y * 18));
            int xpos = (int)(65536.0 / 1920 * (109 + x * 18));

            //SimMouse.Click(MouseButtons.Left, xpos, ypos, 10);

            //SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, xpos, ypos, 0, 0);

            Debug.WriteLine("Left clicked at x: " + xpos.ToString() + ", y: " + ypos.ToString());
        }

        //send right click
        public static void rightClick(int x, int y)
        {
            int xpos = 109 + x * 18;
            int ypos = 109 + y * 18;
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_RIGHTUP, xpos, ypos, 0, 0);
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
                Debug.WriteLine("Looks like she wasn't running, bud, sorry");
            }
            System.Threading.Thread.Sleep(5000);
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

            Images images = new Images(); //our "everything to do with bitmaps and tile ID" class instance

            //for debugging: Screenie(proc);

            System.Threading.Thread.Sleep(300);

            //Make the first move. 8,15 is the middle, i.e. the spot the least likely to get corner stuck
            LeftClick(8, 15);
            //UpdateGridByPoint(boards, 8, 15); -----is this necessary by colour?----

            // main loop, baby
            Boolean complete = false;
            int[,,] nextMoves = new int[10, 1, 1];
            while (complete == false)
            {
                System.Threading.Thread.Sleep(300);

                images.updateScreenie();
                UpdateGrid(boards, images);
                //printBoard(boards); for console debugging
                
                //first do a check for obvious moves, and if any are found, save them as basicResults
                var basicResults = CheckBasics(boards);

                //printBoard(boards)

                if (basicResults.Item1.Count != 0 || basicResults.Item2.Count != 0)
                {
                    printBoard(boards);
                    while (basicResults.Item1.Count != 0)
                    {
                        int[] pos = basicResults.Item1.Pop();
                        //DoubleLeftClick(boards, pos[0], pos[1]);
                    }
                    while (basicResults.Item2.Count != 0)
                    {
                        int[] pos = basicResults.Item2.Pop();
                        //RightClick(boards, pos[0], pos[1]);
                    }
                } else { //if advanced

                    // check heuristics
                    // call basic inverted edge reading
                    Debug.WriteLine("no basic moves found");//go to advanced
                    SetForegroundWindow(proci32);
                } 

                complete = true;
            }
        }

        //method to update grid, only updating values which are still unknown, to save processing power
        static void UpdateGrid(Boards boards, Images images)
        {
            for (int i = 0; i < 16; i++)
            {
                Debug.WriteLine("Row: " + i.ToString());
                for (int j = 0; j < 30; j++)
                {
                    if (boards.nodes[i, j] == 10)
                    {
                        boards.nodes[i, j] = images.TileID(i, j);
                    }
                }
            }
        }

        //expands a square grid outwards from a point to identify changes, comparing # of unkowns
        //this method should be replaced with a more efficient edge-huggin-expansion algorithm
        static Boolean UpdateGridByPoint(Images images, Boards boards, int r, int c)
        {
            // CURRENTLY: implicit that this is called ONLY after cicked a point, so check the given point
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
            /*
            if (r == 9 && c == 14)
            {
                Debug.WriteLine("WE HIT THE PROBLEMED CASE HERE");
            }
            */
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

            Debug.WriteLine("Checking tile r=" + r.ToString() + " c=" + c.ToString());

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

        //static void LeftClick(Boards boards, int r, int c) { boards.rows[r].Items[c].Click(); }
        //static void DoubleLeftClick(Boards boards, int r, int c) { boards.rows[r].Items[c].DoubleClick(); }
        //static void RightClick(Boards boards, int r, int c) { boards.rows[r].Items[c].RightClick(); }

        //prints a copy of the board as understood by the nodes array in the console for debugging purposes
        static void printBoard(Boards boards)
        {
            Debug.WriteLine("Beginning board debug print");
            for (int r = 0; r < 16; r++)
            {
                for (int c = 0; c < 30; c++)
                {
                    //uniformity of spacing for all values < 10
                    if (boards.nodes[r, c] == 10)
                    {
                        Debug.Write(boards.nodes[r, c].ToString() + " ");
                    } else
                    {
                        Debug.Write(boards.nodes[r, c].ToString() + "  ");
                    }
                }
                Debug.WriteLine("");
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

        /*
        private Image ScreenGrab()
        {

            return null;
        }
        */

        //do some fancy shit on the matrix and try to find where some moves are

    }

    public class Boards
    {

        public int[,] nodes = new int[16, 30];

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
        public Bitmap i2 = new Bitmap(@"../../images/2.bmp");
        public Bitmap i3 = new Bitmap(@"../../images/3.bmp");
        public Bitmap i4 = new Bitmap(@"../../images/4.bmp");
        public Bitmap i5 = new Bitmap(@"../../images/5.bmp");
        public Bitmap i6 = new Bitmap(@"../../images/6.bmp");
        //public Bitmap i7 = new Bitmap(@"../../images/7.bmp");
        //public Bitmap i8 = new Bitmap(@"../../images/8.bmp");
        public Bitmap i0 = new Bitmap(@"../../images/e.bmp"); // empty
        public Bitmap i9 = new Bitmap(@"../../images/f.bmp"); // flag
        public Bitmap i10_1 = new Bitmap(@"../../images/u_1.bmp"); // unknown lightest variant
        public Bitmap i10_2 = new Bitmap(@"../../images/u_2.bmp"); // unknown light variant
        public Bitmap i10_3 = new Bitmap(@"../../images/u_3.bmp"); // unknown dark variant
        public Bitmap i10_4 = new Bitmap(@"../../images/u_4.bmp"); // unknown darker variant

        public void updateScreenie()
        {
            Graphics graphics = Graphics.FromImage(scr);
            graphics.CopyFromScreen(topleftx, toplefty, 0, 0, new Size(540, 288));
            graphics.Dispose();
            //Debug.WriteLine("board image printed");
        }

        // return a int value corresponding to the tile of at a grid coordinate
        // 0 is empty or already solved tile
        // 1-8 are straightforward
        // 9 is a mine
        // 10 is an unclicked tile
        public int TileID(int r, int c)
        {

            Bitmap actual = tileImage(r, c);

            
            if (r == 5 & c == 15)
            {
                Debug.WriteLine("");
            }

            //code below used while debugging to save a copy of the image of the tile to file
            //scr.Save("c:\\Users\\Lou\\Desktop\\tile.png", ImageFormat.Png);
            if (imgCom(actual, i0, 3, true)) { return 0; }
            if (imgCom(actual, i10_1, 13, false) || 
                imgCom(actual, i10_2, 13, false) || 
                imgCom(actual, i10_3, 13, false) || 
                imgCom(actual, i10_4, 13, false)) { return 10; }
            if (imgCom(actual, i1, 5, true)) { return 1; }
            if (imgCom(actual, i2, 5, true)) { return 2; }
            if (imgCom(actual, i3, 5, true)) { return 3; }
            if (imgCom(actual, i4, 5, true)) { return 4; }
            if (imgCom(actual, i5, 5, true)) { return 5; }
            if (imgCom(actual, i6, 5, true)) { return 6; }
            //if (imgCom(actual, i7, 5, true)) { return 7; }
            //if (imgCom(actual, i8, 5, true)) { return 8; }
            if (imgCom(actual, i9, 5, true)) {
                return 9;
            }
            
            
            //imgSim(actual, i0, true)
            //actual.Save("c:\\Users\\Lou\\Desktop\\tile.png", ImageFormat.Png);
            //scr.Save("c:\\Users\\Lou\\Desktop\\board.png", ImageFormat.Png);
            //if all else fails
            errorMessage("Could not identify tile ID at row: " + r.ToString() + ", column: " + c.ToString());
            Environment.Exit(0);
            return 0;
        }

        public Bitmap tileImage (int r, int c)
        {
            int x = c * 18;
            int y = r * 18;
            // Clone a portion of the Bitmap object.
            Rectangle cloneRect = new Rectangle(x, y, 18, 18);
            return scr.Clone(cloneRect, scr.PixelFormat);
        }

        //compare two images for similarity
        //returns 1 for within 20% tolerance
        //returns 0 for out of tolerance, -1 for size mismatch
        static int imgSim(Bitmap im1, Bitmap im2, Boolean offFlag)
        {

            int offset = 0;
            if (offFlag) { offset = 3; }

            float diff = 0;

            for (int y = offset; y < im1.Height - offset; y++)
            {
                for (int x = offset; x < im1.Width - offset; x++)
                {
                    diff += (float)Math.Abs(im1.GetPixel(x, y).R - im2.GetPixel(x, y).R) / 255;
                    diff += (float)Math.Abs(im1.GetPixel(x, y).G - im2.GetPixel(x, y).G) / 255;
                    diff += (float)Math.Abs(im1.GetPixel(x, y).B - im2.GetPixel(x, y).B) / 255;
                }
            }
            return (int) (100 * diff / (im1.Width * im1.Height * 3));

        }

        static Boolean imgCom(Bitmap im1, Bitmap im2, int tol, Boolean offFlag)
        {

            //for some tiles there can be multiple permeatations of shadows on the edge, this flag enabled effectively crops those out
            int offset = 0;
            if (offFlag) { offset = 3; }

            if (im1.Size.Height != 18 || im1.Size.Width != 18)
            {
                errorMessage("bad input to imgCom");
                Environment.Exit(0);
                return false;
            }
            if (im2.Size.Height != 18 || im2.Size.Width != 18)
            {
                errorMessage("bad input to imgCom");
                Environment.Exit(0);
                return false;
            }

            float diff = 0;

            for (int y = offset; y < im1.Height - offset; y++)
            {
                for (int x = offset; x < im1.Width - offset; x++)
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