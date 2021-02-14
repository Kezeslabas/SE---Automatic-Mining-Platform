// Automatic Mining Platform 2 By Kezeslabas
string versionTag = "DEVELOPMENT";
string version = "0.1.0";


//-//////////////////////////////////////////////////
//State ---
/// <summary>
/// The different States of The script
/// </summary>
public enum StateType{
    SET,
    START,
    PAUSE,
    REFRESH,
    STANDBY,
    EMERGENCY,
    AUTOPAUSE,
    ALIGNING,
    SETMOVINGPARTS,
    FINISHED,
    ALIGNINGSTARTINGPOSITION,
    DIGGING,
    INIT
}

/// <summary>
/// A structure that can hold the different data assigned to a <c>StateType</c>
/// <see cref="StateType"/>
/// </summary>
public struct StateData{
    public string Text;
    public Color Col;
    public StateData(string Text,Color Col)
    {
        this.Text = Text;
        this.Col = Col;
    }
}
// ---

public enum StepType{
    HORIZONTAL,
    VERTICAL,
    ROTATION,
    START,
    FINISH
}

//-//////////////////////////////////////////////////
//Hard Controllers ---
public class StepController{
    // StepType stepType = StepType.START;

    // int StepNumber = 0;

    public void SetNew(){

    }
}

/// <summary>
/// The PlatformController Controlls the basic components and functions of the mining platform.
/// </summary>
public class PlatformController{
    //Injection
    private ScriptConfig config;
    private MessageScreen screen;

    //SubControllers
    public RotationController Rotor = new RotationController();
    public PistonController HorizontalPistons = new PistonController();
    public PistonController VerticalPistons = new PistonController();
    public DrillController Drills = new DrillController();

    //Local vars
    IMyTerminalBlock lTerminalBlock;

    public PlatformController(ScriptConfig config, MessageScreen screen){
        this.config = config;
        this.screen = screen;
    }

    /// <summary>
    /// Checks the provided blocks and distributes them to the other controllers.
    /// </summary>
    /// <param name="blocks">List with Blocks gathered from the GridTerminalSystem</param>
    /// <returns>Returns false, if a required component is not found.</returns>
    public bool getBlocksFrom(List<IMyTerminalBlock> blocks){
        HorizontalPistons.Clear();
        VerticalPistons.Clear();
        Rotor.Clear();
        Drills.Clear();

        //Get Rotor
        for(int i=0;i<blocks.Count;i++){
            lTerminalBlock = blocks[i];
            if(lTerminalBlock is IMyMotorAdvancedStator){
                if(!Rotor.SetRotor(lTerminalBlock as IMyMotorStator)){
                    screen.AddMessage("Error"," Additional Rotor Found\n"+lTerminalBlock.CustomName+"\n");
                }                
            }
        }

        //Escape if Rotor not found
        if(!Rotor.IsSet){
            screen.AddMessage("Error"," Rotor not found!");
            return false;
        }

        double vectorDot = 0;
        IMyPistonBase piston;
        Vector3D checkVector;
        //Get Pistons and Drills
        for(int i=0;i<blocks.Count;i++){
            lTerminalBlock = blocks[i];

            if(lTerminalBlock is IMyPistonBase){
                //First Check for Tags
                if(lTerminalBlock.CustomName.Contains(config.VerTag)){
                    if(lTerminalBlock.CustomName.Contains(config.InvTag)){
                        VerticalPistons.AddPiston(new PistonBlock(lTerminalBlock as IMyPistonBase));
                    }
                    else{
                        VerticalPistons.AddPiston(new PistonBlock(lTerminalBlock  as IMyPistonBase));
                    }
                }
                else if(lTerminalBlock.CustomName.Contains(config.HorTag)){
                    if(lTerminalBlock.CustomName.Contains(config.InvTag)){
                        HorizontalPistons.AddPiston(new PistonBlock(lTerminalBlock  as IMyPistonBase));
                    }
                    else{
                        HorizontalPistons.AddPiston(new PistonBlock(lTerminalBlock  as IMyPistonBase));
                    }
                }
                //Then Smart Detection
                else if(config.SmartDetection){
                    piston = lTerminalBlock as IMyPistonBase;
                    checkVector = piston.GetPosition() - piston.Top.GetPosition();
                    checkVector.Normalize();
                    vectorDot = Vector3D.Dot(Rotor.Vertical, checkVector);
                    // screen.AddMessage("Debug"," "+piston.CustomName+"\n"+checkVector.ToString()+"\nDot: "+vectorDot);
                    if(vectorDot>0.9f || vectorDot<-0.9f){
                        //Vetical
                        VerticalPistons.AddPiston(new PistonBlock(piston));
                    }
                    else if(vectorDot<0.1f && vectorDot>-0.1f){
                        //Horizontal
                        HorizontalPistons.AddPiston(new PistonBlock(piston));
                    }
                    else {
                        //Warning
                        screen.AddMessage("Warning"," Piston direction can't be calculated!\n"+lTerminalBlock.CustomName+"\n");
                    }

                }
                else {
                    screen.AddMessage("Warning"," No Subtag Found\n"+lTerminalBlock.CustomName+"\n");
                }
            }
            else if(lTerminalBlock is IMyShipDrill){
                Drills.AddDrill(lTerminalBlock as IMyShipDrill);
            }
        }
        string msg;

        msg = HorizontalPistons.CheckDirections(config.InvTag,config.SmartDetection);

        if(msg!=""){
            screen.AddMessage("Error","Hor: "+msg);
            return false;
        }

        msg = VerticalPistons.CheckDirections(config.InvTag,config.SmartDetection);

        if(msg!=""){
            screen.AddMessage("Error","Ver: "+msg);
            return false;
        }

        //DEBUG
        // screen.AddMessage("Vertical","\n"+VerticalPistons.getReport());
        // screen.AddMessage("Horizontal","\n"+HorizontalPistons.getReport());

        //SetValueIfCorrectStructureFound

        return true;
    }
}

