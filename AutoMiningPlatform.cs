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
//Enums ---/

//Interfaces ---

//Interfaces ---/


//Classes ---

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

public class Message{
    public string Tag;
    public string Content;

    public string getMessage(){
        return "["+ Tag +"] "+Content;
    }
}

public class MessageScreen{
    private string IndexText;
    private bool index = true;
    private readonly StateProvider stateProvider;


    List<Message> StandardMessages = new List<Message>();
    List<Message> AdvancedMessages = new List<Message>();

    public MessageScreen(StateProvider sp){
        stateProvider = sp;
    }

    public string buildMessage(){
        string result = buildIndex();

        for(int i=0;i<StandardMessages.Count;i++){
            result += StandardMessages[i].getMessage();
        }

        for(int i=0;i<AdvancedMessages.Count;i++){
            result += AdvancedMessages[i].getMessage();
        }
        return result;
    }

    public string buildIndex(){
        if(index)IndexText = "[/-/-/-] ";
        else IndexText = "[/-/-/-] ";
        index = !index;

        IndexText += stateProvider.state.ToString() + "...\n";
        return IndexText;
    }

    public void AddStandardMessage(Message msg){
        StandardMessages.Add(msg);
    }
    public void AddAdvancedMessages(Message msg){
        AdvancedMessages.Add(msg);
    }
}

//Classes ---/

////////////////////////////////////////////////////////////
//Init ---

// Config And Saving
ScriptConfig config = new ScriptConfig(version,versionTag);
MyIni gIni = new MyIni();
MyIniParseResult gIniResult;

// ScreenMessaging
static StateProvider mainState = new StateProvider(StateType.INIT);

MessageScreen messageScreen = new MessageScreen(mainState);
Message gMessage = new Message();

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

    AddMessage("Test","Data");

    UpdateScreens();
}
//Main ---/
////////////////////////////////////////////////////////////

public void UpdateScreens(){
    if(config.getShowAdvancedData())GatherAdvancedData();

    string msg = messageScreen.buildMessage();
    Echo(msg);
}

//Messages and Screens
public void AddMessage(string tag, string content){
    gMessage.Tag = tag;
    gMessage.Content = content;

    messageScreen.AddStandardMessage(gMessage);
}

public void GatherAdvancedData(){

}


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