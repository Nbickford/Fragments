//HAIKU DOCUMENTATION TIME!
//A wonderful game
//programmed over a long time
//Hope you enjoy it

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Security.Cryptography;

namespace Doughboy {

    public enum Direction { Up = 0, Left = 1, Down = 2, Right = 3, None = 4 };
    /// <summary>
    /// The main type for Doughboy:Assault!
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game {
        //Graphics and systems.
        public static GraphicsDeviceManager graphics;
        public static SpriteBatch spriteBatch;
        public static InputHandler input;
        public static IntVector2 screenSize;

        //Game parameters.
        public static int[,] board;
        public static int[,] vars; //variables for items
        public static int boardWidth=14;
        public static int boardHeight=12;

        //Collections of boards (used for the menu screen)
        public int[][,] boards;
        public string[] boardNames;

        //Resources
        public static Dictionary<String, Texture2D> images;
        public static Dictionary<String, Texture2D> miniimages;
        public static Dictionary<String, SpriteFont> fonts;
        bool isImport;
        bool hasLoaded;

        //Doughboy subsystems:
        public static PurchaseMenu pm;
        public static DoughboyBoxes breadbox;
        public static Random rand;
        public static DJ hammieham;
        public static OptionsMenu mnuOptions;
        public VideoRenderer vr;

        //State:
        public static int whoseTurn; //Keeps track of the turn of the game. Has six possible states; 0 and 3 are player turns.
        public static double time;
        bool videoMode = false; //True iff we're recording video.

        public static int currentDoughboy; //Points to the current Doughboy in breadbox.
        public static Cow currentCow; //only one Cow on the screen at a time
        public static List<SmartMine> smartmines;//but there can be many SmartMines.

        public static Fauxboy fauxboy;
        public static int currentScenario;
        public static string[] fboyprogs;
        public static string[] fboytexts;
        double scenarioTime;

        //Menu screen state:
        List<double> clickTimes; //for scrolling in the menu screen
        int lvlSelectIndex;
        float lastOset = 0;
        Button btnCredits;
        Button btnBack;
        Button btnBack2;
        Button btnOptions;

        //Screen transitions:
        double animTime;
        int whichAnimation; //0=down to credits. 1=up from credits. 2=zoom to level select screen

        //Animation resources:
        public static Player[] players;
        public static Texture2D[][] doughboyframes;
        public static Texture2D[][] doughboyjumps;
        public static Texture2D[][] cowframes;
        public static Texture2D[][] animationframes;
        public static Texture2D[][] bunkerimages;
        public static Texture2D[][] guillotineframes;
        public static Texture2D[][] musicopts;

        public static Texture2D words;
        public static Texture2D pixeltex;

        public static Texter texter; //Handles turn notifications.

        //Holds state of animations and particle systems:
        public static List<Animation> animations;
        public static List<Particle> particles;
        public static int numFrames;
        public static double turnStart;
        public static int numRounds;

        public static int[] wallIDs = new int[] { 1, 2, 3 };//places Doughboys cannot go

        public static float endTime; //Keeps track of when the game ended.

        //Tutorial/attract mode state:
        public static string[] scenarios;
        public static TutorialHandler tutorial;

        //Global settings (from the Settings menu)
        public static bool showEnemyMines;
        public static bool playClicks;

        //Loading screen:
        Color[] glitchscreen;
        RenderTarget2D drawbuffer;

        public Game1() {
            //Set up the game's window.
            graphics = new GraphicsDeviceManager(this);
            screenSize = new IntVector2(896, 768);

                System.Windows.Forms.Form gameForm = (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(Window.Handle); //sorry about this, but it's to remove ambiguity later on.
                gameForm.MinimumSize = new System.Drawing.Size(screenSize.X / 2, screenSize.Y / 2);

            //FitInScreen();
            this.Window.AllowUserResizing = true;
            this.Window.Title = "Doughboy: Assault!";
            //this.Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);

            hasLoaded = false;
            Content.RootDirectory = "Content";
        }

        protected override void OnExiting(Object sender, EventArgs args) {
            // Stop the threads
            if (vr != null) {
                vr.StopThreads();
            }
            base.OnExiting(sender, args);


        }

        public static void FitInScreen() {
            //Resizes the window to fit in the user's screen, if at least one dimension is less than
            //the normal window width.
            float scale = 1.0f;
            if (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width < screenSize.X ||
                GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height < screenSize.Y||
                (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height==screenSize.Y && !graphics.IsFullScreen)) {
                scale = 0.5f;
            }
            graphics.PreferredBackBufferHeight = (int)(scale * 768);
            graphics.PreferredBackBufferWidth = (int)(scale * 896);
            graphics.ApplyChanges();
            //We don't need to renitialize the InputHandler; it takes care of itself.
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            // TODO: Add your initialization logic here
            glitchscreen = new Color[screenSize.X * screenSize.Y / 4];
            for (int i = 0; i < glitchscreen.Length; i++) {
                glitchscreen[i] = Color.Black;
            }

            rand = new Random();
            numFrames = 0;
            turnStart = 0;
            input = new InputHandler();

            LoadBoard(); //Load the default board so that other classes can work off of that

            //import a intro.dboy
            if (!File.Exists("intro.dboy")) {
                throw new Exception("Someone's messed with your intro.dboy file! #8");
            }
            try {
                TextReader tr = new StreamReader("intro.dboy");
                string ts = tr.ReadToEnd();
                scenarios = System.Text.RegularExpressions.Regex.Split(ts, "scenario [0-9]*");

            } catch (Exception ex) {
                throw new Exception("Someone's messed with your intro.dboy file! #9: " + ex.ToString());
            }
            
            //preferences
            graphics.IsFullScreen = BaseRoutines.ReadPreference("Fullscreen");
            FitInScreen();

            currentCow = null;
            breadbox = new DoughboyBoxes(new IntVector2(7 * 64, 11 * 64));
            pm = new PurchaseMenu();
            mnuOptions = new OptionsMenu();
            mnuOptions.LoadUDIndex();

            animations = new List<Animation>();

            players = new Player[2];
            players[0] = new Player(0, mnuOptions.udvals[mnuOptions.udindex]);
            players[1] = new Player(1, mnuOptions.udvals[mnuOptions.udindex]);

            showEnemyMines = BaseRoutines.ReadPreference("Mines");
            playClicks = BaseRoutines.ReadPreference("Clicks");

            particles = new List<Particle>();

            hammieham = new DJ();

            texter = new Texter();

            time = 0;
            whoseTurn = -1;
            numRounds = 1;
            currentDoughboy = -1;
            endTime = -1f;
            scenarioTime = -15;

            animTime = -1;
            whichAnimation = 1;

            
            if (videoMode) {
                vr = new VideoRenderer();
            }
            

            tutorial = new TutorialHandler();

            clickTimes = new List<double>();
            //Stages of turns:
            //-1: Intro
            //0:Place traps and order doughboys
            //1:Doughboys move
            //2: Cows move
            //3-5 second player

            base.Initialize();
        }

        /// <summary>
        /// Does a quick reinitialization of the game. CONTENT MUST BE LOADED before you can call this function.
        /// </summary>
        public void Reinitialize() {
            numFrames = 0;
            turnStart = 0;
            currentCow = null;
            breadbox = new DoughboyBoxes(new IntVector2(7 * 64, 11 * 64));
            breadbox.LoadContent(Content);
            players = new Player[2];
            players[0] = new Player(0, mnuOptions.udvals[mnuOptions.udindex]);
            players[1] = new Player(1, mnuOptions.udvals[mnuOptions.udindex]);
            texter = new Texter();
            pm = new PurchaseMenu();
            pm.LoadContent(Content);

            time = 0;
            whoseTurn = -1;
            numRounds = 1;
            currentDoughboy = -1;
            endTime = -1f;
            scenarioTime = -15;

            animTime = -1;
            whichAnimation = 1;

            tutorial = new TutorialHandler();
            tutorial.LoadContent(Content);

            clickTimes = new List<double>();
        }

        public void LoadBoard() {
            //NOTE: 0 is ground, 1 is wall, 2 is doughboybane (cows only), 3 is invisiblewall (aka barbwire), 4 is broken wall.
            //5-9 are traps, 32 is cowfood
            //TRAPS:
            //5: Mine
            //6: Springboard
            //7: Guillotine
            //8: Spent guillotine
            //9: Empty pit
            //10: Pit with dead doughboy/drillboy
            //11: Pit with dead disarmer
            //12: Pit with dead bomber

            board = new int[8 + 4, 10 + 4]
            {
                {2,2, 2,2,2,2,2,2,2,2,2,2, 2,2},
                {2,0, 0,3,3,3,3,3,3,3,3,0, 0,2},
                
                {2,0, 0,0,0,1,0,0,1,0,0,0, 0,2},
                {2,2, 0,0,0,1,0,0,1,0,0,0, 2,2},
                {2,2, 0,0,0,1,0,0,1,0,0,0, 2,2},
                {2,0, 0,0,0,1,0,0,1,0,0,0, 0,2},
                {2,0, 0,0,0,1,0,0,1,0,0,0, 0,2},
                {2,2, 0,0,0,1,0,0,1,0,0,0, 2,2},
                {2,2, 0,0,0,1,0,0,1,0,0,0, 2,2},
                {2,0, 0,0,0,1,0,0,1,0,0,0, 0,2},

                {2,0, 0,3,3,3,3,3,3,3,3,0, 0,2},
                {2,2, 2,2,2,2,2,2,2,2,2,2, 2,2}
            };

            //Vars:
            //nibble 0: Is it disarmed? (this is actually a bit)
            //nibble 1: Which player placed the item? 0 is Left, 1 is Right, 15 is Game
            //byte 1:   Item-specific variables. For example, this is (rounds to go) for cowbait.
            vars = new int[8 + 4, 10 + 4];
            for (int y = 0; y < 12; y++)
                for (int x = 0; x < 14; x++)
                    vars[y, x] = 0x00F0;

            //Import a seekret.txt if there is one.
            //This allows us to load in and test out arbitrary boards
            //without modifying the levels file.
            try {
                if (File.Exists("seekret.txt")) {
                    TextReader tr = new StreamReader("seekret.txt");
                    for (int y = 0; y < 8; y++) {
                        string[] chars = tr.ReadLine().Split(',');
                        for (int x = 0; x < 10; x++) {
                            int tn;
                            tn = int.TryParse(chars[x], out tn) ? tn : 0;
                            board[y + 2, x + 2] = tn;
                            if (tn == 32) {
                                vars[y + 2, x + 2] = 0x03F0;
                            }
                        }
                    }
                    tr.Close();
                    isImport = true;
                }

            } catch (Exception) {
                Console.WriteLine("Starting...");
            }
            lvlSelectIndex = 0;
        }

        public void LoadBoards(String filename) {
            filename = "Boards\\" + filename;
            try {
                int[,] tempboard = new int[8 + 4, 10 + 4]
            {
                {2,2, 2,2,2,2,2,2,2,2,2,2, 2,2},
                {2,0, 0,3,3,3,3,3,3,3,3,0, 0,2},
                
                {2,0, 0,0,0,0,0,0,0,0,0,0, 0,2},
                {2,2, 0,0,0,0,0,0,0,0,0,0, 2,2},
                {2,2, 0,0,0,0,0,0,0,0,0,0, 2,2},
                {2,0, 0,0,0,0,0,0,0,0,0,0, 0,2},
                {2,0, 0,0,0,0,0,0,0,0,0,0, 0,2},
                {2,2, 0,0,0,0,0,0,0,0,0,0, 2,2},
                {2,2, 0,0,0,0,0,0,0,0,0,0, 2,2},
                {2,0, 0,0,0,0,0,0,0,0,0,0, 0,2},

                {2,0, 0,3,3,3,3,3,3,3,3,0, 0,2},
                {2,2, 2,2,2,2,2,2,2,2,2,2, 2,2}
            };
                TextReader tr = new StreamReader(filename);
                string whole = tr.ReadToEnd();
                tr.Close();

                //check hash
                if (filename == "boards.dboy") {
                } //what if Kris wants to modify it?

                int boardIndex = 0;
                string[] lines = whole.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                while (lines[boardIndex] != "BEGIN") {
                    boardIndex++;
                }
                List<string> boardnameslist = new List<string>();
                List<int[,]> boardslist = new List<int[,]>();
                boardIndex++;
                string pack = lines[boardIndex]; //Use this maybe?
                boardIndex++;
                int numBoard = 0;
                while (boardIndex < lines.Length) {
                    boardnameslist.Add(FormatForTitle(lines[boardIndex]));
                    boardslist.Add(BaseRoutines.Copy2DArray(tempboard));

                    for (int y = 0; y < 8; y++) {
                        boardIndex++;
                        string[] chars = lines[boardIndex].Split(',');
                        for (int x = 0; x < 10; x++) {
                            boardslist[numBoard][y + 2, x + 2] = int.Parse(chars[x]);
                        }
                    }
                    //enforce emptiness of squares just outside bunkers
                    boardslist[numBoard][5,2] = 0;
                    boardslist[numBoard][6,2] = 0;
                    boardslist[numBoard][5,11] = 0;
                    boardslist[numBoard][6,11] = 0;

                    boardIndex++;
                    numBoard++;
                }

                boards = boardslist.ToArray();
                boardNames = boardnameslist.ToArray();


            } catch (Exception) {
                throw new Exception("It looks like your boards.dboy file is corrupted. Get a new one, perhaps?");
            }
            lvlSelectIndex = 0;
        }

        private String FormatForTitle(String str) {
            char[] ar = str.ToCharArray();
            System.Collections.ObjectModel.ReadOnlyCollection<char> supchars = fonts["cost"].Characters;
            for (int i = 0; i < ar.Length; i++) {
                if (!supchars.Contains(ar[i])) {
                    ar[i] = '?';
                }
            }
            return new String(ar);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);


            //IMPROMPTU DRAW!
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();

            fonts = new Dictionary<string, SpriteFont>();
            fonts.Add("cost", Content.Load<SpriteFont>("53block12"));
            fonts.Add("04-18", Content.Load<SpriteFont>("04s18"));
            fonts.Add("04-12", Content.Load<SpriteFont>("04s12"));

            IntVector2 realsize = new IntVector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Vector2 strsize = fonts["cost"].MeasureString("LOADING...") * 2;
            spriteBatch.DrawString(fonts["cost"], "LOADING...", new Vector2((realsize.X - strsize.X) / 2, (realsize.Y - strsize.Y) / 2), Color.White, 0, Vector2.Zero, 2.0f, SpriteEffects.None, 0);
            spriteBatch.End();
            graphics.GraphicsDevice.Present();

            //Load Doughboy boards -see also Initialize
            LoadBoards("boards.dboy");

            images = new Dictionary<string, Texture2D>();
            miniimages = new Dictionary<string, Texture2D>();

            images.Add("field", Content.Load<Texture2D>("field-kris"));
            images.Add("cursor", Content.Load<Texture2D>("kris cursor"));
            images.Add("wall", Content.Load<Texture2D>("kris wall 2"));
            miniimages.Add("wall", Content.Load<Texture2D>("mini\\mini wall"));

            images.Add("brokenwall", Content.Load<Texture2D>("kris wall 2-rubble"));
            images.Add("doughboy", Content.Load<Texture2D>("kris doughboy"));
            images.Add("cow", Content.Load<Texture2D>("kris cow"));
            images.Add("shadow", Content.Load<Texture2D>("shadow-kris"));
            images.Add("noshadow", Content.Load<Texture2D>("object can not be placed"));
            images.Add("mine", Content.Load<Texture2D>("kris mine"));
            miniimages.Add("mine", Content.Load<Texture2D>("mini\\mini mine"));

            images.Add("pit", Content.Load<Texture2D>("pit-trap"));
            miniimages.Add("pit", Content.Load<Texture2D>("mini\\mini pit trap"));
            images.Add("filled pit", Content.Load<Texture2D>("pit-trap (with dead-boy)"));
            images.Add("filled pit 2", Content.Load<Texture2D>("pit-trap (with dead-boy) 2"));
            images.Add("filled pit 3", Content.Load<Texture2D>("pit-trap (with dead-boy) 3"));
            miniimages.Add("guillotine", Content.Load<Texture2D>("mini\\mini guillotine"));

            images.Add("wire", Content.Load<Texture2D>("barbwire"));
            images.Add("wire-left", Content.Load<Texture2D>("barbwire (left end)"));
            images.Add("wire-right", Content.Load<Texture2D>("barbwire (right end)"));
            images.Add("wire-both", Content.Load<Texture2D>("barbwire (both ends)"));
            miniimages.Add("wire-left", Content.Load<Texture2D>("mini\\mini barb 1"));
            miniimages.Add("wire", Content.Load<Texture2D>("mini\\mini barb 2"));
            miniimages.Add("wire-right", Content.Load<Texture2D>("mini\\mini barb 3"));


            images.Add("spring", Content.Load<Texture2D>("spring board"));
            miniimages.Add("spring", Content.Load<Texture2D>("mini\\mini spring board"));

            images.Add("grass", Content.Load<Texture2D>("grass"));
            miniimages.Add("grass 1", Content.Load<Texture2D>("mini\\mini grass 1"));
            miniimages.Add("grass 2", Content.Load<Texture2D>("mini\\mini grass 2"));

            images.Add("cow bait 0", Content.Load<Texture2D>("cow frames\\cow bait rot 0"));
            images.Add("cow bait 1", Content.Load<Texture2D>("cow frames\\cow bait rot 1"));
            images.Add("cow bait 2", Content.Load<Texture2D>("cow frames\\cow bait rot 2"));
            images.Add("cow bait 3", Content.Load<Texture2D>("cow frames\\cow bait rot 3"));

            images.Add("cow boot e", Content.Load<Texture2D>("cow frames\\cow boot EAST"));
            images.Add("cow boot w", Content.Load<Texture2D>("cow frames\\cow boot WEST"));
            images.Add("zzz 1", Content.Load<Texture2D>("zzz shock and awe 1"));
            images.Add("zzz 2", Content.Load<Texture2D>("zzz shock and awe 2"));

            images.Add("dirt br", Content.Load<Texture2D>("dirt\\dirt 1"));
            images.Add("dirt ur", Content.Load<Texture2D>("dirt\\dirt 2"));
            images.Add("dirt bl", Content.Load<Texture2D>("dirt\\dirt 3"));
            images.Add("dirt ul", Content.Load<Texture2D>("dirt\\dirt 4"));

            images.Add("glitch line", Content.Load<Texture2D>("glitch1"));
            images.Add("glitch 0", Content.Load<Texture2D>("glitch2"));

            images.Add("title-bg", Content.Load<Texture2D>("title\\title page bg"));
            images.Add("title-text", Content.Load<Texture2D>("title\\testtitletext kris"));
            images.Add("title", Content.Load<Texture2D>("title\\doughboy title fixed"));
            images.Add("menu image", Content.Load<Texture2D>("title\\menu image"));
            images.Add("credits", Content.Load<Texture2D>("title\\credits page 2.0"));
            images.Add("credits map", Content.Load<Texture2D>("title\\credits map"));
            images.Add("title text credits", Content.Load<Texture2D>("title\\title text credits"));
            images.Add("back button", Content.Load<Texture2D>("title\\title back"));
            images.Add("options button", Content.Load<Texture2D>("title\\title text options"));

            images.Add("map frame", Content.Load<Texture2D>("lvlselect\\slide bar frame"));
            images.Add("map cell", Content.Load<Texture2D>("lvlselect\\slide cell"));
            images.Add("map header", Content.Load<Texture2D>("lvlselect\\map name place"));
            images.Add("left arrow", Content.Load<Texture2D>("lvlselect\\left arrow"));
            images.Add("right arrow", Content.Load<Texture2D>("lvlselect\\right arrow"));

            musicopts = new Texture2D[2][];
            musicopts[0] = new Texture2D[2];
            musicopts[0][0] = Content.Load<Texture2D>("opt music off");
            musicopts[0][1] = Content.Load<Texture2D>("opt music on");
            musicopts[1] = new Texture2D[2];
            musicopts[1][0] = Content.Load<Texture2D>("opt audio off");
            musicopts[1][1] = Content.Load<Texture2D>("opt audio on");



            pixeltex = Content.Load<Texture2D>("technical\\pixel");
            btnCredits = new Button(images["title text credits"], new Vector2((screenSize.X - images["title text credits"].Width) / 2, 702), true);
            btnBack = new Button(images["back button"], new Vector2(2 * screenSize.X - images["back button"].Width - 32, screenSize.Y - images["back button"].Height - 16), true);
            btnBack2 = new Button(images["back button"], Vector2.Zero, true);
            btnOptions = new Button(images["options button"], Vector2.Zero, true);


            LoadFrames();

            words = Content.Load<Texture2D>("technical\\words moved");


            pm.LoadContent(Content);
            breadbox.LoadContent(Content);
            tutorial.LoadContent(Content);
            mnuOptions.LoadContent(Content);


            hasLoaded = true;
            // TODO: use this.Content to load your game content here
        }

        public void LoadFrames() {
            doughboyframes = new Texture2D[5][];
            for (int i = 0; i < 5; i++) {
                doughboyframes[i] = new Texture2D[24];
            }

            string folder = "doughboy frames\\";
            ProcessFrames(ref doughboyframes, folder + "kris doughboy NORTH (animation strip)", 0, 0, 6);
            ProcessFrames(ref doughboyframes, folder + "kris doughboy WEST (animation strip)", 0, 6, 6);
            ProcessFrames(ref doughboyframes, folder + "kris doughboy SOUTH (animation strip)", 0, 12, 6);
            ProcessFrames(ref doughboyframes, folder + "kris doughboy EAST (animation strip)", 0, 18, 6);

            ProcessFrames(ref doughboyframes, folder + "kris doughbomb NORTH (animation strip)", 1, 0, 6);
            ProcessFrames(ref doughboyframes, folder + "kris doughbomb WEST (animation strip)", 1, 6, 6);
            ProcessFrames(ref doughboyframes, folder + "kris doughbomb SOUTH (animation strip)", 1, 12, 6);
            ProcessFrames(ref doughboyframes, folder + "kris doughbomb EAST (animation strip)", 1, 18, 6);

            ProcessFrames(ref doughboyframes, folder + "kris drillboy NORTH (animation strip)", 2, 0, 6);
            ProcessFrames(ref doughboyframes, folder + "kris drillboy WEST (animation strip)", 2, 6, 6);
            ProcessFrames(ref doughboyframes, folder + "kris drillboy SOUTH (animation strip)", 2, 12, 6);
            ProcessFrames(ref doughboyframes, folder + "kris drillboy EAST (animation strip)", 2, 18, 6);

            //Kris is such a workaholic!
            ProcessFrames(ref doughboyframes, folder + "kris disarmer NORTH (animation strip)", 3, 0, 6);
            ProcessFrames(ref doughboyframes, folder + "kris disarmer WEST (animation strip)", 3, 6, 6);
            ProcessFrames(ref doughboyframes, folder + "kris disarmer SOUTH (animation strip)", 3, 12, 6);
            ProcessFrames(ref doughboyframes, folder + "kris disarmer EAST (animation strip)", 3, 18, 6);

            ProcessFrames(ref doughboyframes, folder + "z kris drillboy NORTH (depleted)", 4, 0, 6);
            ProcessFrames(ref doughboyframes, folder + "z kris drillboy WEST (depleted)", 4, 6, 6);
            ProcessFrames(ref doughboyframes, folder + "z kris drillboy SOUTH (depleted)", 4, 12, 6);
            ProcessFrames(ref doughboyframes, folder + "z kris drillboy EAST (depleted)", 4, 18, 6);

            folder = "cow frames\\";
            cowframes = new Texture2D[4][];
            for (int i = 0; i < 4; i++)
                cowframes[i] = new Texture2D[8];
            ProcessFrames(ref cowframes, folder + "kris cow NORTH (animation strip)", 0, 0, 8);
            ProcessFrames(ref cowframes, folder + "kris cow WEST (animation strip)", 1, 0, 8);
            ProcessFrames(ref cowframes, folder + "kris cow SOUTH (animation strip)", 2, 0, 8);
            ProcessFrames(ref cowframes, folder + "kris cow EAST (animation strip)", 3, 0, 8);

            folder = "explosions\\";
            animationframes = new Texture2D[28][];

            animationframes[0] = new Texture2D[6];
            animationframes[1] = new Texture2D[6];
            ProcessFrames(ref animationframes, folder + "Dough death- blown up gold", 0, 0, 6);
            ProcessFrames(ref animationframes, folder + "Dough death- blown up", 1, 0, 6);

            animationframes[2] = new Texture2D[5];
            animationframes[3] = new Texture2D[5];
            ProcessFrames(ref animationframes, folder + "Dough death- guillotined gold", 2, 0, 5);
            ProcessFrames(ref animationframes, folder + "Dough death- guillotined", 3, 0, 5);

            animationframes[4] = new Texture2D[6];
            animationframes[5] = new Texture2D[6];
            ProcessFrames(ref animationframes, folder + "Dough death- timed out gold", 4, 0, 6, new IntVector2(2, 2), 70);
            ProcessFrames(ref animationframes, folder + "Dough death- timed out", 5, 0, 6, new IntVector2(2, 2), 70);

            animationframes[6] = new Texture2D[7];
            animationframes[7] = new Texture2D[8];
            animationframes[8] = new Texture2D[10];
            ProcessFrames(ref animationframes, folder + "doughbomb boom (animation strip)", 6, 0, 7);
            ProcessFrames(ref animationframes, folder + "kris mine-boom (animation strip)", 7, 0, 8);
            ProcessFrames(ref animationframes, folder + "kris wall 2-exploding (animation strip)", 8, 0, 10);

            animationframes[9] = new Texture2D[5];
            ProcessFrames(ref animationframes, "spring board in action", 9, 0, 5);

            folder = "guillotine\\Guillotine ";

            animationframes[10] = new Texture2D[11];
            animationframes[11] = new Texture2D[11];
            ProcessFrames(ref animationframes, folder + "left transparent", 10, 0, 11, new IntVector2(1, 1), new IntVector2(0, 131), new IntVector2(64, 128));
            ProcessFrames(ref animationframes, folder + "right transparent", 11, 0, 11, new IntVector2(1, 1), new IntVector2(0, 131), new IntVector2(64, 128));
            animationframes[12] = new Texture2D[6];
            animationframes[13] = new Texture2D[4];
            animationframes[14] = new Texture2D[5];
            animationframes[15] = new Texture2D[5];
            ProcessFrames(ref animationframes, folder + "(boom)", 12, 0, 6, new IntVector2(1, 1), new IntVector2(68, 0), new IntVector2(64, 128));
            ProcessFrames(ref animationframes, folder + "(disarm)", 13, 0, 4, new IntVector2(1, 1), new IntVector2(68, 0), new IntVector2(64, 128));
            ProcessFrames(ref animationframes, folder + "(spent) disarm", 14, 0, 5, new IntVector2(1, 1), new IntVector2(68, 0), new IntVector2(64, 128));
            ProcessFrames(ref animationframes, folder + "(boom) 2", 15, 0, 5, new IntVector2(1, 1), new IntVector2(68, 0), new IntVector2(64, 128));

            animationframes[16] = new Texture2D[9];
            animationframes[17] = new Texture2D[5];
            ProcessFrames(ref animationframes, "Bunker battle damage\\bunker \'splosion", 16, 0, 9, new IntVector2(1, 1), new IntVector2(68, 0), new IntVector2(64, 128));
            ProcessFrames(ref animationframes, "Bunker battle damage\\kris mine-boom (animation strip) surrounding explosion", 17, 0, 5);

            animationframes[18] = new Texture2D[6];
            ProcessFrames(ref animationframes, "kris mine (disarmed animation)", 18, 0, 6, new IntVector2(1, 1), 68);

            //Doughboy deaths.
            //First, pits.
            folder = "explosions\\";
            animationframes[19] = new Texture2D[5];
            animationframes[20] = new Texture2D[5];
            animationframes[21] = new Texture2D[5];
            animationframes[22] = new Texture2D[5];
            animationframes[23] = new Texture2D[5];
            ProcessFrames(ref animationframes, folder + "pit death (animation strip)", 19, 0, 5);
            ProcessFrames(ref animationframes, folder + "pit death 2 (animation strip)", 20, 0, 5);
            ProcessFrames(ref animationframes, folder + "pit death 3 (animation strip)", 21, 0, 5);
            ProcessFrames(ref animationframes, folder + "pit death 4 (animation strip)", 22, 0, 5);
            ProcessFrames(ref animationframes, folder + "pit death 5 (animation strip)", 23, 0, 5);
            animationframes[24] = new Texture2D[5];
            animationframes[25] = new Texture2D[5];
            animationframes[26] = new Texture2D[5];
            ProcessFrames(ref animationframes, folder + "pit-trap (animation strip)", 24, 0, 5, IntVector2.One, 66);
            ProcessFrames(ref animationframes, folder + "pit-trap (animation strip) disarmer", 25, 0, 5, IntVector2.One, 66);
            ProcessFrames(ref animationframes, folder + "pit-trap (animation strip) doughbomb", 26, 0, 5, IntVector2.One, 66);

            animationframes[27] = new Texture2D[16];
            ProcessFrames(ref animationframes, "a arrows animation strip", 27, 0, 16, IntVector2.One, 48 * IntVector2.UnitY, new IntVector2(80, 44));

            folder = "Bunker battle damage\\";
            bunkerimages = new Texture2D[2][];
            for (int s = 0; s < 2; s++) {
                bunkerimages[s] = new Texture2D[6];
                for (int i = 0; i < 5; i++) {
                    bunkerimages[s][i] = Content.Load<Texture2D>(folder + "bunker " + (s + 1).ToString() + "-" + i.ToString());
                }
                bunkerimages[s][5] = Content.Load<Texture2D>(folder + "bunker dead+bg " + (s + 1).ToString());
            }

            folder = "doughboy jumps\\mid air ";
            doughboyjumps = new Texture2D[4][];
            string[] doughboynames = new string[] { "doughboy", "doughbomb", "drillboy", "disarmer" };
            string[] directions = new string[] { "NORTH", "WEST", "SOUTH", "EAST" };
            for (int d = 0; d < doughboynames.Length; d++) {
                doughboyjumps[d] = new Texture2D[4];
                for (int i = 0; i < 4; i++)
                    doughboyjumps[d][i] = Content.Load<Texture2D>(folder + doughboynames[d] + "- " + directions[i]);
            }



        }

        public void ProcessFrames(ref Texture2D[][] frames, string stripName, int set, int frameStart, int numFrames) {
            Texture2D strip = Content.Load<Texture2D>(stripName);
            for (int i = 0; i < numFrames; i++) {
                frames[set][frameStart + i] = new Texture2D(Game1.graphics.GraphicsDevice, 64, 64);
                Color[] pixels = new Color[64 * 64];
                strip.GetData<Color>(0, new Rectangle(1, 67 * i, 64, 64), pixels, 0, 64 * 64);

                //Due to Kris actually making the animation frames legible (darn!) we have to set the top and left columns to Transparent
                for (int j = 0; j < 64; j++) {
                    pixels[j] = Color.Transparent;
                }
                for (int j = 0; j < 64; j++) {
                    pixels[64 * j] = Color.Transparent;
                }
                frames[set][frameStart + i].SetData<Color>(pixels);
            }
        }

        public void ProcessFrames(ref Texture2D[][] frames, string stripName, int set, int frameStart, int numFrames, IntVector2 frameOffset, int dy) {
            Texture2D strip = Content.Load<Texture2D>(stripName);
            for (int i = 0; i < numFrames; i++) {
                frames[set][frameStart + i] = new Texture2D(Game1.graphics.GraphicsDevice, 64, 64);
                Color[] pixels = new Color[64 * 64];
                strip.GetData<Color>(0, new Rectangle(frameOffset.X, frameOffset.Y + dy * i, 64, 64), pixels, 0, 64 * 64);

                frames[set][frameStart + i].SetData<Color>(pixels);
            }
        }

        public void ProcessFrames(ref Texture2D[][] frames, string stripName, int set, int frameStart, int numFrames, IntVector2 frameOffset, IntVector2 dFrame, IntVector2 frameSize) {
            Texture2D strip = Content.Load<Texture2D>(stripName);
            IntVector2 pos = frameOffset;
            int pxls = frameSize.X * frameSize.Y;
            for (int i = 0; i < numFrames; i++) {
                frames[set][frameStart + i] = new Texture2D(Game1.graphics.GraphicsDevice, frameSize.X, frameSize.Y);
                Color[] pixels = new Color[pxls];
                strip.GetData<Color>(0, new Rectangle(pos.X, pos.Y, frameSize.X, frameSize.Y), pixels, 0, pxls);

                frames[set][frameStart + i].SetData<Color>(pixels);
                pos = pos + dFrame;
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            // Allows the game to exit
            input.Update(IsActive);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            if (input.KeyJustPressed(Keys.Escape)) {
                if (whoseTurn < 0) {
                    this.Exit();//ESCAPE!
                } else {
                    Reinitialize();
                    whoseTurn = -2; //back to level select screen
                }
            }

            if (input.KeyJustPressed(Keys.OemTilde)) {
                this.Reinitialize();
                return;
            }

            if (endTime > -0.5f) {
                endTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (endTime > 2.0f) {
                    Reinitialize();
                }
            }
            numFrames++;

            IntVector2 mousePos = getMouseArrayPos();
            if (whoseTurn % 3 != 2 && whoseTurn>=0) {
                pm.Update(gameTime);
                breadbox.Update(gameTime);
            }
            hammieham.Update();
            texter.Update(gameTime);
            //texter.transmissions.Add("PW2");
            switch (whoseTurn) {
                case 0:
                    break;
                case 1:
                    if (CheckDoughboyRange())
                        breadbox.UpdateDoughboy(currentDoughboy, gameTime);
                    break;
                case 2:
                    UpdateCow(gameTime);
                    break;
                case 3:
                    break;
                case 4:
                    if (CheckDoughboyRange())
                        breadbox.UpdateDoughboy(currentDoughboy, gameTime);
                    break;
                case 5:
                    UpdateCow(gameTime);
                    break;

            }

            //Manage mine chain reactions
            for (int i = 0; i < animations.Count; i++) {
                if (animations[i].isOnGrid && (animations[i].animationID >= 6 && animations[i].animationID <= 8 || animations[i].animationID == 16)) {
                    int x = animations[i].pos.X;
                    int y = animations[i].pos.Y;
                    int mx = x / 64;
                    int my = y / 64;
                    if (board[my, mx] == 5) {
                        hammieham.addCue("splosion2");
                        if (GetAnimationType(mx + 1, my) == -1 || true) //what did I put this in for? I don't remember, and it doesn't seem necessary...
                        {
                            animations.Add(new Animation(16, new IntVector2(x + 64, y), false, 0));
                        }
                        if (GetAnimationType(mx - 1, my) == -1 || true) {
                            animations.Add(new Animation(16, new IntVector2(x - 64, y), false, 0));
                        }
                        if (GetAnimationType(mx, my + 1) == -1 || true) {
                            animations.Add(new Animation(16, new IntVector2(x, y + 64), false, 0));
                        }
                        if (GetAnimationType(mx, my - 1) == -1 || true) {
                            animations.Add(new Animation(16, new IntVector2(x, y - 64), false, 0));
                        }
                        board[my, mx] = 0;
                    } else if (board[my, mx] == 1) //if it is a wall
                    {
                        board[my, mx] = 4; //...blow it up
                        //(and turn it into rubble)
                    } else if (board[my, mx] == 6) //if it is a springboard
                    {
                        board[my, mx] = 0;//...blow it up.
                    } else {
                        if (board[my, mx] == 7) //if it is a guillotine
                    {
                            board[my, mx] = 0;//...blow it up.
                            //but first, change the animation:
                            IntVector2 animpos=new IntVector2(x, y - 64);
                            animations.RemoveAt(i);
                            animations.Add(new Animation(12, animpos, true, 2));
                            for (int j = animations.Count-1; j >= 0; j--) {
                                if ((animations[j].animationID==10 || animations[j].animationID==11) && animations[j].pos == animpos) {
                                    animations.RemoveAt(j);
                                }
                            }
                        } else if (board[my, mx] == 8) //if it is a guillotine, already used
                    {
                            board[my, mx] = 0;//...blow it up.
                            //but first, change the animation:
                            IntVector2 animpos = new IntVector2(x, y - 64);
                            animations.RemoveAt(i);
                            animations.Add(new Animation(15, animpos, true, 2));
                            for (int j = animations.Count-1; j >= 0; j--) {
                                if ((animations[j].animationID == 10 || animations[j].animationID == 11) && animations[j].pos == animpos) {
                                    animations.RemoveAt(j);
                                }
                            }
                        }

                    }
                }
            }

            //Manage animations
            for (int i = animations.Count - 1; i >= 0; i--) {
                animations[i].Update(gameTime);
                if (animations[i].isDone)
                    animations.RemoveAt(i);
            }

            //manage particles
            for (int i = particles.Count - 1; i >= 0; i--) {
                particles[i].Update(gameTime);
                if (particles[i].removeplz)
                    particles.RemoveAt(i);
            }

            //manage music
            if (whoseTurn>=0 && input.mouseState.LeftButton == ButtonState.Pressed && input.lastMouseState.LeftButton == ButtonState.Released) {
                Vector2 musicpos = new Vector2(screenSize.X - 64, pm.pos.Y + 64 + 32 + 6);
                Vector2 soundpos = new Vector2(screenSize.X - 32, pm.pos.Y + 64 + 32 + 6);
                if (IsInRange(input.mousePosition, musicpos, musicpos + 32 * Vector2.One))
                    hammieham.ToggleBackgroundMusic(true);
                if (IsInRange(input.mousePosition, soundpos, soundpos + 32 * Vector2.One))
                    hammieham.ToggleSoundFX(true);
            }

            tutorial.Update(gameTime);

            base.Update(gameTime);
        }

        static void UpdateCow(GameTime gameTime) {
            if (currentCow == null) {
                int r = rand.Next(4); //choose corner
                int r2 = rand.Next(2);//choose edge
                int r3 = rand.Next(2) + 1;//choose place
                Vector2 cpos = new Vector2(r3, 0);
                if (r2 == 1) cpos = new Vector2(cpos.Y, cpos.X);
                cpos.X = (r < 2) ? cpos.X : 13 - cpos.X;
                cpos.Y = (r % 2 == 0) ? cpos.Y : 11 - cpos.Y;

                Vector2 npos = cpos;
                if (cpos.X < 0.1) npos = new Vector2(cpos.X - 1, cpos.Y);
                if (cpos.X > 12.9) npos = new Vector2(cpos.X + 1, cpos.Y);
                if (cpos.Y < 0.1) npos = new Vector2(cpos.X, cpos.Y - 1);
                if (cpos.Y > 10.9) npos = new Vector2(cpos.X, cpos.Y + 1);
                cpos *= 64;
                npos *= 64;
                currentCow = new Cow(npos, cpos);
                currentCow.BeginMoving(gameTime);
            } else {
                currentCow.Update(gameTime);
            }
        }

        public static int GetAnimationType(int arPosX, int arPosY) {
            int resizedX = 64 * arPosX;
            int resizedY = 64 * arPosY;
            foreach (Animation anim in animations) {
                if (anim.pos.X == resizedX && anim.pos.Y == resizedY)
                    return anim.animationID;
            }
            return -1;
        }

        public static bool AnimationTypeAtQ(int arPosX, int arPosY, int type) {
            int resizedX = 64 * arPosX;
            int resizedY = 64 * arPosY;
            foreach (Animation anim in animations) {
                if (anim.pos.X == resizedX && anim.pos.Y == resizedY)
                    if (anim.animationID == type)
                        return true;
            }
            return false;
        }

        static bool CheckDoughboyRange() {
            if (currentDoughboy >= breadbox.doughboys.Count - 1) {
                if (whoseTurn >= 0) {
                    currentDoughboy = -1;
                    whoseTurn++;
                    if (currentCow != null)
                        currentCow.HasMoved = false;
                }
                return false;
            }
            return currentDoughboy >= 0;
        }

        static IntVector2 getMouseArrayPos() {
            IntVector2 result = new IntVector2(input.mousePosition / 64);
            return result;
        }

        static bool IsInRange(Vector2 place, Vector2 lo, Vector2 hi) {
            return (lo.X <= place.X) && (place.X < hi.X) && (lo.Y <= place.Y) && (place.Y < hi.Y);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {

            drawbuffer = new RenderTarget2D(GraphicsDevice, screenSize.X, screenSize.Y);
            drawbuffer.Name = "Main screen buffer";
            GraphicsDevice.SetRenderTarget(drawbuffer);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw the screen!
            spriteBatch.Begin();


            spriteBatch.Draw(images["field"], Vector2.Zero, Color.White);



            for (int y = 0; y < 12; y++)
                for (int x = 0; x < 14; x++) {
                    switch (board[y, x]) {
                        case 0:
                            break;
                        case 1:
                            spriteBatch.Draw(images["wall"], new Vector2(64 * x, 64 * y), Color.White);
                            break;
                        case 2:
                            break;
                        case 3:
                                bool contleft = (board[y, x - 1] == 3);
                                bool contright = (board[y, x + 1] == 3);
                                if (contleft && contright)
                                    spriteBatch.Draw(images["wire"], new Vector2(64 * x, 64 * y), Color.White);
                                else if (!contleft && contright)
                                    spriteBatch.Draw(images["wire-left"], new Vector2(64 * x, 64 * y), Color.White);
                                else if (contleft && !contright)
                                    spriteBatch.Draw(images["wire-right"], new Vector2(64 * x, 64 * y), Color.White);
                                else
                                    spriteBatch.Draw(images["wire-both"], new Vector2(64 * x, 64 * y), Color.White);
                            break;
                        case 4:
                            spriteBatch.Draw(images["brokenwall"], new Vector2(64 * x, 64 * y), Color.White);
                            break;
                        case 5:
                            if (!AnimationTypeAtQ(x, y, 18)) {
                                if ((vars[y, x] & 1) == 1) {
                                    spriteBatch.Draw(animationframes[18][5], new Vector2(64 * x, 64 * y), Color.White);
                                } else {
                                    int whoseMine = (vars[y, x] >> 4) & 15;
                                    if (whoseMine == whoseTurn / 3 || whoseMine == 15 || showEnemyMines) {
                                        spriteBatch.Draw(images["mine"], new Vector2(64 * x, 64 * y), Color.White);
                                    }
                                }
                            }
                            break;
                        case 6:
                            if (!AnimationTypeAtQ(x, y, 9))
                                spriteBatch.Draw(images["spring"], new Vector2(64 * x, 64 * y), Color.White);
                            break;
                        case 7:
                            if (!(AnimationTypeAtQ(x, y - 1, 10) || AnimationTypeAtQ(x, y - 1, 13) || AnimationTypeAtQ(x, y - 1, 14))) {
                                int v = DoughboyIsBehind(y);
                                if ((vars[y, x] & 1) == 1) {
                                    if (v <= 0) spriteBatch.Draw(animationframes[13][3], new Vector2(64 * x, 64 * y - 64), Color.White);
                                } else {
                                    if (v <= 0) spriteBatch.Draw(animationframes[10][0], new Vector2(64 * x, 64 * y - 64), Color.White);//note that the square is moved up
                                    if (v == -1) spriteBatch.Draw(animationframes[11][0], new Vector2(64 * x, 64 * y - 64), Color.White);
                                }
                            }
                            break;
                        case 8:
                            if (!(AnimationTypeAtQ(x, y - 1, 10) || AnimationTypeAtQ(x, y - 1, 13) || AnimationTypeAtQ(x, y - 1, 14))) {
                                int v = DoughboyIsBehind(y);
                                if ((vars[y, x] & 1) == 1) {
                                    if (v <= 0) spriteBatch.Draw(animationframes[14][3], new Vector2(64 * x, 64 * y - 64), Color.White);
                                } else {
                                    if (v <= 0) spriteBatch.Draw(animationframes[10][10], new Vector2(64 * x, 64 * y - 64), Color.White);
                                    if (v == -1) spriteBatch.Draw(animationframes[11][10], new Vector2(64 * x, 64 * y - 64), Color.White);
                                }
                            }
                            break;
                        case 9:
                            spriteBatch.Draw(images["pit"], new Vector2(64 * x, 64 * y), Color.White);
                            break;
                        case 10:
                            spriteBatch.Draw(images["filled pit"], new Vector2(64 * x, 64 * y), Color.White);
                            break;
                        case 11:
                            spriteBatch.Draw(images["filled pit 2"], new Vector2(64 * x, 64 * y), Color.White);
                            break;
                        case 12:
                            spriteBatch.Draw(images["filled pit 3"], new Vector2(64 * x, 64 * y), Color.White);
                            break;
                        case 32:
                            int state=(3 - (vars[y, x] >> 8));
                            
                            if (state == 3) {
                                double timeSinceStart = (gameTime.TotalGameTime.TotalSeconds - turnStart)/1.5f;
                                int intensity = (int)(256 * Math.Pow(1 - timeSinceStart, 2.2f));
                                if (timeSinceStart <1) {
                                    spriteBatch.Draw(images["cow bait 3"], new Vector2(64 * x, 64 * y), Color.FromNonPremultiplied(255, 255, 255, intensity));
                                } else {
                                    Game1.board[y, x] = 0;
                                    Game1.vars[y, x] = 0;
                                }
                            } else if (state >= 0 && state < 3) {
                                spriteBatch.Draw(images["cow bait " + state], new Vector2(64 * x, 64 * y), Color.White);
                            } else {
                                Console.WriteLine("WTF?");
                                throw new ArgumentException("A cow bait was created with " + state + " rounds to go. This should never happen.");
                            }
                            break;
                    }
                }

            //Draw animations furthest back
            for (int i = animations.Count - 1; i >= 0; i--) {
                if (!animations[i].isDone && animations[i].layer == -1)
                    animations[i].Draw();
            }

            if (currentCow != null) {
                currentCow.Draw();
            }



            //Draw bunkers
            players[0].DrawBunker(255);
            players[1].DrawBunker(255);


            switch (whoseTurn) {
                case -1:
                    if (fauxboy != null)
                        fauxboy.Draw();
                    break;
                case 0:
                    break;
                case 1:
                    if (CheckDoughboyRange())
                        breadbox.DrawDoughboy(currentDoughboy, gameTime);
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    if (CheckDoughboyRange())
                        breadbox.DrawDoughboy(currentDoughboy, gameTime);
                    break;
                case 5:
                    break;
            }


            //Draw animations in back
            for (int i = animations.Count - 1; i >= 0; i--) {
                if (!animations[i].isDone && animations[i].layer == 0)
                    animations[i].Draw();
            }

            //Draw guillotines and their animations
            for (int y = 0; y < Game1.board.GetLength(0); y++)
                for (int x = 0; x < Game1.board.GetLength(1); x++) {
                    switch (Game1.board[y, x]) {
                        case 7:
                            if (!(AnimationTypeAtQ(x, y - 1, 10) || AnimationTypeAtQ(x, y - 1, 13) || AnimationTypeAtQ(x, y - 1, 14))) {
                                int v = DoughboyIsBehind(y);
                                if ((vars[y, x] & 1) == 1) {
                                    if (v == 1) spriteBatch.Draw(animationframes[13][3], new Vector2(64 * x, 64 * y - 64), Color.White);
                                } else {
                                    if (v == 1) spriteBatch.Draw(animationframes[10][0], new Vector2(64 * x, 64 * y - 64), Color.White);//note that the square is moved up
                                    if (v >= 0) spriteBatch.Draw(animationframes[11][0], new Vector2(64 * x, 64 * y - 64), Color.White);

                                }
                            }
                            break;
                        case 8:
                            if (!(AnimationTypeAtQ(x, y - 1, 10) || AnimationTypeAtQ(x, y - 1, 13) || AnimationTypeAtQ(x, y - 1, 14))) {
                                int v = DoughboyIsBehind(y);
                                if ((vars[y, x] & 1) == 1) {
                                    if (v == 1) spriteBatch.Draw(animationframes[14][3], new Vector2(64 * x, 64 * y - 64), Color.White);
                                } else {
                                    if (v == 1) spriteBatch.Draw(animationframes[10][10], new Vector2(64 * x, 64 * y - 64), Color.White);//note that the square is moved up
                                    if (v >= 0) spriteBatch.Draw(animationframes[11][10], new Vector2(64 * x, 64 * y - 64), Color.White);
                                }
                            }

                            break;
                    }
                    foreach (Animation anim in animations)
                        if (anim.layer == 2 && anim.isOnGrid && anim.pos.X / 64 == x && anim.pos.Y / 64 == y - 1)
                            anim.Draw();
                }



            //Draw bunkers, slightly transparent
            players[0].DrawBunker(200);
            players[1].DrawBunker(200);

            //Draw animations on front
            for (int i = animations.Count - 1; i >= 0; i--) {
                if (!animations[i].isDone && animations[i].layer == 1)
                    animations[i].Draw();
            }

            //Draw particles
            for (int i = particles.Count - 1; i >= 0; i--) {
                if (!particles[i].removeplz)
                    particles[i].Draw();
            }

            //Draw UI components if we're not in attract mode
            if (whoseTurn >= 0) {
                tutorial.Draw(gameTime);
                breadbox.Draw();
                pm.Draw();
                texter.Draw();

                spriteBatch.Draw(musicopts[0][hammieham.isPlayingBackground ? 1 : 0], new Vector2(screenSize.X - 64, pm.pos.Y + 64 + 32 + 6), Color.White);
                spriteBatch.Draw(musicopts[1][hammieham.isPlayingSounds ? 1 : 0], new Vector2(screenSize.X - 32, pm.pos.Y + 64 + 32 + 6), Color.White);
            }

            


            spriteBatch.End();

            if (whoseTurn < 0) {
                switch (whoseTurn) {
                    case -1:
                        DrawOpening(gameTime);
                        break;
                    case -2:
                        DrawLevelSelect(gameTime);
                        break;
                    case -3:
                        mnuOptions.Update(gameTime);
                        mnuOptions.Draw(gameTime);
                        break;
                }
            }

            spriteBatch.Begin();

            //Draw the things which must always be drawn
            spriteBatch.Draw(images["cursor"], input.mousePosition, Color.White);
            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null); //To the backbuffer!
            GraphicsDevice.Clear(Color.Black);
            //spriteBatch.Begin();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
            IntVector2 actualSize = new IntVector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            float scale = 1.0f;
            if (actualSize.X < screenSize.X || actualSize.Y < screenSize.Y) {
                scale = 0.5f;
            }

            spriteBatch.Draw((Texture2D)drawbuffer, new Rectangle((int)(actualSize.X - scale * screenSize.X) / 2, (int)(actualSize.Y - scale * screenSize.Y) / 2, (int)(scale * screenSize.X), (int)(scale * screenSize.Y)),Color.White);

            //Debug information
            if (false) {
                spriteBatch.DrawString(fonts["04-12"], "Scale: " + scale.ToString() + "  Render size: " + screenSize.ToString() + " Screen size: " + actualSize.ToString()
                    , Vector2.One * 16, Color.White);
            }
            if (input.IsKeyPressed(Keys.F1)) { //Path visualization for AIs.
                //AIFrame path = AIRoutines.IsSquareAccessible(new IntVector2(12, 5), 5000, DoughboyType.Regular, 0);
                DateTime dt=DateTime.Now;
                AIFrame path = AIRoutines.ComputeRoutes(new IntVector2(1, 5), DoughboyType.Regular, 0, true)[5, 12];
                Console.WriteLine(DateTime.Now.Subtract(dt).TotalMilliseconds + "ms");
                if(path==null){
                    spriteBatch.DrawString(fonts["04-18"],"NO",Vector2.One*16,Color.White);
                    
                }else{
                    spriteBatch.DrawString(fonts["04-18"],"YES",Vector2.One*16,Color.White);
                    for (int i = 0; i < path.prevPositions.Length - 1;i++ ) {
                        IntVector2 tp = 32*path.prevPositions[i]+IntVector2.One*16;
                        IntVector2 np = 32*path.prevPositions[i + 1]+IntVector2.One*16;
                        float th = (float)Math.Atan2(np.Y - tp.Y, np.X - tp.X);
                        float len=(float)((np-tp).ToVector2().Length());
                        spriteBatch.Draw(pixeltex, new Rectangle(tp.X, tp.Y, (int)len, 1), null, Color.White, th, Vector2.Zero, SpriteEffects.None, 0);
                    }
                }
            }
            if (Game1.input.IsKeyPressed(Keys.PrintScreen)) {
                DateTime time = DateTime.Now;
                String filename = string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}.png", DateTime.Now);
                drawbuffer.SaveAsPng(File.OpenWrite(filename), drawbuffer.Width, drawbuffer.Height);
                spriteBatch.DrawString(fonts["04-12"], "Saved screenshot as " + filename,Vector2.One*16,Color.White);
            }

            spriteBatch.End();

            if (Game1.input.IsKeyPressed(Keys.F8) && videoMode) {
                //copy the frame
                vr.AddCopiedFrame((Texture2D)drawbuffer);
            }

            drawbuffer.Dispose();
            base.Draw(gameTime);
        }

        /// <summary>
        /// Checks to see if a doughboy or a cow is behind the object
        /// </summary>
        /// <param name="ary"></param>
        /// <returns>1 if it is behind, 0 if in layer, -1 if in front</returns>
        public int DoughboyIsBehind(int ary) {
            if (CheckDoughboyRange()) {
                Doughboy doughboy = breadbox.doughboys[breadbox.GetArrayIndex(currentDoughboy)];
                int ty = (int)Math.Round((doughboy.screenPos.Y) / 64);
                ty = Math.Min(ty, currentCow.arrayPos.Y);
                if (ty == ary) {
                    return 0;
                }
                if (ty < ary) {
                    return 1;
                }
            }
            //...return false, I guess?
            //there's no doughboy, so it shouldn't really matter (so long as it's consistent)
            return -1;
        }

        public int mod2(int a, int m) {
            int t = a % m;
            if (t < 0) t += m;
            return t;
        }

        public float mod2(float a, int m) {
            float t = a % m;
            if (t < 0) t += m;
            return t;
        }

        public void DrawCredits(GameTime gameTime, int alpha, Vector2 offset) {
            //Game1.spriteBatch.Draw(pixeltex, new Rectangle(0, 0, screenSize.X, screenSize.Y), Color.FromNonPremultiplied(0, 0, 0, alpha));
            Game1.spriteBatch.Draw(images["credits"], offset, Color.White);
        }

        public void DrawLevelSelect(GameTime gameTime) {
            double t = gameTime.TotalGameTime.TotalSeconds;
            spriteBatch.Begin();
            DrawScreenLines(gameTime, Vector2.Zero);
            Vector2 oset = new Vector2(0, 244);
            //manage click times

            for (int i = clickTimes.Count - 1; i >= 0; i--) {
                double t2 = t - Math.Abs(clickTimes[i]);
                if (t2 < 1) {
                    oset.X += BaseRoutines.Hermite(0, 0, -Math.Sign(clickTimes[i]) * 224, 0, (float)t2);
                } else {

                    clickTimes.RemoveAt(i);
                }
            }

            oset.X = mod2(oset.X, 224);

            //Update position
            if (oset.X > lastOset && lastOset + 224 - oset.X < 112) {
                lvlSelectIndex++;
            }
            if (oset.X < lastOset && oset.X + 224 - lastOset < 112) {
                lvlSelectIndex--;
            }

            oset.X -= 224 / 2;

            //pseudorasterize
            oset.X = (int)oset.X;

            for (int i = -1; i < 6; i++) {
                DrawMiniLevel(new Vector2(i * 224 + 32, 24) + oset, mod2((i + lvlSelectIndex), boards.GetLength(0)));
                spriteBatch.Draw(images["map cell"], new Vector2(i * 224, 0) + oset, Color.White);

            }

            spriteBatch.Draw(images["map frame"], new Vector2(0, 216), Color.White);

            Vector2 larpos = new Vector2(22, 294);
            Vector2 rarpos = new Vector2(screenSize.X - 22 - 56, 294);
            spriteBatch.Draw(images["left arrow"], larpos, Color.White);
            spriteBatch.Draw(images["right arrow"], rarpos, Color.White);

            bool arrowClicked = false;
            if (input.mouseState.LeftButton == ButtonState.Pressed && input.lastMouseState.LeftButton == ButtonState.Released) {
                if (IsInRange(input.mousePosition, larpos, larpos + new Vector2(56, 84))) {
                    if (IsInTexture(images["left arrow"], input.mousePosition - larpos)) {
                        clickTimes.Add(-gameTime.TotalGameTime.TotalSeconds);
                        arrowClicked = true;
                    }
                }
                if (IsInRange(input.mousePosition, rarpos, rarpos + new Vector2(56, 84))) {
                    if (IsInTexture(images["right arrow"], input.mousePosition - rarpos)) {
                        //lvlSelectIndex++;
                        clickTimes.Add(gameTime.TotalGameTime.TotalSeconds);
                        arrowClicked = true;
                    }
                }
            }

            btnOptions.pos = new Vector2(Game1.screenSize.X/2-btnOptions.tex.Width - 32, Game1.screenSize.Y - btnOptions.tex.Height - 16);
            btnOptions.Update();
            btnOptions.Draw();
            if (btnOptions.IsJustClicked) {
                //Go to options menu
                whoseTurn = -3;
                mnuOptions.parentScreen = -2;
            }

            btnBack2.pos = new Vector2(Game1.screenSize.X / 2 + 32, Game1.screenSize.Y - btnOptions.tex.Height - 16); //no, btnOptions isn't a typo. It's to make the text aligned vertically.
            btnBack2.Update();
            btnBack2.Draw();
            if (btnBack2.IsJustClicked) {
                //go back, obviously
                whoseTurn = -1;
            }
            
            //draw map name place
            spriteBatch.Draw(images["map header"], new Vector2(342, 142), Color.White);
            int boardIndex = mod2(lvlSelectIndex + 1 + ((oset.X + 224 / 2) / 224 > 0.501 ? 0 : 1), boardNames.Length);
            string name = boardNames[boardIndex];
            Vector2 strsize = fonts["cost"].MeasureString(name);
            spriteBatch.DrawString(fonts["cost"], name, BaseRoutines.trunc(new Vector2(362 + (176 - strsize.X) / 2, 170 + (20 - strsize.Y) / 2), 2) + Vector2.UnitX, Color.White);

            //load game
            if (!arrowClicked && input.mouseState.LeftButton == ButtonState.Pressed && input.lastMouseState.LeftButton == ButtonState.Released) {
                for (int i = 0; i <= 5; i++) {
                    if (IsInRange(input.mousePosition, new Vector2(i * 224+39, 26) + oset, new Vector2(i * 224 + 184, 150) + oset)) {
                        BaseRoutines.Copy2DArray(boards[mod2(boardIndex + i - 2, boards.Length)], out board);
                        BeginGame();
                    }
                }
            }
            //BeginGame();
            spriteBatch.End();
            lastOset = oset.X + 224 / 2; //addition is to counteract oset.X -= 224/2 above
        }

        public void DrawMiniLevel(Vector2 pos, int i) {
            //float scale = 0.25f;
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 10; x++) {
                    if ((x + y) % 2 == 0) {
                        spriteBatch.Draw(miniimages["grass 1"], pos + new Vector2(16 * x, 16 * y), Color.White);
                    } else {
                        spriteBatch.Draw(miniimages["grass 2"], pos + new Vector2(16 * x, 16 * y), Color.White);
                    }
                    switch (boards[i][y + 2, x + 2]) {
                        case 1:
                            spriteBatch.Draw(miniimages["wall"], pos + new Vector2(16 * x, 16 * y), Color.White);
                            break;
                        case 3:
                            spriteBatch.Draw(miniimages["wire"], pos + new Vector2(16 * x, 16 * y), Color.White);
                            break;
                        case 4:
                            spriteBatch.Draw(miniimages["brokenwall"], pos + new Vector2(16 * x, 16 * y), Color.White);
                            break;
                        case 5:
                            spriteBatch.Draw(miniimages["mine"], pos + new Vector2(16 * x, 16 * y), Color.White);
                            break;
                        case 6:
                            spriteBatch.Draw(miniimages["spring"], pos + new Vector2(16 * x, 16 * y), Color.White);
                            break;
                        case 7:
                            spriteBatch.Draw(miniimages["guillotine"], pos + new Vector2(16 * x, 16 * (y - 1)), Color.White);
                            break;
                        case 9:
                            spriteBatch.Draw(miniimages["pit"], pos + new Vector2(16 * x, 16 * y), Color.White);
                            break;
                    }
                }
            }
        }

        public bool IsInTexture(Texture2D tex, Vector2 texPos) {
            Color[] colors = new Color[1];
            if (texPos.X < 0 || texPos.Y < 0 || texPos.X >= tex.Width || texPos.Y >= tex.Height) {
                return false;
            }
            tex.GetData<Color>(0, new Rectangle((int)texPos.X, (int)texPos.Y, 1, 1), colors, 0, 1);
            return colors[0].A > 128;
        }

        public Color GetPixelFromTexture(Texture2D tex, Vector2 texPos) {
            Color[] colors = new Color[1];
            if (texPos.X < 0 || texPos.Y < 0 || texPos.X >= tex.Width || texPos.Y >= tex.Height) {
                return Color.Transparent;
            }
            tex.GetData<Color>(0, new Rectangle((int)texPos.X, (int)texPos.Y, 1, 1), colors, 0, 1);
            return colors[0];
        }

        /// <summary>
        /// The only purpose of this is to draw the opening screen.
        /// </summary>
        /// <param name="gameTime"></param>
        public void DrawOpening(GameTime gameTime) {
            spriteBatch.Begin();
            //spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
            GraphicsDevice.PresentationParameters.MultiSampleCount = 0;

            Texture2D tex = new Texture2D(graphics.GraphicsDevice, screenSize.X / 2, screenSize.Y / 2);
            tex.Name = "temporary draw buffer";
            double t = gameTime.TotalGameTime.TotalSeconds;
            double e = gameTime.ElapsedGameTime.TotalSeconds;
            int size = screenSize.X * screenSize.Y / 4;

            Vector2 pos;
            switch (whichAnimation) {
                case 0:
                    if (t - animTime < 1) {
                        pos = Vector2.Hermite(Vector2.Zero, -128 * Vector2.UnitX, -screenSize.X * Vector2.UnitX, Vector2.Zero, (float)(t - animTime));
                    } else {
                        pos = -screenSize.X * Vector2.UnitX;
                    }
                    break;
                case 1:
                    if (t - animTime < 1) {
                        pos = Vector2.Hermite(-screenSize.X * Vector2.UnitX, 128 * Vector2.UnitX, Vector2.Zero, Vector2.Zero, (float)(t - animTime));
                    } else {
                        pos = Vector2.Zero;
                        whichAnimation = 1;
                    }
                    break;
                default:
                    pos = Vector2.Zero;
                    break;
            }



            float zoomLen = 0.55f;
            float zoomTime = 6.85f - zoomLen;




            if (t < 2) {
                int iold = (int)((t - e) * size / 2) % size;
                int inew = (int)(t * size / 2) % size;
                for (int i = iold; i < Math.Min(inew, size); i++) {
                    glitchscreen[i] = Color.FromNonPremultiplied(rand.Next(255), rand.Next(255), rand.Next(255), 255);
                }
            } else if (t < 4) {
                //TODO: make faster and remove errors.
                //Right now it may or may not give the computer a heart attack.
                try {
                    int perline = screenSize.X / 32;

                    double tt = t - 2;
                    int iold = (int)((tt - e) * size / (perline * 32));

                    int inew = (int)(tt * size / (perline * 32));
                    if (iold > inew) return;
                    Color[] randtex = new Color[32 * 32];
                    Dictionary<string, Texture2D>.Enumerator temp = images.GetEnumerator();
                    int r = rand.Next(images.Count - 1);
                    for (int i = 0; i < r; i++) {
                        temp.MoveNext();
                    }

                    temp.Current.Value.GetData<Color>(0, new Rectangle(0, 0, 32, 32), randtex, 0, 32 * 32);
                    for (int i = 0; i < 32 * 32; i++) {
                        randtex[i].A = 255;
                    }

                    for (int i = iold; i < Math.Min(inew, size); i++) {
                        int posx = (i % perline) * 32 / 2;//to get overlapping effect
                        int posy = (i / perline) * 32 / 2;
                        int counter = 0;
                        for (int x = posx; x < posx + 32; x++)
                            for (int y = posy; y < posy + 32; y++) {
                                glitchscreen[x + 32 * perline * y] = randtex[counter];
                                counter++;
                            }

                    }
                } catch (Exception) {
                }
            } else if (t < 5) {
                //just wait
            } else if (t < zoomTime + zoomLen) {
                if (t - e < 4) {
                    for (int i = 0; i < size; i++)
                        glitchscreen[i] = Color.Black;
                }

                if (t > zoomTime) {
                    DrawScreenLines(gameTime, pos);
                    float scale = (float)((t - zoomTime) / zoomLen);
                    float s2 = (float)Math.Pow(scale, 6);
                    int c = (int)(255 * s2);
                    spriteBatch.Draw(images["title"], scale * (new Vector2(0, 64 - screenSize.Y / 2)) + new Vector2((screenSize.X - images["title"].Width * s2) / 2, screenSize.Y / 2),
                        null, Color.FromNonPremultiplied(c, c, c, 255), 0, Vector2.Zero, s2, SpriteEffects.None, 0);
                } else {
                    tex.SetData<Color>(glitchscreen);
                    spriteBatch.Draw(tex, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 2, SpriteEffects.None, 0);

                    spriteBatch.DrawString(fonts["cost"], "INITIAL TESTS INDICATE\n\n    ALL SYSTEMS GO", BaseRoutines.trunc(new Vector2((screenSize.X - 22 * 8) / 2, 64), 2) + Vector2.UnitX, Color.White);
                }
            } else {
                if (t - e < zoomTime + zoomLen + 0.01) {
                    scenarioTime = -15;
                }

                scenarioTime += e;

                /*if((input.mousePosition-input.lastMousePosition).LengthSquared()>36){
                    scenarioTime=-15;
                }*/

                if (scenarioTime < 0) {

                    DrawScreenLines(gameTime, pos);

                    spriteBatch.Draw(images["menu image"], pos + new Vector2(244, 240), Color.White);
                    spriteBatch.Draw(images["title"], pos + new Vector2((screenSize.X - images["title"].Width) / 2, 10), Color.White);


                    //string credits = "Art & Animations: Kris Owen - Music: Sean Mortensen - Programming: Neil Bickford";
                    //spriteBatch.DrawString(fonts["04-12"], credits, pos + new Vector2((screenSize.X - fonts["04-12"].MeasureString(credits).X) / 2, screenSize.Y - 14), Color.White);

                } else {
                    //We cheat and do a bit of updating here as well
                    if (scenarioTime - e < 0.01) {
                        #region tutorial
                        //load scenario
                        if (t - e < 26) {
                            currentScenario = 1; //always show the general one first
                        } else {
                            currentScenario = rand.Next(1, scenarios.Length);
                        }
                        string[] slines = scenarios[currentScenario].Split('\n');
                        string[] tline;
                        int tint;
                        for (int y = 0; y < 8; y++) {
                            tline = slines[y + 1].Split(',');
                            for (int x = 0; x < 10; x++) {
                                if (!int.TryParse(tline[x], out tint))
                                    throw new Exception("Someone's messed with your intro.dboy file! #9");
                                board[y + 2, x + 2] = tint;
                                vars[y + 2, x + 2] = 0x00F0;
                            }
                        }
                        int istart = 0;
                        List<string> fboyblocks = new List<string>();
                        List<string> fboytitles = new List<string>();
                        while (true) {
                            istart = scenarios[currentScenario].IndexOf('\"', istart + 1); //first quote
                            if (istart == -1) break;
                            fboytitles.Add(scenarios[currentScenario].Substring(istart + 1, scenarios[currentScenario].IndexOf('\"', istart + 1) - istart - 1));
                            istart = scenarios[currentScenario].IndexOf('\"', istart + 1);//second quote
                            fboyblocks.Add(scenarios[currentScenario].Substring(istart + 3, scenarios[currentScenario].IndexOf("end", istart) - istart - 5));
                        }
                        fboyprogs = fboyblocks.ToArray();
                        fboytexts = fboytitles.ToArray();
                        fauxboy = new Fauxboy(fboyprogs[0]);
                        currentDoughboy = 0;
                        #endregion
                    }
                    int oldindex = currentDoughboy;
                    Game1.spriteBatch.DrawString(fonts["cost"], fboytexts[currentDoughboy], new Vector2(0.5f * (screenSize.X - 2 * fonts["cost"].MeasureString(fboytexts[currentDoughboy]).X), 32),
                        Color.White, 0, Vector2.Zero, 2.0f, SpriteEffects.None, 0);
                    #region update tutorial
                    fauxboy.Update(gameTime);
                    if (currentDoughboy != oldindex) {
                        if (currentDoughboy >= fboyprogs.Length) {
                            scenarioTime = -15;
                            Game1.players[0].health = 100;
                            Game1.players[1].health = 100;
                        } else {
                            fauxboy = new Fauxboy(fboyprogs[currentDoughboy]);
                        }
                    }
                    #endregion

                }

                if (whichAnimation == 0) {
                    DrawCredits(gameTime, (int)(255 * Math.Min(gameTime.TotalGameTime.TotalSeconds - animTime, 1)), pos + screenSize.X * Vector2.UnitX);
                }
                if (whichAnimation == 1) {
                    DrawCredits(gameTime, (int)(255 - 255 * Math.Min(gameTime.TotalGameTime.TotalSeconds - animTime, 1)), pos + screenSize.X * Vector2.UnitX);
                }
                if (whichAnimation == 0) {
                    scenarioTime = -15;
                    Color tc = GetPixelFromTexture(images["credits map"], input.mousePosition - pos - new Vector2(screenSize.X, 0));
                    if (tc.A > 0) {
                        Game1.spriteBatch.Draw(pixeltex, new Rectangle(0, screenSize.Y - 16, screenSize.X, 16), Color.Black);
                        if (tc.R == 0) {
                            string cred = "Kris Owen: Designer, artist, dynamo, and fiend.";
                            Game1.spriteBatch.DrawString(fonts["04-12"], cred, new Vector2((screenSize.X - fonts["04-12"].MeasureString(cred).X) / 2, screenSize.Y - 14), Color.White);
                        }
                        if (tc.R == 128) {
                            string cred = "Neil Bickford: Programmer, save occasional lapses into designer.";
                            Game1.spriteBatch.DrawString(fonts["04-12"], cred, new Vector2((screenSize.X - fonts["04-12"].MeasureString(cred).X) / 2, screenSize.Y - 14), Color.White);
                        }
                        if (tc.R == 255) {
                            string cred = "Sean Mortensen: Composer magnificent, and dashing stranger.";
                            Game1.spriteBatch.DrawString(fonts["04-12"], cred, new Vector2((screenSize.X - fonts["04-12"].MeasureString(cred).X) / 2, screenSize.Y - 14), Color.White);
                        }
                    }
                }

                float spb = 1f;
                if ((t % spb) < 0.5 * spb) {
                    spriteBatch.Draw(images["title-text"], pos + new Vector2((screenSize.X-images["title-text"].Width)/2, 598), Color.White);
                }

                if (scenarioTime < 0) {

                    btnCredits.offset = pos;
                    btnCredits.Update();
                    btnCredits.Draw();

                    if (btnCredits.IsJustClicked) {
                        whichAnimation = 0;
                        animTime = gameTime.TotalGameTime.TotalSeconds;
                    }

                    btnBack.offset = pos;
                    btnBack.Update();
                    btnBack.Draw();

                    if (btnBack.IsJustClicked) {
                        whichAnimation = 1;
                        animTime = gameTime.TotalGameTime.TotalSeconds;
                    }

                    btnOptions.pos=new Vector2((screenSize.X-btnOptions.tex.Width)/2,648);
                    btnOptions.offset = pos;
                    btnOptions.Update();
                    btnOptions.Draw();

                    if (btnOptions.IsJustClicked) {
                        //Go to options menu
                        whoseTurn = -3;
                        mnuOptions.parentScreen = -1;
                    }
                }
                if (whoseTurn == -1) { //if the options button has not been pressed
                    //        is at start                                         left mouse is justclicked                                                                        mouse is in screen
                    if (pos.X > -1 && whichAnimation == 1 && input.mouseState.LeftButton == ButtonState.Pressed && input.lastMouseState.LeftButton == ButtonState.Released && IsInRange(input.mousePosition, Vector2.Zero, screenSize.ToVector2())) {
                        //move on to level select screen
                        //BeginGame();
                        whoseTurn = -2;
                    }
                }



                if (t < zoomTime + zoomLen + 1.0f) {
                    Game1.spriteBatch.Draw(pixeltex, new Rectangle(0, 0, screenSize.X, screenSize.Y), Color.FromNonPremultiplied(255, 255, 255, (int)(255 * Math.Sqrt(Math.Abs(zoomTime + zoomLen + 1.0 - t)))));
                }
            }

            if (t < 6) {
                tex.SetData<Color>(glitchscreen);
                spriteBatch.Draw(tex, Vector2.Zero, null, Color.White, 0, Vector2.Zero, 2, SpriteEffects.None, 0);
            }





            /*if (t < 10)
            {
                for (int x = 0; x <= (int)screenSize.X / 128; x++)
                {
                    for (int y = -1; y <= (int)screenSize.Y / 128; y++)
                        spriteBatch.Draw(images["title-bg"], BaseRoutines.trunc(new Vector2(128 * x, 128 * y + 32 * ((float)(t / 3.2) % 1)), 1), Color.FromNonPremultiplied(255,255,255,128));
                }
            }*/
            spriteBatch.End();
            tex.Dispose();
        }

        /// <summary>
        /// Does a partial reinitialization
        /// </summary>
        public void BeginGame() {

            //LoadBoard();
            whoseTurn = 0;
            currentDoughboy = -1;
            Game1.players[0].health = 100;
            Game1.players[1].health = 100;
            fauxboy = null;
            texter.AddMessage("1");

            //set everything to have been by the computer
            for (int y = 0; y < 12; y++)
                for (int x = 0; x < 14; x++)
                    vars[y, x] = 0x00F0;
        }

        public void DrawScreenLines(GameTime time, Vector2 offset) {
            int xp = (int)(-offset.X / 128);
            for (int x = xp; x <= (int)screenSize.X / 128 + xp; x++) {
                for (int y = -1; y <= (int)screenSize.Y / 128; y++)
                    spriteBatch.Draw(images["title-bg"], BaseRoutines.trunc(offset + new Vector2(128 * x, 128 * y + 32 * ((float)(time.TotalGameTime.TotalSeconds / 3.2) % 1)), 1), Color.White);
            }
        }

    }
}