/// <summary>
/// It controlls the Rotor.
/// </summary>
public class RotationController{
    public bool IsSet = false;
    public Vector3D Vertical;
    
    IMyMotorStator Rotor;

    /// <summary>
    /// Sets the <c>RotationController</c> to a Rotor.
    /// </summary>
    /// <param name="block">Rotor</param>
    /// <returns>Retruns false, if Rotor already set.</returns>
    public bool SetRotor(IMyMotorStator block){
        if(IsSet)return false;
        Rotor = block;
        IsSet = true;

        Vertical = Rotor.GetPosition() - Rotor.Top.GetPosition();
        Vertical.Normalize();

        return IsSet;
    }

    /// <summary>
    /// Resets the RotationController
    /// </summary>
    public void Clear(){
        IsSet = false;
        Rotor = null;
        Vertical = Vector3D.Zero;
    }
}

/// <summary>
/// It controlls a piston arm.
/// </summary>
public class PistonController{
    List<PistonBlock> pistons = new List<PistonBlock>();

    public void AddPiston(PistonBlock pb){
        pistons.Add(pb);
    }


    /// <returns>Returns the number of pisons in the controller.</returns>
    public int getCount(){
        return pistons.Count;
    }
    /// <summary>Resets the controller</summary>
    public void Clear(){
        pistons.Clear();
    }

    /// <summary>Checks and sets the directions of the pistons.</summary>
    /// <param name="invTag">The Tag that identifies Inverted Pistons</param>
    /// <param name="smart">Whether use smart detection or not.</param>
    /// <returns>Return Error messageses, if any.</returns>
    public string CheckDirections(String invTag, bool smart){
        if(pistons.Count == 0)return "";

        bool foundFirst = false;
        Vector3D baseVector = Vector3D.Zero;
        //InvTag Search, and init if not SmartDetection
        for(int i=0;i<pistons.Count;i++){
            if(pistons[i].CheckInvertedTag(invTag)){
                if(!foundFirst){
                    foundFirst = true;
                    baseVector = pistons[i].Direction;
                }
            }
        }
        if(smart){
            //Check Inverted Tag Consistency and Set Pistons
            string result = "";
            if(foundFirst)baseVector = baseVector*-1;
            else baseVector = pistons[0].Direction;
            for(int i=0;i<pistons.Count;i++){
                result += pistons[i].CheckInOrderDirection(baseVector,foundFirst);
            }
            return result;
        }

        return "";
    }

