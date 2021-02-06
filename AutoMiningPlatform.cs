// Automatic Mining Platform 2 By Kezeslabas
static string versionTag = "DEVELOPMENT";
static string version = "0.1.0";


////////////////////////////////////////////////////////////
//Enums ---
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

public enum StepType{
    HORIZONTAL,
    VERTICAL,
    ROTATION,
    START,
    FINISH
}
//Enums ---/

//Interfaces ---

//Interfaces ---/


//Classes ---

// Controllers ---
public class StepController{
    // StepType stepType = StepType.START;

    // int StepNumber = 0;

    public void SetNew(){

    }
}

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

        if(!Rotor.IsSet){
            screen.AddMessage("Error"," Rotor not found!");
            return false;
        }

        double vectorDot = 0;
        IMyPistonBase piston;
        for(int i=0;i<blocks.Count;i++){
            lTerminalBlock = blocks[i];

            if(lTerminalBlock is IMyPistonBase){
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
                else if(config.SmartDetection){
                    piston = lTerminalBlock as IMyPistonBase;
                    vectorDot = Vector3D.Dot(Rotor.Vertical,piston.Position - piston.Top.Position);
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

        if(!HorizontalPistons.CheckDirections(config.InvTag)){
            screen.Message("Error"," Inverted Hor Piston Tag Error!");
            return false;
        }
        if(!VerticalPistons.CheckDirections()){
            screen.Message("Error"," Inverted Ver Piston Tag Error!");
            return false;
        }

        return true;
    }
}

public class RotationController{
    public bool IsSet = false;
    public Vector3D Vertical;
    
    IMyMotorStator Rotor;


    public bool SetRotor(IMyMotorStator block){
        if(IsSet)return false;
        Rotor = block;
        IsSet = true;

        Vertical = Rotor.Position - Rotor.Top.Position;
        Vertical.Normalize;

        return IsSet;
    }

    public void Clear(){
        IsSet = false;
        Rotor = null;
    }
}

public class PistonController{
    List<PistonBlock> pistons = new List<PistonBlock>();

    public void AddPiston(PistonBlock pb){
        pistons.Add(pb);
    }

    public int getCount(){
        return pistons.Count;
    }

    public void Clear(){
        pistons.Clear();
    }

    public bool CheckDirections(String invTag, bool smart){
        if(pistons.Count == 0)return false;

        bool foundFirst = false;
        Vector3D InvDirection;
        double vectorDot = 0;
        //InvTag Search, and init if not SmartDetection
        for(int i=0;i<pistons.Count;i++){
            if(pistons[i].CheckInverted(invTag)){
                if(!foundFirst){
                    foundFirst = true;
                    InvDirection = pistons[i].Direction;
                }
                else{
                    vectorDot = Vector3D.Dot(InvDirection,pistons[i].Direction);
                    //If not paralell to InvDirection, then throw error
                    if(vectorDot<0.9f){
                        return false;
                    }
                }
                if(smart)break;
            }
        }

        if(foundFirst && smart){
            for(int i=0;i<pistons.Count;i++){
                if(pistons[i].CheckInverted(invTag)){
                    vectorDot = Vector3D.Dot(InvDirection,pistons[i].Direction);
                    if(vectorDot>0.9f){
                        pistons[i].Inverted = true;
                    }
                    else if(vectorDot<0.1f && vectorDot>-0.1f){
                        return false;
                    }
                }
            }
        }

        return true;
    }
}

public class DrillController{
    List<IMyShipDrill> Drills = new List<IMyShipDrill>();

    public void AddDrill(IMyShipDrill block){
        Drills.Add(block);
    }

    public void Clear(){
        Drills.Clear();
    }

    public int getCount(){
        return Drills.Count;
    }
}

// Blocks
public class PistonBlock{
    public IMyPistonBase Block;
    public bool Inverted;
    public float TargetDistance;
    private Vector3D Direction { get; }

    public PistonBlock(IMyPistonBase pis, bool inv=false){
        Block=pis;
        Inverted=inv;
        if(inv)TargetDistance=Block.HighestPosition;
        else TargetDistance=Block.LowestPosition;

        Direction = Block.Position - Block.Top.Position;
        Direction.Normalize();
    }

    public bool CheckInverted(String tag){
        if(Block.CustomName.Contains(tag))Inverted = true;

        return Inverted;
    }
}

// Config
public class ScriptConfig{
    private bool IsDevelopment = true;
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

    bool ShowAdvancedData = true;//Special
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
    
    public ScriptConfig(string version, string mode){
        this.version = version;
        if(mode == "RELEASE")IsDevelopment = false;
    }

    public void setShowAdvancedData(bool b){
        if(!IsDevelopment)ShowAdvancedData = !b;
    }
    public bool getShowAdvancedData(){
        return ShowAdvancedData;
    }

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

        +"\n[Advanced Options]\n"
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
}

// Screen and Messages
public class StateProvider {
    public StateType state { get; set; }

    public StateProvider(StateType state){
        this.state = state;
    }
}

public class MessageScreen{
    private string IndexText;
    private bool index = true;
    private readonly StateProvider stateProvider;
    private string StandardMessages = "";
    private string LastMessages = "";


    public MessageScreen(StateProvider sp){
        stateProvider = sp;
    }

    public string buildMessage(){
        string result = buildIndex();

        result += StandardMessages;

        LastMessages = StandardMessages;
        StandardMessages = "";

        return result;
    }

    public string buildIndex(){
        if(index)IndexText = "[/-/-/-] ";
        else IndexText = "[-/-/-/] ";
        index = !index;

        IndexText += stateProvider.state.ToString() + "...\n";
        return IndexText;
    }

    public void AddMessage(string tag, string content){
        StandardMessages += "["+tag+"] "+content+"\n";
    }

}

//Classes ---/

////////////////////////////////////////////////////////////
//Init ---

// Config And Saving
static ScriptConfig config = new ScriptConfig(version,versionTag);
MyIni gIni = new MyIni();
MyIniParseResult gIniResult;

// ScreenMessaging
static StateProvider mainState = new StateProvider(StateType.INIT);

static MessageScreen messageScreen = new MessageScreen(mainState);

// Blocks
List<IMyTerminalBlock> gTerminalBlocks = new List<IMyTerminalBlock>();

// Controllers
PlatformController mainController = new PlatformController(config,messageScreen);

//Init ---/
////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////
//Main ---
public Program()
{

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
    SetConfig();
    GatherBlocks();
    RefreshHardBlocks();

    messageScreen.AddMessage("Hor",""+mainController.HorizontalPistons.getCount());
    messageScreen.AddMessage("Ver",""+mainController.VerticalPistons.getCount());
    messageScreen.AddMessage("Rotor",""+mainController.Rotor.IsSet);
    messageScreen.AddMessage("Drill",""+mainController.Drills.getCount());

    UpdateScreens();
}
//Main ---/
////////////////////////////////////////////////////////////

//Messages and Screens ---
public void UpdateScreens(){
    if(config.getShowAdvancedData())GatherAdvancedData();

    string msg = messageScreen.buildMessage();
    Echo(msg);
}

public void GatherAdvancedData(){

}
//Messages and Screens ---/

// Blocks ---

public void RefreshHardBlocks(){
    mainController.getBlocksFrom(gTerminalBlocks);
}

public void GatherBlocks(){
    gTerminalBlocks.Clear();
    GridTerminalSystem.SearchBlocksOfName(config.MainTag,gTerminalBlocks);
}

// Blocks ---/

//Config And Saving ---

// Config
public bool GetConfig(bool softLoad = false){
    if(Me.CustomData=="" || !gIni.TryParse(Me.CustomData, out gIniResult)){
        return false;
    }

    return true;
}

public void SetConfig(){
    // Set Config String By Hand, for better editability in Custom Data
    Me.CustomData=config.toConfigString();
    
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