// Automatic Mining Platform 2 By Kezeslabas
String versionTag = "DEVELOPMENT";
String version = "0.1.0";

//Enums ---

//Enums ---/

//Interfaces ---
public Interface IMessageContent{
    String getContent();
}
//Interfaces ---/


//Classes ---

// Config
public class Config{
    
}

// Screen and Messages
public Class BaseMessageContent implements IMessageContent{
        String value;
        
        public String getContent(){
            return value;
        }
}
public Class UnitMessageContent implements IMessageContent{
        String value;
        String Unit;
        
        public String getContent(){
            return value + " " + Unit;
        }
}
public Class ComplexMessageContent implements IMessageContent{
        String value;
        String complexData;
        
        public String getContent(){
            return value + complexData;
        }
}

public Class Message{
    public String Tag;
    public IMessageContent Content;

    public String getMessage{
        return "["+Tag+"] "+Content.getContent();
    }
}

public Class MessageScreen{
    String Header;
    String Status;
    List<Message> StandardMessages = new List<Message>();
    List<Message> AdvancedMessages = new List<Message>();

    public String buildMessage{
        String result = Header;
        result += Status;
        for(int i=0;i<StandardMessages.Count;i++){
            result += StandardMessages[i].getMessage();
        }
        for(int i=0;i<AdvancedMessages.Count;i++){
            result += AdvancedMessages[i].getMessage();
        }
        return result;
    }
}

//Classes ---/


//Init ---

// Config And Saving
MyIni gIni = new MyIni();
MyIniParseResult gIniResult;

// ScreenMessaging
MessageScreen messageScreen = new MessageScreen();

//Init ---/
public void Program()
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

    Echo(messageScreen.buildMessage());
}

//Config And Saving ---

// Config
public bool GetConfig(bool softLoad = false){
    if(Me.CustomData=="" || !gIni.TryParse(Me.CustomData, out IniResult)){
        return false;
    }

    return true;
}

public SetConfig(){
    // Set Config String By Hand, for better editability in Custom Data
    Me.CustomData=""
    +"[Mining Platform Configuration]\n"
    +"Version="+Version+"\n"
    +";You can Configure the script by changing the values below.\n"

    Me.CustomData = ""
    +"/n---"
}

// Saving
public SaveData(){
    gIni.Clear();
    //gIni.Set("Category","Parameter",Value);

    Storage = gIni.ToString();
}

public LoadData(){
    gini.TryParse(Storage, out IniResult);

    //Parse Methods
    // TryGetBoolean
    // TryGetInt32
    // TryGetInt64
    if(IniResult.IsDefined && IniResult.Success){
        //if(!gIni.Get("Category","Parameter").TryGetBoolean(out Variable))//Message
    }
}
//Config And Saving ---/