    /// <returns>Returns a report from all Pistons in the controller</returns>
    public string getReport(){
        string result = "";

        for(int i=0;i<pistons.Count;i++){
            result += pistons[i].getReport();
        }

        result += "\n";
        return result;
    }
}

/// <summary>
/// It Controlls the drills.
/// </summary>
public class DrillController{
    List<IMyShipDrill> Drills = new List<IMyShipDrill>();

    public void AddDrill(IMyShipDrill block){
        Drills.Add(block);
    }

    /// <summary>Resets the DrillController</summary>
    public void Clear(){
        Drills.Clear();
    }

    /// <returns>Returns the number of drills in the container</returns>
    public int getCount(){
        return Drills.Count;
    }
}
// Block
/// <summary>
/// A Piston with additional data for the platform.
/// </summary>
public class PistonBlock{
    public IMyPistonBase Block;
    public bool Inverted;
    public float TargetDistance;
    public Vector3D Direction;

    public PistonBlock(IMyPistonBase pis, bool inv=false){
        Block=pis;
        Inverted=inv;
        if(inv)TargetDistance=Block.HighestPosition;
        else TargetDistance=Block.LowestPosition;

        Direction = pis.GetPosition() - pis.Top.GetPosition();
        Direction.Normalize();
    }

    /// <summary>Checks if a piston has the Inverted Tag, and sets the PistonBlock</summary>
    /// <param name="tag">Inverted Tag String</param>
    /// <returns>Return true if the Piston is Inverted</returns>
    public bool CheckInvertedTag(String tag){
        if(Block.CustomName.Contains(tag))Inverted = true;

        return Inverted;
    }

    /// <summary>Checks the Piston's direction, and sets it accordingly</summary>
    /// <param name="inOrderVector">The In-Order vector to Check against.</param>
    /// <param name="foundInverted">If the Inverted direction already found.</param>
    /// <returns>Returns errors, if any.</returns>
    public string CheckInOrderDirection(Vector3D inOrderVector, bool foundInverted){
        double vectorDot = Vector3D.Dot(inOrderVector,Direction);
        if(vectorDot>0.9f){
            if(Inverted)return "Inconsistent Inv Tag\n"+this.getReport();
        }
        else if(vectorDot<0.1f && vectorDot>-0.1f){
            return "Piston Out of Order\n"+this.getReport();
        }
        else if(vectorDot<-0.9f){
            if(foundInverted)Inverted = true;
            else return "Missing Inv Tag\n"+this.getReport();
        }
        return "";
    }

    /// <returns>Returns a report from the Piston</returns>
    public string getReport(){
        return ""+Block.CustomName+" | "+ (Inverted ? "Inverted" : "In Order") + "\n";
    }
}
//Hard Controllers ---/
////////////////////////////////////////////////////////////
//Soft Controllers ---/

/// <summary>
/// The SoftController Controlls the extra components and functions of the mining platform, 
/// that are not required for the platform to work.
/// It controlls the screens, including the Programmable Block's main screen.
/// </summary>
public class SoftController{
    private ScriptConfig config;
    private MessageScreen msgScreen;
    private StateProvider stateProvider;

    private ScreenController screenController;

    private IMyTerminalBlock lTerminalBlock;
    public SoftController(
        ScriptConfig config, 
        StateProvider stateProvider,
        MessageScreen msgScreen){
        
        this.config = config;
        this.stateProvider = stateProvider;
        this.msgScreen = msgScreen;
        screenController = new ScreenController(config);
    }

    /// <summary>Sets the main screen.</summary>
    /// <param name="me">This Programmable Block</param>
    public void init(IMyProgrammableBlock me){
        screenController.AddMainScreen(me);
    }

    /// <summary>Checks the provided blocks and distributes them to the other controllers.</summary>
    /// <param name="blocks">List of Blocks gathered from the GridTerminalSystem</param>
    public void getBlocksFrom(List<IMyTerminalBlock> blocks){
        for(int i=0;i<blocks.Count;i++){
            lTerminalBlock = blocks[i];
            if(lTerminalBlock is IMyTextSurfaceProvider){
                screenController.AddScreensOf(lTerminalBlock);
            }
        }

        screenController.initScreens();
    }

    /// <summary>Uses the ScreenController to update all the screens with a message</summary>
    /// <param name="msg">The message to write out to the screens.</param>
    public void UpdateScreens(string msg){
        screenController.UpdateScreens(msg, stateProvider.stateData.Col);
    }
}

/// <summary>
/// It Controlls the screens and LCDs of the script.
/// </summary>
public class ScreenController{
    private List<IMyTextSurface> Screens = new List<IMyTextSurface>();
    private ScriptConfig config;

    private long MainEntity;

    private string[] lScreenData;
    private IMyTextSurface lTextSurface;
    private IMyTextSurfaceProvider lTextSurfaceProvider;

    public ScreenController(ScriptConfig config){
        this.config = config;
    }

    /// <summary>Sets the main [0] screen of the script.</summary>
    /// <param name="me">This Programmable Block</param>
    public void AddMainScreen(IMyProgrammableBlock me){
        Screens.Add(me.GetSurface(0));
        MainEntity = me.EntityId;
    }

    /// <summary>Searches for Screens in a Block, and adds the based on the Custom Data</summary>
    /// <param name="block">A Block that is IMyTextSurfaceProvider</param>
    /// <returns>Returns false if provided block is not IMyTextSurfaceProvider</returns>
    public bool AddScreensOf(IMyTerminalBlock block){
        if(block is not IMyTextSurfaceProvider)return false;
        if(block is IMyTextPanel){
            Screens.Add(block as IMyTextSurface);
        }
        else{
            lScreenData=block.CustomData.Split('\n');
            string currentString;
            int n;
            for(int i=0;i<lScreenData.Length;i++)
            {
                currentString=lScreenData[i];
                if(currentString.StartsWith("@"))
                {
                    currentString=currentString.Substring(1);
                    if(currentString.Contains(config.MainTag))
                    {
                        currentString=currentString.Replace(config.MainTag,"");
                        if(Int32.TryParse(currentString, out n))
                        {
                            lTextSurfaceProvider=block as IMyTextSurfaceProvider;
                            if(lTextSurfaceProvider.SurfaceCount>=n)
                            {
                                lTextSurface=lTextSurfaceProvider.GetSurface(n);
                                Screens.Add(lTextSurface);
                            }
                        }
                    }
                }
            }
        }
        return true;
    }

    /// <summary>Updates the Screens with a message, and a color, based on the config.</summary>
    /// <param name="msg">Message to display.</param>
    /// <param name="col">Color of the text on screen</param>
    public void UpdateScreens(string msg, Color col){
        for(int i=0;i<Screens.Count;i++){
            lTextSurface = Screens[i];
            if(config.LcdColorCoding)lTextSurface.FontColor = col;
            lTextSurface.WriteText(msg);
        }
    }

    /// <summary>Changes all screens to display Text and Images</summary>
    public void initScreens(){
        for(int i=0;i<Screens.Count;i++){
            lTextSurface = Screens[i];
            lTextSurface.ContentType=ContentType.TEXT_AND_IMAGE;
        }
    }
}

//Soft Controllers ---/
////////////////////////////////////////////////////////////
//Generic ---

// Config
/// <summary>
/// Class to Store all the configuration data for the script
/// </summary>
public class ScriptConfig{
    public bool IsDevelopment { get; } = true;
    private string version;

    //Config Values
    public bool ShowScriptName = true;//NEW

    //Highlighted Hard Values
    public string MainTag = "/Mine 01/";
    public float MaxRotorAngle = 360;
    public float MinRotorAngle = 0;

    //Soft Values
    public long TransmissionReceiverAddress = 0;
    public bool UseAutoPause = true;
    public float HighCargoLimit = 0.9f;
    public float LowCargoLimit = 0.5f;

    public bool ShowAdvancedData = true;
    public bool UseCargoContainersOnly = true;
    public bool LcdColorCoding = true;
    public bool DynamicRotorTensor = true;
    public bool AlwaysUpdateDetailedInfo = false;

    public float RotorSpeedAt10m = 0.5f;
    public float HorizontalExtensionSpeed = 0.5f;
    public float VerticalExtensionSpeed = 0.5f;

    public float DigModeSpeed = 3f;

    //Hard Values
    public bool SmartDetection = true;
    public bool AlwaysRetractHorizontalPistons = false;
    public bool ShareInertiaTensor = true;

    public float MinHorizontalLimit = 0;
    public float MaxHorizontalLimit = 0;

    public float MinVerticalLimit = 0;
    public float MaxVerticalLimit = 0;

    public string VerTag="/Ver/";
    public string HorTag="/Hor/";
    public string InvTag="/Inv/";
    public string StartTimerTag="/Start/";
    public string PauseTimerTag="/Pause/";
    public string FinishedTimerTag="/Finished/";

    // Color Coding
    public readonly Dictionary<StateType,StateData> StateConfig = 
    new Dictionary<StateType, StateData>
        {
            {StateType.SET,new StateData("Set",Color.Magenta)},
            {StateType.START,new StateData("Start",Color.Cyan)},
            {StateType.PAUSE,new StateData("Pause",Color.Yellow)},
            {StateType.REFRESH,new StateData("Refresh",Color.Violet)},
            {StateType.STANDBY,new StateData("Waiting For Commands",Color.White)},
            {StateType.EMERGENCY,new StateData("Emergency Stop",Color.Crimson)},
            {StateType.AUTOPAUSE,new StateData("Auto Pause",Color.Gold)},
            {StateType.ALIGNING,new StateData("Aligning...",Color.DodgerBlue)},
            {StateType.SETMOVINGPARTS,new StateData("Setting Moving Parts",Color.DodgerBlue)},
            {StateType.FINISHED,new StateData("Mining Finished",Color.Lime)},
            {StateType.ALIGNINGSTARTINGPOSITION,new StateData("Alingning Starting Position",Color.Magenta)},
            {StateType.DIGGING,new StateData("Digging...",Color.Tomato)},
            {StateType.INIT,new StateData("Initializing...",Color.Tomato)}
        };
    
    public ScriptConfig(string version, string mode){
        this.version = version;
        if(mode == "RELEASE")IsDevelopment = false;
    }

    /// <summary>Converts the config data to a string, that can be parsed by the Ini</summary>
    /// <returns>Return a string that contains the configurable data.</returns>
    public String toConfigString(){
        return ""
        +"[Mining Platform Configuration]\n"
        +"Version="+version+"\n"
        +";You can Configure the script by changing the values below.\n"

        +"\n[Highlighted Options]\n"
        +";They will apply when the Set command is used.\n\n"

        +"MainTag="+MainTag+"\n"
        +"MaxRotorAngle="+MaxRotorAngle+"\n"
        +"MinRotorAngle="+MinRotorAngle+"\n"

        +"\n[Quick Options]\n"
        +";They will apply when the Refresh or Set command is used.\n\n"

        +"TransmissionReceiverAddress="+TransmissionReceiverAddress+"\n"
        +"UseAutoPause="+UseAutoPause+"\n"
        +"HighCargoLimit="+HighCargoLimit+"\n"
        +"LowCargoLimit="+LowCargoLimit+"\n\n"

        +"ShowAdvancedData="+ShowAdvancedData+"\n"
        +"UseCargoContainersOnly="+UseCargoContainersOnly+"\n"
        +"LcdColorCoding="+LcdColorCoding+"\n"
        +"DynamicRotorTensor="+DynamicRotorTensor+"\n"
        +"AlwaysUpdateDetailedInfo="+AlwaysUpdateDetailedInfo+"\n\n"

        +"RotorSpeedAt10m="+RotorSpeedAt10m+"\n"
        +"HorizontalExtensionSpeed="+HorizontalExtensionSpeed+"\n"
        +"VerticalExtensionSpeed="+VerticalExtensionSpeed+"\n\n"

        +"DigModeSpeed="+DigModeSpeed+"\n"

        +"\n[Hard Options]\n"
        +";They will apply when the Set command is used.\n\n"

        +"SmartDetection="+SmartDetection+"\n"
        +"AlwaysRetractHorizontalPistons="+AlwaysRetractHorizontalPistons+"\n"
        +"ShareInertiaTensor="+ShareInertiaTensor+"\n\n"

        +"MinHorizontalLimit="+MinHorizontalLimit+"\n"
        +"MaxHorizontalLimit="+MaxHorizontalLimit+"\n\n"

        +"MinVerticalLimit="+MinVerticalLimit+"\n"
        +"MaxVerticalLimit="+MaxVerticalLimit+"\n\n"

        +"VerTag="+VerTag+"\n"
        +"HorTag="+HorTag+"\n"
        +"InvTag="+InvTag+"\n"
        +"StartTimerTag="+StartTimerTag+"\n"
        +"PauseTimerTag="+PauseTimerTag+"\n"
        +"FinishedTimerTag="+FinishedTimerTag+"\n"

        +"\n---";
    }

    /// <returns>Returns false if not all StateType has config data.</returns>
    public bool CheckStateConfig(){
        if(!IsDevelopment)return true;

        foreach(StateType i in Enum.GetValues(typeof(StateType))){
            if(!StateConfig.ContainsKey(i))return false;
        }

        return true;
    }
}

// Screen and Messages
/// <summary>
/// Stores the Current State of the Script
/// </summary>
public class StateProvider {
    private StateType state;
    public StateData stateData;

    private readonly ScriptConfig config;

    public StateProvider(ScriptConfig config,StateType state){
        this.config = config;
        this.setState(state);
    }

    /// <summary>Sets the State</summary>
    /// <param name="state">The State</param>
    public void setState(StateType state){
        this.state = state;
        this.stateData = config.StateConfig[state];
    }

    /// <returns>Returns the Current State</returns>
    public StateType getState(){
        return state;
    }
}

/// <summary>
/// Handles the messages that are displayed.
/// </summary>
public class MessageScreen{
    private readonly StateProvider stateProvider;
    private readonly ScriptConfig config;

    private string IndexText;
    private bool index = true;

    private string StandardMessages = "";
    private string LastMessages = "";


    public MessageScreen(ScriptConfig config,StateProvider stateProvider){
        this.config = config;
        this.stateProvider = stateProvider;
    }

    /// <returns>Builds and Returns a constructed message</returns>
    public string buildMessage(){
        string result = buildIndex();

        result += StandardMessages;

        LastMessages = StandardMessages;
        StandardMessages = "";

        return result;
    }

    /// <returns>Returns the header part of the message</returns>
    public string buildIndex(){
        IndexText = "";

        if(config.IsDevelopment)IndexText += "[Development]\n";

        if(index)IndexText += "[/-/-/-] ";
        else IndexText += "[-/-/-/] ";
        index = !index;

        IndexText += stateProvider.stateData.Text + "\n";
        return IndexText;
    }

    /// <summary>Adds Info to the message with a tag and content</summary>
    /// <param name="tag">Tag</param>
    /// <param name="content">Content</param>
    public void AddMessage(string tag, string content){
        StandardMessages += "["+tag+"] "+content+"\n";
    }

}

//Generic ---/
////////////////////////////////////////////////////////////
//Init ---
MyIni gIni = new MyIni();
MyIniParseResult gIniResult;

// Blocks
List<IMyTerminalBlock> gTerminalBlocks = new List<IMyTerminalBlock>();

//Init ---/
////////////////////////////////////////////////////////////
//Declaration ---
// Config And Saving
ScriptConfig config;

// State Mamagement
StateProvider mainState;

// ScreenMessaging
MessageScreen messageScreen;

// Controllers
PlatformController mainController;
SoftController softController;
//Declaration ---/
////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////
//Main ---
public Program()
{
    Init();
    if(!GetConfig())SetConfig();
}

public void Save()
{

}


public void Main(string argument, UpdateType updateSource)
{
    if((updateSource & UpdateType.Update100)==0)
    {

    }
    else
    {
        
    }

    // Just Testing Stuff
    if(!GetConfig())SetConfig();

    GatherBlocks();
    RefreshHardBlocks();
    RefreshSoftBlocks();

    messageScreen.AddMessage("Hor",""+mainController.HorizontalPistons.getCount());
    messageScreen.AddMessage("Ver",""+mainController.VerticalPistons.getCount());
    messageScreen.AddMessage("Rotor",""+mainController.Rotor.IsSet);
    messageScreen.AddMessage("Drill",""+mainController.Drills.getCount());
    // ---
    
    UpdateScreens();
}
//Main ---/
////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////
//Init ---
/// <summary>
/// Sets the Instances of the controllers and providers.
/// Checks integritiy if in Development
/// </summary>
public void Init(){
    config = new ScriptConfig(version,versionTag);

    mainState = new StateProvider(config,StateType.INIT);

    messageScreen = new MessageScreen(config,mainState);

    mainController = new PlatformController(config,messageScreen);
    softController = new SoftController(config,mainState,messageScreen);

    if(!config.CheckStateConfig())messageScreen.AddMessage("DEV-DEBUG","EnumState Missing!");
}
//Init/
////////////////////////////////////////////////////////////
//Messages and Screens ---
/// <summary>
/// Initiates the Update Sequence for the Detailed Info and the Screens
/// </summary>
public void UpdateScreens(){
    if(config.ShowAdvancedData)GatherAdvancedData();

    string msg = messageScreen.buildMessage();
    Echo(msg);
    softController.UpdateScreens(msg);
}

public void GatherAdvancedData(){

}
//Messages and Screens ---/
////////////////////////////////////////////////////////////
//Block Providing ---

/// <summary>Get Hard Blocks from gTerminalBlocks</summary>
public void RefreshHardBlocks(){
    mainController.getBlocksFrom(gTerminalBlocks);
}

/// <summary>Get Soft Blocks from gTerminalBlocks</summary>
public void RefreshSoftBlocks(){
    softController.init(Me);
    softController.getBlocksFrom(gTerminalBlocks);
}

/// <summary>
/// Gathers Blocks form the Gridterminal System to gTerminalBlocks.
/// Uses the MainTag from the Config as a filter.
/// </summary>
public void GatherBlocks(){
    gTerminalBlocks.Clear();
    GridTerminalSystem.SearchBlocksOfName(config.MainTag,gTerminalBlocks);
}

//Block Providing ---/
////////////////////////////////////////////////////////////
//Config And Saving ---

// Config

public bool GetConfig(bool softLoad = false){
    if(Me.CustomData=="" || !gIni.TryParse(Me.CustomData, out gIniResult)){
        messageScreen.AddMessage("Config","Couldn't load config! Reseting...");
        return false;
    }

    bool hardChange = false;
    string s;
    float f;
    bool b;

    //Highlighted options
    s=gIni.Get("Highlighted Options","MainTag").ToString();
    if(s != config.MainTag){hardChange = true; config.MainTag = s;}
    f=gIni.Get("Highlighted Options","MaxRotorAngle").ToSingle();
    if(f != config.MaxRotorAngle){hardChange = true; config.MaxRotorAngle = f;}
    f=gIni.Get("Highlighted Options","MinRotorAngle").ToSingle();
    if(f != config.MinRotorAngle){hardChange = true; config.MinRotorAngle = f;}


    //Hard Options
    b=gIni.Get("Hard Options","SmartDetection").ToBoolean();
    if(b != config.SmartDetection){hardChange = true; config.SmartDetection = b;}
    b=gIni.Get("Hard Options","AlwaysRetractHorizontalPistons").ToBoolean();
    if(b != config.AlwaysRetractHorizontalPistons){hardChange = true; config.AlwaysRetractHorizontalPistons = b;}
    b=gIni.Get("Hard Options","ShareInertiaTensor").ToBoolean();
    if(b != config.ShareInertiaTensor){hardChange = true; config.ShareInertiaTensor = b;}

    f=gIni.Get("Hard Options","MinHorizontalLimit").ToSingle();
    if(f != config.MinHorizontalLimit){hardChange = true; config.MinHorizontalLimit = f;}
    f=gIni.Get("Hard Options","MaxHorizontalLimit").ToSingle();
    if(f != config.MaxHorizontalLimit){hardChange = true; config.MaxHorizontalLimit = f;}

    f=gIni.Get("Hard Options","MinVerticalLimit").ToSingle();
    if(f != config.MinVerticalLimit){hardChange = true; config.MinVerticalLimit = f;}
    f=gIni.Get("Hard Options","MaxVerticalLimit").ToSingle();
    if(f != config.MaxVerticalLimit){hardChange = true; config.MaxVerticalLimit = f;}

    s=gIni.Get("Hard Options","VerTag").ToString();
    if(s != config.VerTag){hardChange = true; config.VerTag = s;}
    s=gIni.Get("Hard Options","HorTag").ToString();
    if(s != config.HorTag){hardChange = true; config.HorTag = s;}
    s=gIni.Get("Hard Options","InvTag").ToString();
    if(s != config.InvTag){hardChange = true; config.InvTag = s;}
    s=gIni.Get("Hard Options","StartTimerTag").ToString();
    if(s != config.StartTimerTag){hardChange = true; config.StartTimerTag = s;}
    s=gIni.Get("Hard Options","PauseTimerTag").ToString();
    if(s != config.PauseTimerTag){hardChange = true; config.PauseTimerTag = s;}
    s=gIni.Get("Hard Options","FinishedTimerTag").ToString();
    if(s != config.FinishedTimerTag){hardChange = true; config.FinishedTimerTag = s;}


    //Soft Options
    config.TransmissionReceiverAddress=gIni.Get("Quick Options","TransmissionReceiverAddress").ToInt64();

    config.UseAutoPause=gIni.Get("Quick Options","UseAutoPause").ToBoolean();
    config.HighCargoLimit=gIni.Get("Quick Options","HighCargoLimit").ToSingle();
    config.LowCargoLimit=gIni.Get("Quick Options","LowCargoLimit").ToSingle();
    config.UseCargoContainersOnly=gIni.Get("Quick Options","UseCargoContainersOnly").ToBoolean();

    config.ShowAdvancedData=gIni.Get("Quick Options","ShowAdvancedData").ToBoolean();

    config.LcdColorCoding=gIni.Get("Quick Options","LcdColorCoding").ToBoolean();

    config.DynamicRotorTensor=gIni.Get("Quick Options","DynamicRotorTensor").ToBoolean();
    config.AlwaysUpdateDetailedInfo=gIni.Get("Quick Options","AlwaysUpdateDetailedInfo").ToBoolean();

    config.RotorSpeedAt10m=gIni.Get("Quick Options","RotorSpeedAt10m").ToSingle();
    config.HorizontalExtensionSpeed=gIni.Get("Quick Options","HorizontalExtensionSpeed").ToSingle();
    config.VerticalExtensionSpeed=gIni.Get("Quick Options","VerticalExtensionSpeed").ToSingle();
    
    config.DigModeSpeed=gIni.Get("Quick Options","DigModeSpeed").ToSingle();


    if(hardChange){
        messageScreen.AddMessage("Config","Hard Changes Detected!");
    }
    
    messageScreen.AddMessage("Config", "Loaded!");

    return true;
}

public void SetConfig(){
    // Set Config String By Hand, for better editability in Custom Data
    Me.CustomData=config.toConfigString();
    messageScreen.AddMessage("Config", "Config Set to Custom Data");
    
}

// Saving
public void SaveData(){
    gIni.Clear();
    //gIni.Set("Category","Parameter",Value);

    Storage = gIni.ToString();
}

public void LoadData(){
    gIni.TryParse(Storage, out gIniResult);

    //Parse Methods
    // TryGetBoolean
    // TryGetInt32
    // TryGetInt64
    if(gIniResult.IsDefined && gIniResult.Success){
        //if(!gIni.Get("Category","Parameter").TryGetBoolean(out Variable))//Message
    }
}
//Config And Saving ---/
////////////////////////////////////////////////////////////