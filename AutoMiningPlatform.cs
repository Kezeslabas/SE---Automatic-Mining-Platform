//Features
// Mining Sequence: Working
//  - The script manages the Pistons, Drills and a Rotor to mine.
// Adaptive Extension: Working
//  - The script calcualtes the optimal extension length for vertical pistons based on the length of the drilling head
// Legacy Detection: Working
//  - The script detects it's components by a main tag, and secondaray tags, like "/Ver/","/Hor/","/Inv/",/Adv/"
// (New) Adaptive Speed: Working
//  - The script detects the length of the piston arm, and adapts the rotation speed to optimze effectiveness.
// (New) Advanced Set: Working
//  - After the "set;" command and argument can be passed. A regular even number sets the script to a step, 
//    a number with the "m" like "30m" sets the script to the closest step to that mining distance.
// (New) Smart Detection: Working
//  - The script detects it's components by a main tag, and detects the direction of pistons too.
//    The "/Inv/" tag is still required, but only once.
// (New) Dig Mode: WIP
//  - An alternate mode, where the script expects the player to be in a cockpit and use the drills secondary mining mode.
//    The script speeds up the extensions and rotation speed dinamically to finish the digging as soon as possible.
//  - You can only activate it from a cockpit
//  - Command: dig


//Resolvable Issues
// - Advanced Set: ExtendH changes each time
// - When Dig Mode Paused by Program, Vertical Pistons are Retracting
// - Sometimes the DigSpeed is not used by the Rotor after DigModeChange, until first step is done
// - Step.DigMode possible inconsistencies 
// - When using Horizontal Drills Something calculates wrongly
// - Test report: drills on the ground, script still running, after pause can't reset (didn't redraw drills?)

//Loadable Variables ---
//Costumizable ---
//Highlighted Stuff ---
public string MainTag="/Mine 01/";
public float MaxRotorAngle=360;
public float MinRotorAngle=0;


//End of Highlighted Stuff --

//Quick Updateable ---
bool UseAutoPause=true;
float HighCargoLimit=0.9f;
float LowCargoLimit=0.5f;
long TransmissionReceiverAddress=0;

bool ShowAdvancedData=true;
bool LcdColorCoding=true;
bool DynamicRotorTensor=true;
bool UseCargoContainersOnly=true;
bool AlwaysUpdateDetailedInfo=false;

float HorizontalExtensionSpeed=0.5f;
float VerticalExtensionSpeed=0.5f;
float RotorSpeedAt10m=0.5f;

float DigModeSpeed=3f;

//Hard Updateable ---
bool SmartDetection=true;
bool AlwaysRetractHorizontalPistons=false;
bool ShareInertiaTensor=true;

bool AdaptiveHorizontalExtension=true;
float HpStepLength=3.33f;
float VpStepLength=2.5f;

float ManualDrillArmLength=0f;

float MinHorizontalLimit=0;
float MaxHorizontalLimit=0;

float MinVerticalLimit=0;
float MaxVerticalLimit=0;

public string VerTag="/Ver/";
public string HorTag="/Hor/";
public string InvTag="/Inv/";
public string StartTimerTag="/Start/";
public string PauseTimerTag="/Pause/";
public string FinishedTimerTag="/Finished/";
//Non Costumizable ---
//Save&Load these
bool ComponentsReady=false;
bool ImRunning=false;
bool PlatformIsMoving=false;
StepData Step = new StepData();  //Step.Value, Step.ExtendH
RotorData MainRotor = new RotorData(); //RotateToMax, PassedDebugZone
CargoModule Cargo = new CargoModule(); //HighTarget
bool DigModeEnabled=false;
//End of Loadable Stuff ---

float Version=3.8f;
bool NewSet=false;
bool SendMessage=false;
bool AntennaFound=false;
bool UseCargoModule=false;
bool StepCheckResult=false;
bool FirstLoad=true;

//ToReduceAllocation ---
int DebugNumber=0;
string Result="";
List<IMyTerminalBlock> blocks  = new List<IMyTerminalBlock>();
IMyTerminalBlock block;
MyIni _ini = new MyIni();
MyIniParseResult IniResult;

TimeSpan TotalTime = new TimeSpan();
DateTime StartTime = new DateTime();
EventWatcher TimerEvent = new EventWatcher();
ArgumentDecoder ArgumentData = new ArgumentDecoder();

ScreenMessage Message = new ScreenMessage();
DrillingArm DrillArm = new DrillingArm();
PistonArm VerticalArm = new PistonArm();
PistonArm HorizontalArm = new PistonArm();
List<IMyTextSurface> Screens = new List<IMyTextSurface>();

public struct StateData
{
    public string State;
    public Color Color;
    public StateData(string s,Color c)
    {
        State=s;
        Color=c;
    }
}

public enum StateType
{
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
    DIGGING
}

public class CargoModule
{
    public bool IsSet;
    public bool AutoPauseEnabled;
    public bool ShowOnScreen;
    public float HighLimit;
    public float LowLimit;
    public float Fill;
    public bool HighTarget;
    public int Count;

    List<IMyTerminalBlock> Inventorys;
    MyFixedPoint CurrentVolume;
    MyFixedPoint MaxVolume;
    IMyTerminalBlock CurrentBlock;
    string Message;
    bool CreateNewMaxVolume;
    int i;

    public CargoModule()
    {
        IsSet=false;
        AutoPauseEnabled=false;
        ShowOnScreen=false;
        HighLimit=1;
        LowLimit=0;
        Fill=0;
        HighTarget=true;
        Count=0;
        Inventorys = new List<IMyTerminalBlock>();
        CurrentVolume=0;
        MaxVolume=0;
        Message="";
        CreateNewMaxVolume=false;
        i=0;
    }
    public void New(List<IMyTerminalBlock> _blocks, bool cargoOnly,float HighL, float LowL)
    {
        HighLimit=HighL*100f;
        LowLimit=LowL*100f;
        Fill=0;
        AutoPauseEnabled=false;
        Inventorys.Clear();
        CreateNewMaxVolume=false;
        MaxVolume=0;
        if(cargoOnly)
        {
            for(i=0;i<_blocks.Count;i++)
            {
                CurrentBlock=_blocks[i];
                if(CurrentBlock is IMyCargoContainer)
                {
                    Inventorys.Add(CurrentBlock);
                    MaxVolume+=CurrentBlock.GetInventory(0).MaxVolume;
                }
            }
        }
        else
        {
            for(i=0;i<_blocks.Count;i++)
            {
                CurrentBlock=_blocks[i];
                if(CurrentBlock.HasInventory && !(CurrentBlock is IMyShipDrill))
                {
                    Inventorys.Add(CurrentBlock);
                    MaxVolume+=CurrentBlock.GetInventory(0).MaxVolume;
                }
            }            
        }
        Count=Inventorys.Count;
        if(Count==0)
        {
            for(int i=0;i<_blocks.Count;i++)
            {
                CurrentBlock=_blocks[i];
                if(CurrentBlock is IMyShipDrill)
                {
                    Inventorys.Add(CurrentBlock);
                }
            }
            ShowOnScreen=false;
        }
        else ShowOnScreen=true;
        if(Count>0)IsSet=true;
        else IsSet=false;

        Update();
    }
    public void Update() //Updates the data about volume in the class
    {
        CurrentVolume=0;
        if(ShowOnScreen)
        {
            for(i=0;i<Inventorys.Count;i++)
            {
                CurrentBlock=Inventorys[i];
                if(CurrentBlock!=null && CurrentBlock.CubeGrid.GetCubeBlock(CurrentBlock.Position)!=null)
                {
                    CurrentVolume+=CurrentBlock.GetInventory(0).CurrentVolume;
                }
                else
                {
                    CreateNewMaxVolume=true;
                    Inventorys.RemoveAt(i);
                    i--;
                }
            }
            if(CreateNewMaxVolume)
            {
                MaxVolume=0;
                for(i=0;i<Inventorys.Count;i++)
                {
                    MaxVolume+=Inventorys[i].GetInventory(0).MaxVolume;
                }
            }
            Fill=(float)CurrentVolume/(float)MaxVolume;
            Fill*=100;
        }
        else
        {
            if(Inventorys.Count>0)
            {
                CurrentBlock=Inventorys[0];
                if(CurrentBlock!=null && CurrentBlock.CubeGrid.GetCubeBlock(CurrentBlock.Position)!=null)
                {
                    MaxVolume=CurrentBlock.GetInventory(0).MaxVolume;
                    CurrentVolume=CurrentBlock.GetInventory(0).CurrentVolume;
                    Fill=(float)CurrentVolume/(float)MaxVolume;
                    Fill*=100;
                }
            }
        }
    }
    public void CheckIfFilled() //Set the data of the class, based on the fillage of the cargo block list
    {
        if(HighTarget)
        {
            if(Fill>=HighLimit)
            {
                AutoPauseEnabled=true;
                HighTarget=false;
            }
            else AutoPauseEnabled=false;
        }
        else
        {
            if(Fill<=LowLimit)
            {
                AutoPauseEnabled=false;
                HighTarget=true;
            }
            else AutoPauseEnabled=true;
        }
    }
    public string ConstructMsg() //Creates a string that contains formatted data for the Screens and Detailed Info
    {
        Message="";
        Fill=(float)Math.Round(Fill,MidpointRounding.AwayFromZero);
        Message+="Cargo:      [";
        for(i=0;i<Math.Round(Fill/2.5);i++)
        {
            Message+="|";
        }   
        while(i<40)
        {
            Message+="'";
            i++;
        }
        return Message+="] "+Fill+"%\n";      
    }
};

public class RotorData
{
    public bool IsSet;
    public IMyMotorStator Rotor;
    public Vector3D Position;
    public Vector3D DirectionVector;
    public float CurrentAngle;
    public float CurrentAngleRad;
    public float TargetSpeed;
    public byte TensorTimer;
    public bool RotateToMax;
    public bool IsInPosition;
    public bool PassedDebugZone;
    public float DebugZoneHighDeg;
    public float DebugZoneLowDeg;
    public ITerminalAction InertiaTensor;
    public bool ConstantTensor;
    public float Speed;
    public bool UseDigSpeed;
    public float DigSpeed;

    float DebugZoneLow;
    float DebugZoneHigh;
    bool Integrity;
    float MaxAngle;
    float MinAngle;
    float InnerDistance;
    bool First;
    
    public RotorData()
    {
        IsSet=false;
        Position=Vector3D.Zero;
        DirectionVector=Vector3D.Up;
        CurrentAngle=0;
        CurrentAngleRad=0;
        TargetSpeed=0.5f;
        TensorTimer=0;
        RotateToMax=true;
        IsInPosition=false;
        PassedDebugZone=false;
        DebugZoneHighDeg=0;
        DebugZoneLowDeg=0;
        ConstantTensor=false;

        DebugZoneLow=0;
        DebugZoneHigh=0;
        Integrity=false;
        MaxAngle=360f;
        MinAngle=0;
        Speed=0.5f;
        InnerDistance=360f;
        First=false;
        UseDigSpeed=false;
        DigSpeed=0;
    }
    public void New(IMyMotorStator _rotor)
    {
        Rotor=_rotor;
        Position=Rotor.GetPosition();
        IMyAttachableTopBlock top = Rotor.Top;
        DirectionVector = Position-top.GetPosition();
        TensorTimer=0;
        IsInPosition=false;
        Integrity=true;
        InertiaTensor=Rotor.GetActionWithName("ShareInertiaTensor");
        ConstantTensor=false;
        IsSet=true;
        First=false;
        UseDigSpeed=false;
    }
    public void UpdateTensor() //Enables the Share Inertia Tensor at the 40. run of the method for 1 run
    {
        if(!ConstantTensor)
        {
            if(TensorTimer>=25)
            {
                EnableTensor();
                TensorTimer=0;
            }
            else
            {
                if(TensorTimer==3)
                {
                    EnableTensor(false);
                }
            }
            TensorTimer++;
        }
    }
    public void Init(float max, float min) //Sets the Rotor's Limits, based on degree, and saves it in Radian with 1 degree correction, for later use
    {
        max=(float)((max*Math.PI)/180f);
        min=(float)((min*Math.PI)/180f);
        if(max<min)
        {
            float switcher=max;
            max=min;
            min=switcher;
        }

        if(max-min>6.28318531f)min=max-6.28318531f;

        MaxAngle=max-0.008f;
        MinAngle=min+0.008f;

        Rotor.UpperLimitRad=MaxAngle;
        Rotor.LowerLimitRad=MinAngle;

        Rotor.BrakingTorque=1000000000f;

        InnerDistance=(MaxAngle-MinAngle)*0.25f;
        DebugZoneHigh=MaxAngle-InnerDistance;
        DebugZoneLow=MinAngle+InnerDistance;

        DebugZoneHighDeg=(float)Math.Round(DebugZoneHigh*180f/Math.PI,1,MidpointRounding.AwayFromZero);
        DebugZoneLowDeg=(float)Math.Round(DebugZoneLow*180f/Math.PI,1,MidpointRounding.AwayFromZero);

    }
    public void QuickInit(float _speed, float _digSpeed, float _distance=0, bool _setSpeedToo=false)
    {
        Speed=_speed;
        DigSpeed=_digSpeed;
        if(_setSpeedToo)SetSpeed(_distance);
    }
    public void SetToTarget(float distance, bool _first=true) //Set the values of the Rotor to the given step number.
    {
        First=_first;
        CurrentAngleRad=Rotor.Angle;
        if(CurrentAngleRad>6.28318531f)CurrentAngleRad=CurrentAngleRad%6.28318531f;
        else if(CurrentAngleRad<-6.28318531f)CurrentAngleRad=CurrentAngleRad%-6.28318531f;

        if(First)
        {
            if(CurrentAngleRad==MaxAngle)
            {
                RotateToMax=true;
                if(UseDigSpeed)TargetSpeed=10*DigSpeed/distance;
                else TargetSpeed=10*Speed/distance;
            }
            else if(CurrentAngleRad==MinAngle)
            {
                RotateToMax=false;
                if(UseDigSpeed)TargetSpeed=-10*DigSpeed/distance;
                else TargetSpeed=-10*Speed/distance;
            }
            if(CurrentAngleRad<MaxAngle && CurrentAngleRad>MinAngle)
            {
                //Inside of Boundary
                if(MaxAngle-CurrentAngleRad>=CurrentAngleRad-MinAngle)
                {
                    RotateToMax=false;
                    if(UseDigSpeed)TargetSpeed=-10*DigSpeed/distance;
                    else TargetSpeed=-10*Speed/distance;
                }
                else 
                {
                    RotateToMax=true;
                    if(UseDigSpeed)TargetSpeed=10*DigSpeed/distance;
                    else TargetSpeed=10*Speed/distance;
                }
            }
            else
            {
                //outside of Boundary
                if(CurrentAngleRad<MinAngle)
                {
                    InnerDistance=Math.Abs(CurrentAngleRad-MinAngle);
                    if(InnerDistance<(6.28318531f-InnerDistance))
                    {
                        RotateToMax=false;
                        SetLimits(MinAngle);
                        if(UseDigSpeed)TargetSpeed=10*DigSpeed/distance;
                        else TargetSpeed=10*Speed/distance;
                    }
                    else
                    {
                        RotateToMax=true;
                        SetLimits(MaxAngle);
                        if(UseDigSpeed)TargetSpeed=-10*DigSpeed/distance;
                        else TargetSpeed=-10*Speed/distance;
                    }
                }
                else
                {
                    InnerDistance=Math.Abs(CurrentAngleRad-MaxAngle);
                    if(InnerDistance<(6.28318531f-InnerDistance))
                    {
                        RotateToMax=true;
                        SetLimits(MaxAngle);
                        if(UseDigSpeed)TargetSpeed=-10*DigSpeed/distance;
                        else TargetSpeed=-10*Speed/distance;
                    }
                    else
                    {
                        RotateToMax=false;
                        SetLimits(MinAngle);
                        if(UseDigSpeed)TargetSpeed=10*DigSpeed/distance;
                        else TargetSpeed=10*Speed/distance;
                    }
                }
            }
            PassedDebugZone=true;
        }
        else 
        {
            PassedDebugZone=false;
                    
            if(RotateToMax && IsInPosition)RotateToMax=false;
            else if(!RotateToMax && IsInPosition)RotateToMax=true;

            if(UseDigSpeed)
            {
                if(RotateToMax)TargetSpeed=10*DigSpeed/distance;
                else TargetSpeed=-10*DigSpeed/distance;
            }
            else
            {
                if(RotateToMax)TargetSpeed=10*Speed/distance;
                else TargetSpeed=-10*Speed/distance;
            }
        }

        TargetSpeed=(float)Math.Round(TargetSpeed,3,MidpointRounding.AwayFromZero);
        Rotor.TargetVelocityRPM=TargetSpeed;

        CurrentAngle=(float)Math.Round(CurrentAngleRad*180f/Math.PI,1,MidpointRounding.AwayFromZero);
        
        IsInPosition=false;
    }
    public void ReverseTurn()
    {
        RotateToMax=!RotateToMax;
        Rotor.TargetVelocityRPM=-TargetSpeed;
    }
    public void SetSpeed(float distance)
    {
        if(UseDigSpeed)
        {
            if(RotateToMax)TargetSpeed=10*DigSpeed/distance;
            else TargetSpeed=-10*DigSpeed/distance;
        }
        else
        {
            if(RotateToMax)TargetSpeed=10*Speed/distance;
            else TargetSpeed=-10*Speed/distance;
        }

        TargetSpeed=(float)Math.Round(TargetSpeed,3,MidpointRounding.AwayFromZero);
        Rotor.TargetVelocityRPM=TargetSpeed;
    }
    public void Update() //Refreshes the Current Angle of the Rotor in degrees
    {
        CurrentAngle=(float)Math.Round(Rotor.Angle*180f/Math.PI,1,MidpointRounding.AwayFromZero);
    }
    public void CheckTarget()
    {
        CurrentAngleRad=Rotor.Angle;
        if(PassedDebugZone)
        {
            if(First)
            {
                if(Math.Abs(CurrentAngleRad-MaxAngle)<0.004f)
                {
                    IsInPosition=true;
                    RotateToMax=true;
                }
                else if(Math.Abs(CurrentAngleRad-MinAngle)<0.004f)
                {
                    IsInPosition=true;
                    RotateToMax=false;
                }
            }
            else if(RotateToMax)
            {
                if(Math.Abs(CurrentAngleRad-MaxAngle)<0.004f)IsInPosition=true;
            }
            else
            {
                if(Math.Abs(CurrentAngleRad-MinAngle)<0.004f)IsInPosition=true;
            }
        }
        else
        {
            if(CurrentAngleRad>=DebugZoneLow && CurrentAngleRad<=DebugZoneHigh)PassedDebugZone=true;
            else
            {
                if(RotateToMax)
                {
                    if(CurrentAngleRad>=DebugZoneHigh)ReverseTurn();
                }
                else
                {
                    if(CurrentAngleRad<=DebugZoneLow)ReverseTurn();
                }
                
            }
        }
    }
    public bool IntegrityTest()
    {
        if(Rotor!=null && Rotor.CubeGrid.GetCubeBlock(Rotor.Position)!=null)return true;
        else
        {
            Integrity=false;
            return false;
        }
    }
    public string EmergencyStop()
    {
        if(!Integrity)
        {
            return "[System]:Rotor missing!\n";
        }
        else
        {
            Rotor.Enabled=false;
            return "";
        }
    }
    public void SetLimits(float _limit)
    {
        Rotor.UpperLimitRad=_limit;
        Rotor.LowerLimitRad=_limit;
    }
    public void SetLimits(bool _default=true)
    {
        Rotor.UpperLimitRad=MaxAngle;
        Rotor.LowerLimitRad=MinAngle;
    }
    public void EnableTensor(bool _on=true)
    {
        if(_on!=Rotor.GetValueBool("ShareInertiaTensor"))
        {
            InertiaTensor.Apply(Rotor);
        }
    }
    public void EnableTensorStatic(bool _on=true)
    {
        ConstantTensor=_on;
        if(_on!=Rotor.GetValueBool("ShareInertiaTensor"))
        {
            InertiaTensor.Apply(Rotor);
        }
    }
    public float InnerRadius()
    {
        return Math.Abs(MaxAngle-MinAngle);
    }
};

public class DrillingArm
{
    public List<IMyShipDrill> Drills;
    public float Length;
    
    public Vector3D FurthestVector;
    public Vector3D ClosestVector;

    IMyShipDrill FurthestDrill;
    IMyShipDrill ClosestDrill;
    IMyShipDrill CurrentBlock;
    bool Integrity;

    public DrillingArm()
    {
        Drills = new List<IMyShipDrill>();
        FurthestVector = Vector3D.Zero;
        ClosestVector = Vector3D.Zero;;
        Integrity=true;
    }
    public void New()
    {
        Drills.Clear();
        FurthestVector = Vector3D.Zero;
        ClosestVector = Vector3D.Zero;;
        Integrity=true;
    }
    public void Init(float _length,Vector3D RotorPos) //Sets the Furthest&Closest Drills and their DirectionVectors from the Rotor's Position
                                                      //Also, if the given length is not 0, then it calculates the lenght of the DrillArm
    {
        Vector3D CheckVector;
        double MaxLength=0;
        double MinLength=int.MaxValue;
        double CurrentLenth=0;
        int MaxIndex=0;
        int MinIndex=0;
        for(int i=0;i<Drills.Count;i++)
        {
            CheckVector=Drills[i].GetPosition();
            CheckVector=CheckVector-RotorPos;
            CurrentLenth=CheckVector.Length();
            if(CurrentLenth>MaxLength)
            {
                MaxLength=CurrentLenth;
                MaxIndex=i;
            }
            else if(CurrentLenth<MinLength)
            {
                MinLength=CurrentLenth;
                MinIndex=i;
            }
        }
        FurthestDrill=Drills[MaxIndex];
        ClosestDrill=Drills[MinIndex];
        FurthestVector=RotorPos-FurthestDrill.GetPosition();
        ClosestVector=RotorPos-ClosestDrill.GetPosition();
        if(_length==0)
        {
            CheckVector=FurthestVector-ClosestVector;
            Length=(float)Math.Round(2.5f+CheckVector.Length(),1,MidpointRounding.AwayFromZero);
        }
        else Length=_length;
    }
    public void Enable(bool _on=true)
    {
        for(int i=0;i<Drills.Count;i++)
        {
            Drills[i].Enabled=_on;
        }
    }
    public bool IntegrityTest()
    {
        for(int i=0;i<Drills.Count;i++)
        {
            CurrentBlock=Drills[i];
            if(CurrentBlock==null || CurrentBlock.CubeGrid.GetCubeBlock(CurrentBlock.Position)==null)
            {
                Integrity=false;
                return false;
            }
        }
        return true;
    }
    public string EmergencyStop()
    {
        if(!Integrity)
        {
            for(int i=0;i<Drills.Count;i++)
            {
                CurrentBlock=Drills[i];
                if(CurrentBlock!=null && CurrentBlock.CubeGrid.GetCubeBlock(CurrentBlock.Position)!=null)CurrentBlock.Enabled=false;
            }
            return "[System]: Drill is Missing!\n";
        }
        else
        {
            for(int i=0;i<Drills.Count;i++)
            {
                Drills[i].Enabled=false;
            }
            return "";
        }
    }
};

public class PistonBlock
{
    public IMyPistonBase Block;
    public bool Inverted;
    public float TargetDistance;

    public PistonBlock(IMyPistonBase pis, bool inv=false)
    {
        Block=pis;
        Inverted=inv;
        if(inv)TargetDistance=Block.HighestPosition;
        else TargetDistance=Block.LowestPosition;
    }
}

public class StepData
{
    public bool IsSet;
    public int Value;
    public int V;
    public int H;
    public int MaxV;
    public int MaxH;
    public int Max;
    public bool ExtendH;
    public int Progression;
    public bool Odd;
    public bool Finished;
    public bool First;
    public bool Horizontal;
    public bool UseExtendH;
    public float MaxTime;
    public float CurrentTime;
    public int Hours;
    public int Minutes;
    public bool DigMode;

    float HStepExtensionTime;
    float VStepExtensionTime;
    public List<float> RotationTimes;//
    float CurrentRotationTime;
    public float AFullRotationTime;//
    int i;

    public StepData()
    {
        IsSet=false;
        Value=0;
        V=0;
        H=0;
        MaxV=0;
        MaxH=0;
        Max=0;
        ExtendH=true;
        Progression=0;
        Odd=false;
        Finished=false;
        First=false;
        Horizontal=true;
        UseExtendH=false;
        i=0;

        MaxTime=0;
        CurrentTime=0;

        HStepExtensionTime=0;
        VStepExtensionTime=0;
        RotationTimes = new List<float>();
        CurrentRotationTime=0;
        AFullRotationTime=0;

        Hours=0;
        Minutes=0;
        DigMode=false;
    }
    public void New(int val=0,double mH=0, double mV=0, bool _alwaysRetract=false, bool _first=true)
    {
        if(mV>=0)MaxV=(int)mV;
        else mH=0;
        if(mH>=0)MaxH=(int)mH;
        else MaxH=0;
        Max=2+MaxV*2+(MaxV+1)*MaxH*2;
        if(val>Max)Value=Max;
        else if(val<0)Value=0;
        else Value=val;

        UseExtendH=!_alwaysRetract;
        ExtendH=true;
        IsSet=true;
        First=_first;
        Analysis();
        Progression=(int)Math.Round((Value*100f/Max));
        i=0;

        MaxTime=0;
        CurrentTime=0;
        HStepExtensionTime=0;
        VStepExtensionTime=0;
        CurrentRotationTime=0;
        AFullRotationTime=0;

        Hours=0;
        Minutes=0;
        DigMode=false;
    }
    public void DigModeChange(double mH, double mV)
    {
        if(mV>=0)MaxV=(int)mV;
        else mH=0;
        if(mH>=0)MaxH=(int)mH;
        else MaxH=0;
        Max=2+MaxV*2+(MaxV+1)*MaxH*2;

        Value=0;
        First=true;
        Analysis();
        
        Progression=(int)Math.Round((Value*100f/Max));
        DigMode=!DigMode;
    }
    public bool Update() //Increases the Step Value by one, and checks if the Max Value is reached.
    {
        Value++;
        Progression=(int)Math.Round((Value*100f/Max));
        Analysis();
        First=false;
        return Finished;

    }
    public void Analysis(int num=0, bool replace=false)//Sets the Horizontal and Vertical Step Values from a given Step number
    {
        if(replace)Value=num;
        else num=Value;
        if(num%2==1)
        {
            num--;
        }
        if(num<=0 || num>=Max)
        {
            V=0;
            H=0;
        }     
        else
        {
            num=num/2;
                        
            V=(int)Math.Floor(num/(double)(MaxH+1));
            if(num%(MaxH+1)==0)
            {
                H=0;
            }
            else 
            {
                H=(num%(MaxH+1));
            }
        }
        if(H==0)Horizontal=false;
        else Horizontal=true;

        if(UseExtendH)
        {
            if(V%2==0)ExtendH=true;
            else ExtendH=false;
        }
        else ExtendH=true;

        if(Value%2==0)Odd=false;
        else Odd=true;

        if(Value>=Max)
        {
            Finished=true;
            Value=Max;
        }
        else Finished=false;
    }
    public void SetToVerticalStep(int _vStep)
    {
        if(_vStep!=V)
        {
            if(_vStep>V)
            {
                for(i=V;i<_vStep;i++)
                {
                    Value+=(MaxH*2)+2;
                }
            }
            else
            {
                for(i=_vStep;i<V;i++)
                {
                    Value-=(MaxH*2)+2;
                }
            }
        }
        Value-=H*2;
        if(Value%2==1)Value--;
        Analysis();
        Progression=(int)Math.Round((Value*100f/Max));
    }
    public void NewETA(float vStepTime, float hStepTime, float hLength, float hStepLength, float rRadius, float rSpeed)
    {
        RotationTimes.Clear();

        VStepExtensionTime=vStepTime;
        HStepExtensionTime=hStepTime;
        MaxTime=VStepExtensionTime*MaxV+HStepExtensionTime*MaxH*(MaxV+1);//333,28
        if(!UseExtendH)MaxTime+=HStepExtensionTime*MaxH*MaxV;
        
        AFullRotationTime=0;
        for(i=0;i<MaxH+1;i++)
        {
            CurrentRotationTime=(60*rRadius/6.28318531f)/(10*rSpeed/(hLength+hStepLength*i));
            RotationTimes.Add(CurrentRotationTime);
            AFullRotationTime+=CurrentRotationTime;
        }
        MaxTime+=AFullRotationTime*(MaxV+1);//2412,96

        MaxTime+=Max;//72

    }
    public void UpdateETA()
    {
        if(Finished)CurrentTime=0;
        else
        {
            CurrentTime=MaxTime;
            
            if(Odd || First)CurrentTime-=VStepExtensionTime*V+HStepExtensionTime*H*(V+1);
            else if(Horizontal)CurrentTime-=VStepExtensionTime*V+HStepExtensionTime*H-1*(V+1);
            else CurrentTime-=VStepExtensionTime*V-1+HStepExtensionTime*H-1*(V+1);

            if(!UseExtendH)
            {
                CurrentTime-=HStepExtensionTime*MaxH*V;
            }

            if(ExtendH)
            {

                for(i=0;i<H;i++)
                {
                    CurrentTime-=RotationTimes[i];
                }
            }
            else
            {
                for(i=H-1;i>=0;i--)
                {
                    CurrentTime-=RotationTimes[i];
                }        
            }
            
            CurrentTime-=AFullRotationTime*V;
            CurrentTime-=Value;
        }

        Hours=(int)Math.Floor(CurrentTime/3600f);
        Minutes=(int)Math.Ceiling((CurrentTime%3600f)/60f);
    }
}

public class PistonArm
{
    public List<PistonBlock> Pistons;
    public float Length;
    public float StepLength;
    public float ExtendableLength;
    public float ArmTargetDistance;
    public Vector3D DebugVector;
    public bool IsInPosition;
    public bool Enabled;
    public float ActualSpeed;
    public float ExtensionSpeed;

    public float MinExtendableLength;
    public float MaxExtendableLength;

    public bool UseDigSpeed;
    public float DigSpeed;
    public float DigStepLength;

    bool Integrity;
    int MovingPistons;

    IMyPistonBase CurrentPiston;
    float _RemainingDistance;
    bool _FoundInverted;

    public PistonArm()
    {
        Pistons=new List<PistonBlock>();
        Length=0;
        StepLength=2.5f;
        ExtendableLength=0;
        ArmTargetDistance=0;
        DebugVector = Vector3D.Zero;
        IsInPosition=false;
        
        Enabled=false;
        MovingPistons=0;
        ExtensionSpeed=0.5f;
        ActualSpeed=ExtensionSpeed;

        MaxExtendableLength=0;
        MinExtendableLength=0;

        UseDigSpeed=false;
        DigSpeed=0;
        DigStepLength=0;
        
        MovingPistons=0;
        Integrity=true;

        _RemainingDistance=0;
        _FoundInverted=false;
    }
    public void New()
    {
        Pistons.Clear();
        Length=0;
        StepLength=2.5f;
        ExtendableLength=0;
        ArmTargetDistance=0;
        DebugVector = Vector3D.Zero;
        IsInPosition=false;
        Enabled=false;
        //ExtensionSpeed=0.5f;
        ActualSpeed=ExtensionSpeed;

        MaxExtendableLength=0;
        MinExtendableLength=0;

        UseDigSpeed=false;
        //DigSpeed=0;

        Integrity=true;
        MovingPistons=0;
    }
    public float EffectiveTargetDistance() //Calculates the effective distance of the Arm. Used when setting the rotor's speed.
    {
        return Length+ArmTargetDistance;
    }
    public void SetStepLength(float referenceLength, bool adaptive=false)
    {
        if(adaptive)
        {
            StepLength=(float)Math.Round((ExtendableLength/Math.Ceiling(ExtendableLength/referenceLength)),2,MidpointRounding.AwayFromZero);
        }
        else StepLength=referenceLength;
        DigStepLength=StepLength+5f;
    }
    public float StepExtensionTime()
    {
        if(UseDigSpeed)return DigStepLength/DigSpeed;
        else return StepLength/ExtensionSpeed;
    }
    public void SetToTarget(int step, bool extend=true)
    {
        IsInPosition=false;
        MovingPistons=0;

        if(UseDigSpeed)
        {
            if(extend)ArmTargetDistance=step*DigStepLength;
            else ArmTargetDistance=ExtendableLength-step*DigStepLength;
        }
        else
        {
            if(extend)ArmTargetDistance=step*StepLength;
            else ArmTargetDistance=ExtendableLength-step*StepLength;
        }

        ArmTargetDistance+=MinExtendableLength;
        if(ArmTargetDistance>MaxExtendableLength)ArmTargetDistance=MaxExtendableLength;

        _RemainingDistance=ArmTargetDistance;
        for(int i=0;i<Pistons.Count;i++)
        {
            CurrentPiston=Pistons[i].Block;
            if(_RemainingDistance>0)
            {
                if(CurrentPiston.HighestPosition>_RemainingDistance)
                {
                    if(!Pistons[i].Inverted)
                    {
                        CurrentPiston.MinLimit=_RemainingDistance;
                        CurrentPiston.MaxLimit=_RemainingDistance;
                    }
                    else
                    {
                        CurrentPiston.MinLimit=CurrentPiston.HighestPosition-_RemainingDistance;
                        CurrentPiston.MaxLimit=CurrentPiston.HighestPosition-_RemainingDistance;
                    }
                    _RemainingDistance=0;
                }
                else
                {
                    if(!Pistons[i].Inverted)
                    {
                        CurrentPiston.MinLimit=CurrentPiston.HighestPosition;
                        CurrentPiston.MaxLimit=CurrentPiston.HighestPosition;
                    }
                    else 
                    {
                        CurrentPiston.MinLimit=CurrentPiston.LowestPosition;
                        CurrentPiston.MaxLimit=CurrentPiston.LowestPosition;
                    }
                    _RemainingDistance-=CurrentPiston.HighestPosition;
                }
            }
            else
            {
                if(Pistons[i].Inverted)
                {
                    CurrentPiston.MinLimit=CurrentPiston.HighestPosition;
                    CurrentPiston.MaxLimit=CurrentPiston.HighestPosition;
                }
                else
                {
                    CurrentPiston.MinLimit=CurrentPiston.LowestPosition;
                    CurrentPiston.MaxLimit=CurrentPiston.LowestPosition;
                }
            }
            Pistons[i].TargetDistance=CurrentPiston.MaxLimit;

            if(CurrentPiston.CurrentPosition!=Pistons[i].TargetDistance)MovingPistons++;

            if(CurrentPiston.CurrentPosition<CurrentPiston.MaxLimit)CurrentPiston.Extend();
            else CurrentPiston.Retract();
        }
        SetSpeed();
    }
    public void SetSpeed()
    {
        if(MovingPistons==0)MovingPistons=1;

        if(UseDigSpeed)ActualSpeed=(float)Math.Round(DigSpeed/MovingPistons,2,MidpointRounding.AwayFromZero);
        else ActualSpeed=(float)Math.Round(ExtensionSpeed/MovingPistons,2,MidpointRounding.AwayFromZero);
        for(int i=0;i<Pistons.Count;i++)
        {
            CurrentPiston=Pistons[i].Block;
            if(CurrentPiston.MaxVelocity>ActualSpeed)
            {
                if(CurrentPiston.Velocity>0)CurrentPiston.Velocity=ActualSpeed;
                else CurrentPiston.Velocity=-ActualSpeed;
            }
            else
            {
                if(CurrentPiston.Velocity>0)CurrentPiston.Velocity=CurrentPiston.MaxVelocity;
                else CurrentPiston.Velocity=-1*CurrentPiston.MaxVelocity;
            }
        }
    }
    public void Init(Vector3D RotorToDrillVector, bool _smart, float _min, float _max)
    {
        IMyAttachableTopBlock top;
        IMyPistonBase piston;
        Vector3D BaseVector;
        Vector3D CheckVector;
        double VectorData;
        if(Pistons.Count>0)
        {
            _FoundInverted=false;
            int i=0;
            if(_smart)
            {
                for(i=0;i<Pistons.Count;i++)//Search first Invereted Piston in list
                {
                    if(Pistons[i].Inverted)
                    {
                        _FoundInverted=true;
                        break;
                    }
                }
            }
            if(_FoundInverted)
            {
                top=Pistons[i].Block.Top;
                BaseVector = Pistons[i].Block.GetPosition();
                BaseVector = BaseVector - top.GetPosition();
                BaseVector.Normalize();
            }
            else
            {
                top=Pistons[0].Block.Top;
                BaseVector = Pistons[0].Block.GetPosition();
                BaseVector = BaseVector - top.GetPosition();
                BaseVector.Normalize();
                BaseVector=Vector3D.Negate(BaseVector);
            }

            if(_smart)
            {
                for(i=0;i<Pistons.Count;i++)//Initialize Piston directions based on found inverted piston + Set DefaultSpeed
                {
                    CurrentPiston=Pistons[i].Block;

                    if(CurrentPiston.MaxVelocity>ExtensionSpeed)CurrentPiston.Velocity=CurrentPiston.MaxVelocity;
                    else CurrentPiston.Velocity=ExtensionSpeed;

                    if(!Pistons[i].Inverted)
                    {
                        top=CurrentPiston.Top;
                        CheckVector = CurrentPiston.GetPosition();
                        CheckVector = CheckVector - top.GetPosition();
                        CheckVector.Normalize();
                        VectorData=Vector3D.Dot(BaseVector,CheckVector);
                        if(VectorData>0.9f)
                        {
                            Pistons[i].Inverted=true;
                        }
                        else if(VectorData<0.1f && VectorData>0.1f)
                        {
                            Pistons.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
            else
            {
                CurrentPiston=Pistons[i].Block;

                if(CurrentPiston.MaxVelocity>ExtensionSpeed)CurrentPiston.Velocity=CurrentPiston.MaxVelocity;
                else CurrentPiston.Velocity=ExtensionSpeed;
            }
            DebugVector=Vector3D.Cross(BaseVector,RotorToDrillVector);
            DebugVector=Vector3D.Cross(DebugVector,BaseVector);
            DebugVector = Vector3D.ProjectOnPlane(ref RotorToDrillVector, ref DebugVector);
            Length = (float)DebugVector.Length();


            MaxExtendableLength=0;
            for(i=0;i<Pistons.Count;i++)//Set the cumulated ExtendableLength of the arm, and adjust Lenght if Pistons are not in position
            {
                piston=Pistons[i].Block;
                MaxExtendableLength+=piston.HighestPosition;
                MinExtendableLength+=piston.LowestPosition;
                if(Pistons[i].Inverted)
                {
                    Length=Length-piston.HighestPosition+piston.CurrentPosition;
                }
                else
                {
                    Length-=piston.CurrentPosition;
                }
            }
            Length = (float)Math.Round(Length,1,MidpointRounding.AwayFromZero);
            Length = Math.Abs(Length);

            if(_max>0 && _max<MaxExtendableLength)
            {
                MaxExtendableLength=_max;
            }
            if(_min>MinExtendableLength)
            {
                MinExtendableLength=_min;
            }
            ExtendableLength=MaxExtendableLength-MinExtendableLength;
        }
        Enable(false);
    }
    public void QuickInit(float _speed, float _digSpeed, bool _setSpeedToo=false)
    {
        ExtensionSpeed=Math.Abs(_speed);
        DigSpeed=_digSpeed;
        if(_setSpeedToo)SetSpeed();
    }
    public void CheckTarget()
    {
        IsInPosition=true;
        for(int i=0;i<Pistons.Count;i++)
        {
            CurrentPiston=Pistons[i].Block;
            if(CurrentPiston.CurrentPosition!=Pistons[i].TargetDistance)
            {
                IsInPosition=false;
                break;
            }
        }
    }
    public bool IntegrityTest()
    {
        for(int i=0;i<Pistons.Count;i++)
        {
            CurrentPiston=Pistons[i].Block;
            if(CurrentPiston==null || CurrentPiston.CubeGrid.GetCubeBlock(CurrentPiston.Position)==null)
            {
                Integrity=false;
                return false;
            }
        }
        return true;
    }
    public string EmergencyStop()
    {
        if(!Integrity)
        {
            for(int i=0;i<Pistons.Count;i++)
            {
                CurrentPiston=Pistons[i].Block;
                if(CurrentPiston!=null && CurrentPiston.CubeGrid.GetCubeBlock(CurrentPiston.Position)!=null)CurrentPiston.Enabled=false;
            }
            return "[System]: Piston is Missing!\n";
        }
        else
        {
            for(int i=0;i<Pistons.Count;i++)
            {
                Pistons[i].Block.Enabled=false;
            }
            return "";
        }
    }
    public void Enable(bool _on=true)
    {
        if(Enabled!=_on)
        {
            for(int i=0;i<Pistons.Count;i++)
            {
                Pistons[i].Block.Enabled=_on;
            }
            Enabled=_on;
        }
    }
    public int GetVStepFromDistance(float _distance, int _maxV)
    {
        if(_maxV!=0)
        {
            if(_distance>=MaxExtendableLength)return _maxV;
            else if(_distance<=MinExtendableLength)return 0;
            else
            {
                _distance-=MinExtendableLength;
                if(UseDigSpeed) return (int)Math.Floor(_distance/DigStepLength);
                else return (int)Math.Floor(_distance/StepLength);
            }
        }
        else return 0;
    }
    public int GetVStepFromDistance(int _maxV)
    {
        if(_maxV!=0)
        {
            if(UseDigSpeed)return (int)Math.Floor(ArmTargetDistance/DigStepLength);
            else return (int)Math.Floor(ArmTargetDistance/StepLength);
        }
        else return 0;
    }
};

public class ScreenMessage
{
    public bool run_indicator=true;
    public StateType State=StateType.STANDBY;
    
    public string Message;
    public string Header;
    public string Report;
    public string Data;
    public Color Color;
    
    public bool AutoRun;
    public bool LcdColoring;
    public string MainTag;
    public List<IMyTextSurface> Screens;

    Dictionary<StateType,StateData> StateConfig;
    StateData stateData;
    IMyTextSurface CurrentScreen;
    IMyTextSurfaceProvider CurrentProvider;
    string[] ScreenData;
    string CurrentString;
    int i;
    int n;

    public ScreenMessage()
    {
        run_indicator=true;
        State=StateType.STANDBY;
        Message="";
        Header="";
        Report="";
        Data="";
        Color=Color.White;
        AutoRun=false;
        LcdColoring=true;
        MainTag="";

        StateConfig = new Dictionary<StateType, StateData>
        {
            {StateType.SET,new StateData("Set",Color.Magenta)},
            {StateType.START,new StateData("Start",Color.Cyan)},
            {StateType.PAUSE,new StateData("Pause",Color.Yellow)},
            {StateType.REFRESH,new StateData("Refresh",Color.Violet)},
            {StateType.STANDBY,new StateData("Waiting For Commands",Color.White)},
            {StateType.EMERGENCY,new StateData("Emergency Stop",Color.Crimson)},
            {StateType.AUTOPAUSE,new StateData("Auto Pause",Color.Gold)},
            {StateType.ALIGNING,new StateData("Aligning...",Color.DodgerBlue)},
            {StateType.DIGGING,new StateData("Digging...",Color.Tomato)},
            {StateType.SETMOVINGPARTS,new StateData("Setting Moving Parts",Color.DodgerBlue)},
            {StateType.FINISHED,new StateData("Mining Finished",Color.Lime)},
            {StateType.ALIGNINGSTARTINGPOSITION,new StateData("Alingning Starting Position",Color.Magenta)}
        };
        stateData=StateConfig[StateType.STANDBY];

        Screens = new List<IMyTextSurface>();
    }
    public void New(IMyTextSurface _surface)
    {
        Screens.Clear();
        _surface.ContentType=ContentType.TEXT_AND_IMAGE;
        Screens.Add(_surface);
    }
    public void AddMe(IMyTextSurface _surface)
    {
        _surface.ContentType=ContentType.TEXT_AND_IMAGE;
        Screens.Add(_surface);
    }
    public void AddToScreens(IMyTerminalBlock _block)
    {
        if(_block is IMyTextPanel)
        {
            CurrentScreen = _block as IMyTextSurface;
            CurrentScreen.ContentType=ContentType.TEXT_AND_IMAGE;
            Screens.Add(CurrentScreen);
        }
        else
        {
            ScreenData=_block.CustomData.Split('\n');
            for(i=0;i<ScreenData.Length;i++)
            {
                CurrentString=ScreenData[i];
                if(CurrentString.StartsWith("@"))
                {
                    CurrentString=CurrentString.Substring(1);
                    if(CurrentString.Contains(MainTag))
                    {
                        CurrentString=CurrentString.Replace(MainTag,"");
                        if(Int32.TryParse(CurrentString, out n))
                        {
                            CurrentProvider=_block as IMyTextSurfaceProvider;
                            if(CurrentProvider.SurfaceCount>=n)
                            {
                                CurrentScreen=CurrentProvider.GetSurface(n);
                                CurrentScreen.ContentType=ContentType.TEXT_AND_IMAGE;
                                Screens.Add(CurrentScreen);
                            }
                        }
                    }
                }
            }
        }
    }
    public void WriteToScreens()
    {
        if(LcdColoring)
        {
            for(int i=0;i<Screens.Count;i++)
            {
                CurrentScreen=Screens[i];
                CurrentScreen.WriteText(Message);
                CurrentScreen.FontColor=Color;
            }
        }
        else
        {
            for(int i=0;i<Screens.Count;i++)
            {
                CurrentScreen=Screens[i];
                CurrentScreen.WriteText(Message);
            }
        }
    }
    public void ConstructMsg()
    {
        stateData=StateConfig[State];
        Header=stateData.State;
        Color=stateData.Color;

        if(run_indicator)Message="[-/-/-/] ";
        else Message="[/-/-/-] ";
       
        Message+=Header+"\n";
        if(Report!="")
        {
            Message+=Report+"\n";
        }
        Message+=Data;
    }
    public void AddReport(string s)
    {
        Report+=s+"\n";
    }
    public void Continue()
    {
        run_indicator=!run_indicator;
        Report="";
    }
}

public class ArgumentDecoder
{
    public bool IsSet;
    public bool Passed;
    public ScriptMode Mode;
    public int Step;
    public float Depth;
    public string Message;

    string[] data;
    string CurrentString;
    
    public ArgumentDecoder()
    {
        IsSet=false;
        Mode=ScriptMode.DEFAULT;
        CurrentString="";
        Step=0;
        Passed=false;
        Depth=0;
    }
    public void New(string _arg)
    {
        IsSet=true;
        Step=0;
        Message="";
        data=_arg.Split(';');
        Passed=false;
        Depth=0;
        DecodeData();
    }
    void DecodeData()
    {
        CurrentString=data[0].ToLower();
        switch(CurrentString)
        {
            case "set":
            {
                Mode=ScriptMode.SET;
                if(data.Length>1)
                {
                    if(data[1].EndsWith("m"))
                    {
                        data[1]=data[1].Remove(data[1].Length-1);
                        if(!Single.TryParse(data[1], out Depth))Message+="\n[Error]: Depth Incorrect";
                        else Passed=true;
                    }
                    else if(!Int32.TryParse(data[1], out Step))
                    {
                        
                        Message+="\n[Error]: Wrong Step Number!";
                        //Set to meter instead of step
                    }
                    else if(Step%2!=0)
                    {
                        Message+="\n[Error]: Step Number can't be Odd!";
                        Step=0;
                    }
                    else if(Step<0)
                    {
                        Message+="\n[Error]: Step Number can't be Negative!";
                        Step=0;
                    }
                    else Passed=true;
                }
                else Passed=true;
                break;
            }
            case "refresh":
            {
                Mode=ScriptMode.REFRESH;
                Passed=true;
                break;
            }
            case "start":
            {
                Mode=ScriptMode.START;
                Passed=true;
                break;
            }
            case "pause":
            {
                Mode=ScriptMode.PAUSE;
                Passed=true;
                break;
            }
            case "reset":
            {
                Mode=ScriptMode.RESET;
                Passed=true;
                break;
            }
            case "dig":
            {
                Mode=ScriptMode.DIG;
                Passed=true;
                break;
            }
            default:
            {
                Mode=ScriptMode.DEFAULT;
                Message+="\n[Error]: Mode doesn't found!";
                break;
            }
        }
    }
};

public enum ScriptMode
{
    SET,
    REFRESH,
    START,
    PAUSE,
    RESET,
    DIG,
    DEFAULT
}

public class EventWatcher
{
    public bool IsSet;
    public bool FinishedTimerIsSet;
    public bool StartTimerIsSet;
    public bool PauseTimerIsSet;

    IMyTimerBlock FinishedTimer;
    IMyTimerBlock StartTimer;
    IMyTimerBlock PauseTimer;

    public EventWatcher()
    {
        FinishedTimerIsSet=false;
        PauseTimerIsSet=false;
        StartTimerIsSet=false;
        IsSet=false;
    }
    public void New()
    {
        FinishedTimerIsSet=false;
        StartTimerIsSet=false;
        PauseTimerIsSet=false;
        IsSet=false;
    }
    public void AddFinishedTimer(IMyTerminalBlock _block)
    {
        FinishedTimer=_block as IMyTimerBlock;
        FinishedTimerIsSet=true;
        IsSet=true;
    }
    public void AddStartTimer(IMyTerminalBlock _block)
    {
        StartTimer=_block as IMyTimerBlock;
        StartTimerIsSet=true;
        IsSet=true;
    }
    public void AddPauseTimer(IMyTerminalBlock _block)
    {
        PauseTimer=_block as IMyTimerBlock;
        PauseTimerIsSet=true;
        IsSet=true;
    }
    public void Finished()
    {
        if(FinishedTimerIsSet)FinishedTimer.StartCountdown();
    }
    public void Started()
    {
        if(StartTimerIsSet)StartTimer.StartCountdown();
    }
    public void Paused()
    {
        if(PauseTimerIsSet)PauseTimer.StartCountdown();
    }
}

//End of Declaration

public Program() 
{
    if(!GetConfig())ResetConfig();

    Load_Data();
    if(ComponentsReady)SetSystem(DebugNumber);//If Components should be ready, refresh all data about them
    else //Get the Programmable Block's screen on very first run
    {
        Message.AddMe(Me.GetSurface(0));
    }
    if(Step.Finished)
    {
        Message.State=StateType.FINISHED;
        Message.Data=ConstructBasicData();
    }
    else if(ComponentsReady)
    {
        if(ImRunning)StartScript();
        if(DigModeEnabled)//If game loads with Dig Mode Enabled, Pause the platform, just in case
        {
            Message.State=StateType.PAUSE;
            PausePlatform();
        }
    }
    Message.AutoRun=false;
    UpdateScreens();
    FirstLoad=false;
} 
 
public void Save() 
{
    if(ImRunning)
    {
        TotalTime+=DateTime.Now-StartTime;
        StartTime=DateTime.Now;
    }
    Save_Data();
} 

public void Main(string argument, UpdateType updateSource) 
{
    Message.Continue();
    if((updateSource & UpdateType.Update100)!=0)
    {
        Message.AutoRun=true;
        if(ImRunning)
        {
            if(UpdateData())
            {
                if(!Cargo.AutoPauseEnabled)
                {
                    if(!PlatformIsMoving)StartMovingParts();
                    if(StepCheck())
                    {
                        if(Step.Update())//Updated Step
                        {
                            //Mining Finished
                            FinishMining();
                        }
                        else Message.State=StateType.SETMOVINGPARTS;
                        SetMovingParts();
                    }
                    else
                    {
                        //Aligning
                        if(DigModeEnabled)Message.State=StateType.DIGGING;
                        else Message.State=StateType.ALIGNING;
                    }
                }
                else
                {
                    //Auto Pause
                    if(PlatformIsMoving)PauseMovingParts();
                    Message.State=StateType.AUTOPAUSE;
                }
            }
        }
        else Runtime.UpdateFrequency = UpdateFrequency.None;
    }
    else
    {
        Message.AutoRun=false;
        ArgumentData.New(argument);
        if(ArgumentData.Passed)
        {
            if(ArgumentData.Mode==ScriptMode.SET || ArgumentData.Mode==ScriptMode.RESET)
            {
                NewSet=true;
                if(ArgumentData.Mode==ScriptMode.RESET)ResetConfig();
                Message.State=StateType.SET;

                GetConfig();
                
                SetSystem(ArgumentData.Step);
                EnableDigMode(false);
                NewSet=false;

                if(ComponentsReady)
                {
                    Message.State=StateType.ALIGNINGSTARTINGPOSITION;
                    Message.AddReport("[System]: Ready to Start!");
                }

                PauseScript();
                TotalTime = TimeSpan.Zero;
            }
            else if(ArgumentData.Mode==ScriptMode.REFRESH)
            {
                Message.State=StateType.REFRESH;
                refresh_components(true);
                GetConfig(true);
            }
            else if(ArgumentData.Mode==ScriptMode.START)
            {
                Message.State=StateType.START;
                EnableDigMode(false);
                StartPlatform();
            }
            else if(ArgumentData.Mode==ScriptMode.PAUSE)
            {
                Message.State=StateType.PAUSE;
                PausePlatform();
            }
            else if(ArgumentData.Mode==ScriptMode.DIG)
            {
                if((updateSource & UpdateType.Trigger)!=0)
                {
                    //Message.State=StateType.START;
                    EnableDigMode();
                    StartPlatform();
                }
                else
                {
                    Message.AddReport("[Warning]: Dig mode Invalid");
                }
            }
        }
        else
        {
            Message.AddReport(ArgumentData.Message);
        }
        if(ComponentsReady)Save_Data();
    }
    UpdateScreens();
    //Echo("LRT: "+Runtime.LastRunTimeMs);
}

//End of Main

public void UpdateScreens()
{
    if(Step.Finished)Message.AddReport("Mining Time: "+TotalTime.ToString(@"hh\:mm\:ss"));

    Message.Data=ConstructBasicData();
    Message.ConstructMsg();

    if(SendMessage)
    {
        IGC.SendUnicastMessage(TransmissionReceiverAddress,MainTag,Message.Message);
        Message.Message+="\n[System]: Transmission Sent\n";
    }
    if(!Message.AutoRun || AlwaysUpdateDetailedInfo)Echo(Message.Message);

    Message.WriteToScreens();
}

public string ConstructBasicData()
{
    Result="";
    
    Result+="Step: "+Step.Value+"/"+Step.Max+" | "
    +VerticalArm.ArmTargetDistance+"/"+VerticalArm.MaxExtendableLength+"m | ETA: "
    +Step.Hours+":"+Step.Minutes.ToString("D2")+"\n";

    int i;
    Result+="Progress: [";
    for(i=0;i<Math.Round(Step.Progression/2.5f);i++)
    {
        Result+="|";
    }   
    while(i<40)
    {
        Result+="'";
        i++;
    }
    Result+="] "+Step.Progression+"%\n";
    Result+="[ "+MainTag+" ]\n";

    if(Cargo.ShowOnScreen)Result+=Cargo.ConstructMsg();
    if(ShowAdvancedData)Result+=GatherAdvancedData();

    return Result;
}

public string GatherAdvancedData()
{
    Result=""
    +"Rot-Dig: "+MainRotor.UseDigSpeed+" DSp: "+MainRotor.DigSpeed+"\n"
    +"Rotor: "+MainRotor.CurrentAngle+"°| +: "+MainRotor.RotateToMax+" | "+MainRotor.TargetSpeed+"rpm\n"
    +"H-Arm-L: "+HorizontalArm.Length+"m| H-Arm-Ext: "+HorizontalArm.ExtendableLength+"m\n"
    +"H-Arm-Max: "+HorizontalArm.MaxExtendableLength+"m| Min: "+HorizontalArm.MinExtendableLength+"m\n"
    +"V-Arm-Ext: "+VerticalArm.ExtendableLength+"m| D-Arm-L: "+DrillArm.Length+"m\n"
    +"V-Arm-Max: "+VerticalArm.MaxExtendableLength+"m| Min: "+VerticalArm.MinExtendableLength+"m\n"
    +"HStep-L: "+HorizontalArm.StepLength+"m| VStep-L: "+VerticalArm.StepLength+"m\n"
    +"HStep: "+Step.H+"/"+Step.MaxH+"| VStep: "+Step.V+"/"+Step.MaxV+"| ExtH: "+Step.ExtendH+"\n"
    +"H-Arm-T: "+HorizontalArm.ArmTargetDistance+"m| V-Arm-T: "+VerticalArm.ArmTargetDistance+"m\n"
    +"DbZ: "+MainRotor.DebugZoneLowDeg+"°| "+MainRotor.DebugZoneHighDeg+"°| Pass: "+MainRotor.PassedDebugZone+"\n"
    +"AtP: "+Cargo.AutoPauseEnabled+" |HT: "+Cargo.HighTarget+"|R-ConT: "+MainRotor.ConstantTensor+"\n"
    +"HStp: "+Step.Horizontal+" | Fst: "+Step.First+"| TrC: "+MainRotor.TensorTimer+"\n"
    +"InPos: R:"+MainRotor.IsInPosition+" | H:"+HorizontalArm.IsInPosition+"| V:"+VerticalArm.IsInPosition+"\n"
    +"H-A-Spd: "+HorizontalArm.ActualSpeed+" | V-A-Spd: "+VerticalArm.ActualSpeed+"\n"
    +"CmpR: "+ComponentsReady+"|ImR: "+ImRunning+"|PiM: "+PlatformIsMoving+"\n"
    //+"MxT: "+Step.MaxTime+"s|CrT: "+Step.CurrentTime+"s\n"
    //+"R-IR: "+MainRotor.InnerRadius()+" | AFRT: "+Step.AFullRotationTime+"\n"
    //+"V-ExT: "+VerticalArm.StepExtensionTime()+"\n"
    //+"H-ExT: "+HorizontalArm.StepExtensionTime()+"\n"
    //+"STime: "+StartTime.ToString("T")+"\n"
    //+"TTime: "+TotalTime.ToString("c")+"\n"
    +"";
    return Result;
}

public string ComponentReport()
{
    Result="";

    Result+="Rotor: Found | Drills: "+DrillArm.Drills.Count+"\n";
    int p_count=0;
    int p_inv_count=0;
    for(int i=0;i<HorizontalArm.Pistons.Count;i++)
    {
        if(HorizontalArm.Pistons[i].Inverted)p_inv_count++;
        else p_count++;
    }
    if(HorizontalArm.Pistons.Count>0)Result+="Horizontal P: "+p_count+" | Inverted: "+p_inv_count+"\n";
    p_inv_count=0;
    p_count=0;
    for(int i=0;i<VerticalArm.Pistons.Count;i++)
    {
        if(VerticalArm.Pistons[i].Inverted)p_inv_count++;
        else p_count++;
    }
    if(VerticalArm.Pistons.Count>0)Result+="Vertical P: "+p_count+" | Inverted: "+p_inv_count+"\n";
    if(Message.Screens.Count>1)Result+="Screens: "+(Message.Screens.Count-1)+"\n";
    if(UseCargoModule)Result+="Cargo: "+Cargo.Count+"\n";
    if(TimerEvent.IsSet)
    {
        Result+="Timers: ";
        if(TimerEvent.FinishedTimerIsSet)Result+="| Finished";
        if(TimerEvent.StartTimerIsSet)Result+=" | Start";
        if(TimerEvent.PauseTimerIsSet)Result+=" | Stop";
        Result+="\n";
    }
    if(SendMessage)
    {
        Result+="[System]: Transmission Enabled\n";
        if(!AntennaFound)Result+="[Warning]: Antenna Error!\n";
    }
    Result+="\n[System]: Components Ready!";
    
    return Result;
}

public void refresh_components(bool optionalOnly=false)
{
    bool rotor_found=false;
    bool drill_found=false;

    blocks.Clear();
    GridTerminalSystem.SearchBlocksOfName(MainTag,blocks);

    if(!optionalOnly)
    {
        VerticalArm.New();
        HorizontalArm.New();
        DrillArm.New();
        
        IMyAttachableTopBlock top;
        for(int i=0;i<blocks.Count;i++)
        {
            if(!rotor_found && blocks[i] is IMyMotorAdvancedStator)
            {
                MainRotor.New(blocks[i] as IMyMotorStator);
                if(ShareInertiaTensor || DynamicRotorTensor)
                {
                    MainRotor.EnableTensor();
                }
                rotor_found=true;
                break;
            }
        }
        if(rotor_found)
        {
            double VectorDot=0;
            
            top = MainRotor.Rotor.Top;
            Vector3D RotorVector = MainRotor.Position;
            Vector3D BaseVector = MainRotor.DirectionVector;
            BaseVector.Normalize();
            Vector3D CheckVector;
            IMyPistonBase piston;
            ITerminalAction Tensor=MainRotor.InertiaTensor;
            for(int i=0;i<blocks.Count;i++)
            {
                if(blocks[i] is IMyPistonBase)
                {
                    piston=blocks[i] as IMyPistonBase;
                    if(ShareInertiaTensor)
                    {
                        if(!piston.GetValueBool("ShareInertiaTensor"))
                        {
                            Tensor.Apply(piston);
                        }
                    }
                    if(piston.CustomName.Contains(VerTag))
                    {
                        if(piston.CustomName.Contains(InvTag))VerticalArm.Pistons.Add(new PistonBlock(piston,true));
                        else VerticalArm.Pistons.Add(new PistonBlock(piston));
                    }
                    else if(piston.CustomName.Contains(HorTag))
                    {
                        if(piston.CustomName.Contains(InvTag))HorizontalArm.Pistons.Add(new PistonBlock(piston,true));
                        else HorizontalArm.Pistons.Add(new PistonBlock(piston));
                    }
                    else if(SmartDetection)
                    {
                        top = piston.Top;
                        CheckVector = piston.GetPosition();
                        CheckVector = CheckVector - top.GetPosition();
                        CheckVector.Normalize();

                        VectorDot=Vector3D.Dot(BaseVector,CheckVector);
                        if(VectorDot>0.9f)
                        {
                            if(piston.CustomName.Contains(InvTag))VerticalArm.Pistons.Add(new PistonBlock(piston,true));
                            else VerticalArm.Pistons.Add(new PistonBlock(piston));
                        }
                        else if (VectorDot<0.1f && VectorDot>-0.1f)
                        {
                            if(piston.CustomName.Contains(InvTag))HorizontalArm.Pistons.Add(new PistonBlock(piston,true));
                            else HorizontalArm.Pistons.Add(new PistonBlock(piston));
                        }
                        else if(VectorDot<0.9f)
                        {
                            if(piston.CustomName.Contains(InvTag))VerticalArm.Pistons.Add(new PistonBlock(piston,true));
                            else VerticalArm.Pistons.Add(new PistonBlock(piston));
                        }
                    }
                }
                else if(blocks[i] is IMyShipDrill)
                {
                    DrillArm.Drills.Add(blocks[i] as IMyShipDrill);
                    drill_found=true;
                }
            }
            if(!drill_found)
            {
                Message.AddReport("[Error]: Drill Not Found!");
                ComponentsReady=false;
            }
            else
            {
                ComponentsReady=true;
                DrillArm.Init(ManualDrillArmLength,RotorVector);
                VerticalArm.Init(DrillArm.FurthestVector,SmartDetection,MinVerticalLimit,MaxVerticalLimit);
                HorizontalArm.Init(DrillArm.FurthestVector,SmartDetection,MinHorizontalLimit,MaxHorizontalLimit);
            }
        }
        else 
        {
            ComponentsReady=false;
            Message.AddReport("[Error]: Rotor Not Found!");
        }
    }

    TimerEvent.New();
    Message.New(Me.GetSurface(0));

    for(int i=0;i<blocks.Count;i++)
    {
        block = blocks[i];
        if(block is IMyTextSurfaceProvider)
        {
            Message.AddToScreens(block);
        }
        else if(block is IMyTimerBlock)
        {
            if(block.CustomName.Contains(FinishedTimerTag))
            {
                if(TimerEvent.FinishedTimerIsSet)Message.AddReport("[Warning]: Multiple Timers of same Type!");
                else TimerEvent.AddFinishedTimer(block);
            }
            if(block.CustomName.Contains(StartTimerTag))
            {
                if(TimerEvent.StartTimerIsSet)Message.AddReport("[Warning]: Multiple Timers of same Type!");
                else TimerEvent.AddStartTimer(block);
            }
            if(block.CustomName.Contains(PauseTimerTag))
            {
                if(TimerEvent.PauseTimerIsSet)Message.AddReport("[Warning]: Multiple Timers of same Type!");
                else TimerEvent.AddPauseTimer(block);
            }
        }
    }
    

    

    if(ComponentsReady)
    {
        if(TransmissionReceiverAddress!=0)
        {
            SendMessage=true;
            if(IGC.IsEndpointReachable(TransmissionReceiverAddress,TransmissionDistance.AntennaRelay))AntennaFound=true;
            else AntennaFound=false;
            
        }
        
        
        else SendMessage=false;

        Cargo.New(blocks,UseCargoContainersOnly,HighCargoLimit,LowCargoLimit);
        UseCargoModule=Cargo.ShowOnScreen;
        if(UseAutoPause)
        {
            Cargo.CheckIfFilled();
            if(Cargo.AutoPauseEnabled)
            {
                if(!NewSet && !FirstLoad && PlatformIsMoving)PauseMovingParts();
                Message.State=StateType.AUTOPAUSE;
            }
            else if(!NewSet && !FirstLoad && !PlatformIsMoving)StartMovingParts();
        }

        if(!FirstLoad)Message.AddReport(ComponentReport());
    }
    else Message.AddReport("[System]: Missing Components!");
}

public void EnableDigMode(bool _enable=true)
{
    if(ComponentsReady)
    {
        if(DigModeEnabled!=_enable)
        {
            DigModeEnabled=_enable;
            MainRotor.UseDigSpeed=_enable;
            VerticalArm.UseDigSpeed=_enable;
            HorizontalArm.UseDigSpeed=_enable;
            DrillArm.Enable(!_enable);

            if(DigModeEnabled)
            {
                if(!Step.DigMode)
                {
                    Step.DigModeChange(
                        Math.Ceiling(HorizontalArm.ExtendableLength/HorizontalArm.DigStepLength),
                        Math.Ceiling(VerticalArm.ExtendableLength/VerticalArm.DigStepLength)
                    );

                    Step.SetToVerticalStep(VerticalArm.GetVStepFromDistance(Step.MaxV));

                    Step.NewETA(
                        VerticalArm.StepExtensionTime(),
                        HorizontalArm.StepExtensionTime(),
                        HorizontalArm.Length,
                        HorizontalArm.DigStepLength,
                        MainRotor.InnerRadius(),
                        MainRotor.DigSpeed);
                }
            }
            else
            {
                if(Step.DigMode)
                {
                    Step.DigModeChange(
                        Math.Ceiling(HorizontalArm.ExtendableLength/HorizontalArm.StepLength),
                        Math.Ceiling(VerticalArm.ExtendableLength/VerticalArm.StepLength)
                    );

                    Step.SetToVerticalStep(VerticalArm.GetVStepFromDistance(Step.MaxV));

                    Step.NewETA(
                        VerticalArm.StepExtensionTime(),
                        HorizontalArm.StepExtensionTime(),
                        HorizontalArm.Length,
                        HorizontalArm.StepLength,
                        MainRotor.InnerRadius(),
                        MainRotor.Speed);
                }
            }
            SetMovingParts();
        }
    }
    else Message.AddReport("[Warning]: Components Not Ready for Dig Mode!");
}

public void SetMovingParts()
{
    if(FirstLoad)
    {
        Load_Data(false,false);

        HorizontalArm.SetToTarget(Step.H,Step.ExtendH);
        VerticalArm.SetToTarget(Step.V);
        if(!Step.Finished)
        {
            MainRotor.SetToTarget(HorizontalArm.EffectiveTargetDistance(),NewSet);
            MainRotor.SetSpeed(HorizontalArm.EffectiveTargetDistance());
        }
        if(!Step.Odd)MainRotor.EnableTensorStatic();
    }
    else if(Step.Finished)
    {
        if(!DigModeEnabled)
        {
            VerticalArm.SetToTarget(Step.V);
            HorizontalArm.SetToTarget(Step.H,Step.ExtendH);
        }
        MainRotor.EnableTensorStatic();
    }
    else if(Step.Odd)
    {
        MainRotor.SetToTarget(HorizontalArm.EffectiveTargetDistance(),NewSet);
        MainRotor.EnableTensorStatic(false);
    }
    else
    {
        if(Step.First)
        {
            VerticalArm.SetToTarget(Step.V);
            HorizontalArm.SetToTarget(Step.H,Step.ExtendH);
            MainRotor.SetToTarget(HorizontalArm.EffectiveTargetDistance(),Step.First);
        }
        else
        {
            if(Step.Horizontal)HorizontalArm.SetToTarget(Step.H,Step.ExtendH);
            else if(AlwaysRetractHorizontalPistons)
            {
                HorizontalArm.SetToTarget(Step.H,Step.ExtendH);
                VerticalArm.SetToTarget(Step.V);
                VerticalArm.Enable(false);
            }
            else VerticalArm.SetToTarget(Step.V);
        }
        MainRotor.EnableTensorStatic();
    }
    Step.UpdateETA();
}

public void StartMovingParts()
{
    VerticalArm.Enable();
    HorizontalArm.Enable();
    if(DigModeEnabled)DrillArm.Enable(false);
    else DrillArm.Enable();
    MainRotor.Rotor.Enabled=true;
    if(!NewSet && !FirstLoad)TimerEvent.Started();

    PlatformIsMoving=true;
    if(Step.Odd)MainRotor.EnableTensorStatic(false);
}

public void PauseMovingParts()
{
    VerticalArm.Enable(false);
    HorizontalArm.Enable(false);
    DrillArm.Enable(false);
    MainRotor.Rotor.Enabled=false;
    TimerEvent.Paused();

    PlatformIsMoving=false;
    MainRotor.EnableTensorStatic();
}

public void FinishMining()
{
    DrillArm.Enable(false);
    PauseScript();
    Message.State=StateType.FINISHED;
    PlatformIsMoving=false;
    TimerEvent.Finished();
    //MainRotor.EnableTensorStatic(); Done in SetMovingParts
    Save_Data(); 
}

public void StartScript()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    ImRunning=true;
    StartTime=DateTime.Now;
}

public void PauseScript()
{
    Runtime.UpdateFrequency = UpdateFrequency.None;
    ImRunning=false;
    TotalTime+=DateTime.Now-StartTime;
}

public void SetSystem(int _step=0)
{
    refresh_components();
    if(ComponentsReady)
    {
        VerticalArm.SetStepLength(VpStepLength);
        if(AdaptiveHorizontalExtension)HorizontalArm.SetStepLength(DrillArm.Length,true);
        else HorizontalArm.SetStepLength(HpStepLength);

        Step.New(
            _step,
            Math.Ceiling(HorizontalArm.ExtendableLength/HorizontalArm.StepLength),
            Math.Ceiling(VerticalArm.ExtendableLength/VerticalArm.StepLength),
            AlwaysRetractHorizontalPistons,
            NewSet
        );

        if(NewSet && ArgumentData.Depth!=0)
        {
            Step.SetToVerticalStep(VerticalArm.GetVStepFromDistance(ArgumentData.Depth,Step.MaxV));
        }

        MainRotor.Init(MaxRotorAngle,MinRotorAngle);

        Step.NewETA(
                VerticalArm.StepExtensionTime(),
                HorizontalArm.StepExtensionTime(),
                HorizontalArm.Length,
                HorizontalArm.StepLength,
                MainRotor.InnerRadius(),
                MainRotor.Speed);

        SetMovingParts();
        if(NewSet)StartMovingParts();
        if(FirstLoad)
        {
            if(PlatformIsMoving)
            {
                if(Step.Odd)MainRotor.EnableTensorStatic(false);
            }
            else
            {
                MainRotor.EnableTensorStatic();
            }
        }
    }
}

public bool StepCheck()
{
    StepCheckResult=false;
    if(Step.Odd)
    {
        if(!Step.First)MainRotor.SetLimits();
        if(MainRotor.IsInPosition)StepCheckResult=true;
        else MainRotor.CheckTarget();
    }
    else if(Step.First)
    {
        if(HorizontalArm.IsInPosition && VerticalArm.IsInPosition && MainRotor.IsInPosition)StepCheckResult=true;
        else 
        {
            HorizontalArm.CheckTarget();
            VerticalArm.CheckTarget();
            MainRotor.CheckTarget();
        }
    }
    else if(Step.Horizontal)
    {
        if(HorizontalArm.IsInPosition)StepCheckResult=true;
        else HorizontalArm.CheckTarget();
    }
    else if(AlwaysRetractHorizontalPistons)
    {
        if(HorizontalArm.IsInPosition)
        {
            VerticalArm.Enable();
            if(VerticalArm.IsInPosition)StepCheckResult=true;
            else VerticalArm.CheckTarget();
        }
        else HorizontalArm.CheckTarget();
    }
    else
    {
        if(VerticalArm.IsInPosition)StepCheckResult=true;
        else VerticalArm.CheckTarget();
    }
    return StepCheckResult;
}

public bool IntegrityCheck()
{
    if(MainRotor.IntegrityTest() && DrillArm.IntegrityTest() && VerticalArm.IntegrityTest() && HorizontalArm.IntegrityTest())return true;
    else return false;
}

public bool UpdateData()
{
    if(IntegrityCheck())
    {
        if(ShowAdvancedData)
        {
            MainRotor.Update();
        }
        if(DynamicRotorTensor)MainRotor.UpdateTensor();

        if(UseAutoPause || UseCargoModule)Cargo.Update();
        if(UseAutoPause)Cargo.CheckIfFilled();
        return true;
    }
    else
    {
        Message.State=StateType.EMERGENCY;
        ComponentsReady=false;
        EmergencyStop();
        return false;
    }
}

public void EmergencyStop()
{
    Message.AddReport(MainRotor.EmergencyStop()+DrillArm.EmergencyStop()+VerticalArm.EmergencyStop()+HorizontalArm.EmergencyStop());
    PauseScript();
}

public void StartPlatform(bool auto=false)
{
    if(!ImRunning)
    {
        if(ComponentsReady)
        {
            if(Step.Progression<100)
            {
                if(auto)
                {
                    StartScript();
                    StartMovingParts();
                    Message.AddReport("[System]: Mining Started!");

                }
                else if(UpdateData())
                {
                    SetMovingParts();
                    StartScript();
                    StartMovingParts();
                    Cargo.HighTarget=true;
                    Message.AddReport("[System]: Mining Started!");
                }
                else Message.AddReport("[System]: Platform must be reset before Start!");
            }
            else Message.AddReport("[System]: Platform must be reset before Start!");
        }
        else Message.AddReport("[System]: Platform must be set before Start!");
    }
    else if(UseAutoPause && Cargo.AutoPauseEnabled)
    {
        Cargo.HighTarget=true;
        Cargo.Update();
        Cargo.CheckIfFilled();
        if(!Cargo.AutoPauseEnabled)
        {
            StartMovingParts();
            Message.AddReport("[System]: Mining Started!");
        }
        else Message.AddReport("[System]: Cargo is Filled!");
    }
    else Message.AddReport("[System]: Script is already running!");
}

public void PausePlatform()
{
    if(ImRunning)
    {
        PauseScript();

        if(ComponentsReady)
        {
            PauseMovingParts();
        }

        Message.AddReport("[System]: Mining Paused!");
    }
    else 
    {
        if(ComponentsReady)
        {
            PauseMovingParts();
        }
    }
}

public void Save_Data()
{
    _ini.Clear();
    _ini.Set("Primary","Version",Version);
    _ini.Set("Primary","ComponentsReady",ComponentsReady);
    _ini.Set("Primary","ImRunning",ImRunning);
    _ini.Set("Primary","PlatformIsMoving",PlatformIsMoving);
    _ini.Set("Primary","TotalTime",TotalTime.Ticks);
    _ini.Set("Primary","DigModeEnabled",DigModeEnabled);
    
    if(Step!=null)
    {
        _ini.Set("Primary","Step.Value",Step.Value);
        _ini.Set("Secondary","Step.ExtendH",Step.ExtendH);
    }
    if(MainRotor!=null)
    {
        _ini.Set("Secondary","MainRotor.RotateToMax",MainRotor.RotateToMax);
        _ini.Set("Secondary","MainRotor.PassedDebugZone",MainRotor.PassedDebugZone);
    }
    if(Cargo!=null)_ini.Set("Secondary","Cargo.HighTarget",Cargo.HighTarget);

    Storage=_ini.ToString();

    Message.AddReport("[System]: Data Saved");
}

public void Load_Data(bool primary=true,bool first=true)
{
    if(first)
    {
        if(Storage=="" || !_ini.TryParse(Storage, out IniResult))Message.AddReport("[System]: Load Failed!");
    }
    
    if(IniResult.IsDefined && IniResult.Success)
    {
        if(primary)
        {
            Message.AddReport("[System]: Loading Primary Data...");

            if(!_ini.Get("Primary","ComponentsReady").TryGetBoolean(out ComponentsReady))Message.AddReport("[Load Error]: ComponentsReady");
            if(!_ini.Get("Primary","ImRunning").TryGetBoolean(out ImRunning))Message.AddReport("[Load Error]: ImRunning");
            if(!_ini.Get("Primary","PlatformIsMoving").TryGetBoolean(out PlatformIsMoving))Message.AddReport("[Load Error]: PlatformIsMoving");

            if(!_ini.Get("Primary","DigModeEnabled").TryGetBoolean(out DigModeEnabled))Message.AddReport("[Load Error]: DigModeEnabled");

            long ticks=0;
            if(!_ini.Get("Primary","TotalTime").TryGetInt64(out ticks))Message.AddReport("[Load Error]: TotalTime");
            else TotalTime = TimeSpan.FromTicks(ticks);

            if(ComponentsReady)
            {
                if(!_ini.Get("Primary","Step.Value").TryGetInt32(out DebugNumber))Message.AddReport("[Load Error]: Step.Value");
                if(DigModeEnabled)EnableDigMode();
            }
        }
        else
        {
            Message.AddReport("[System]: Loading Secondary Data...");
            if(Step.IsSet)
            {
                if(!_ini.Get("Secondary","Step.ExtendH").TryGetBoolean(out Step.ExtendH))Message.AddReport("[Load Error]: Step.ExtendH");
            }
            if(MainRotor.IsSet)
            {
                if(!_ini.Get("Secondary","MainRotor.RotateToMax").TryGetBoolean(out MainRotor.RotateToMax))Message.AddReport("[Load Error]: MainRotor.RotateToMax");
                if(!_ini.Get("Secondary","MainRotor.PassedDebugZone").TryGetBoolean(out MainRotor.PassedDebugZone))Message.AddReport("MainRotor.PassedDebugZone");
            }
            if(Cargo.IsSet)
            {
                if(!_ini.Get("Secondary","Cargo.HighTarget").TryGetBoolean(out Cargo.HighTarget))Message.AddReport("[Load Error]: Cargo.HighTarget");
            }
        }
    }
}

public void ResetConfig()
{

    MainTag="/Mine 01/";
    MaxRotorAngle=360;
    MinRotorAngle=0;

//Quick Updateable ---
    UseAutoPause=true;
    HighCargoLimit=0.9f;
    LowCargoLimit=0.5f;
    UseCargoContainersOnly=true;
    ShowAdvancedData=true;
    LcdColorCoding=true;
    DynamicRotorTensor=true;
    AlwaysUpdateDetailedInfo=false;

    TransmissionReceiverAddress=0;

    HorizontalExtensionSpeed=0.5f;
    VerticalExtensionSpeed=0.5f;
    RotorSpeedAt10m=0.5f;

    DigModeSpeed=3f;

//Hard Updateable ---
    SmartDetection=true; 
    AlwaysRetractHorizontalPistons=false;
    ShareInertiaTensor=true;

    AdaptiveHorizontalExtension=true;
    HpStepLength=3.33f;
    VpStepLength=2.5f;

    ManualDrillArmLength=0f;


    MinHorizontalLimit=0;
    MaxHorizontalLimit=0;

    MinVerticalLimit=0;
    MaxVerticalLimit=0;

    VerTag="/Ver/";
    HorTag="/Hor/";
    InvTag="/Inv/";
    StartTimerTag="/Start/";
    PauseTimerTag="/Pause/";
    FinishedTimerTag="/Finished/";

    SetConfig();
    Message.AddReport("[System]: Configuration Reset");
}

public void SetConfig()
{
    Me.CustomData=""
    +"[Mining Platform Configuration]\n"
    +"Version="+Version+"\n"
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

public bool GetConfig(bool optionalOnly=false)
{
    if(Me.CustomData!="" && _ini.TryParse(Me.CustomData, out IniResult))
    {
        if(!optionalOnly)
        {
            MainTag=_ini.Get("Highlighted Options","MainTag").ToString();
            Message.MainTag=MainTag;

            MaxRotorAngle=_ini.Get("Highlighted Options","MaxRotorAngle").ToSingle();
            MinRotorAngle=_ini.Get("Highlighted Options","MinRotorAngle").ToSingle();

            SmartDetection=_ini.Get("Advanced Options","SmartDetection").ToBoolean();
            AlwaysRetractHorizontalPistons=_ini.Get("Advanced Options","AlwaysRetractHorizontalPistons").ToBoolean();
            ShareInertiaTensor=_ini.Get("Advanced Options","ShareInertiaTensor").ToBoolean();

            MinHorizontalLimit=_ini.Get("Advanced Options","MinHorizontalLimit").ToSingle();
            MaxHorizontalLimit=_ini.Get("Advanced Options","MaxHorizontalLimit").ToSingle();

            MinVerticalLimit=_ini.Get("Advanced Options","MinVerticalLimit").ToSingle();
            MaxVerticalLimit=_ini.Get("Advanced Options","MaxVerticalLimit").ToSingle();

            VerTag=_ini.Get("Advanced Options","VerTag").ToString();
            HorTag=_ini.Get("Advanced Options","HorTag").ToString();
            InvTag=_ini.Get("Advanced Options","InvTag").ToString();
            StartTimerTag=_ini.Get("Advanced Options","StartTimerTag").ToString();
            PauseTimerTag=_ini.Get("Advanced Options","PauseTimerTag").ToString();
            FinishedTimerTag=_ini.Get("Advanced Options","FinishedTimerTag").ToString();
        }
        TransmissionReceiverAddress=_ini.Get("Quick Options","TransmissionReceiverAddress").ToInt64();

        UseAutoPause=_ini.Get("Quick Options","UseAutoPause").ToBoolean();
        HighCargoLimit=_ini.Get("Quick Options","HighCargoLimit").ToSingle();
        LowCargoLimit=_ini.Get("Quick Options","LowCargoLimit").ToSingle();
        UseCargoContainersOnly=_ini.Get("Quick Options","UseCargoContainersOnly").ToBoolean();

        ShowAdvancedData=_ini.Get("Quick Options","ShowAdvancedData").ToBoolean();

        LcdColorCoding=_ini.Get("Quick Options","LcdColorCoding").ToBoolean();
        Message.LcdColoring=LcdColorCoding;

        DynamicRotorTensor=_ini.Get("Quick Options","DynamicRotorTensor").ToBoolean();
        AlwaysUpdateDetailedInfo=_ini.Get("Quick Options","AlwaysUpdateDetailedInfo").ToBoolean();

        RotorSpeedAt10m=_ini.Get("Quick Options","RotorSpeedAt10m").ToSingle();
        HorizontalExtensionSpeed=_ini.Get("Quick Options","HorizontalExtensionSpeed").ToSingle();
        VerticalExtensionSpeed=_ini.Get("Quick Options","VerticalExtensionSpeed").ToSingle();
        
        DigModeSpeed=_ini.Get("Quick Options","DigModeSpeed").ToSingle();

        if(!FirstLoad && !NewSet)
        {
            MainRotor.QuickInit(RotorSpeedAt10m,DigModeSpeed,HorizontalArm.EffectiveTargetDistance(),true);
            HorizontalArm.QuickInit(HorizontalExtensionSpeed,DigModeSpeed,true);
            VerticalArm.QuickInit(VerticalExtensionSpeed,DigModeSpeed,true);
        }
        else
        {
            MainRotor.QuickInit(RotorSpeedAt10m,DigModeSpeed);
            HorizontalArm.QuickInit(HorizontalExtensionSpeed,DigModeSpeed);
            VerticalArm.QuickInit(VerticalExtensionSpeed,DigModeSpeed);
        }

        Message.AddReport("[System]: Configuration loaded...");
        return true;
    }
    else
    {
        Message.AddReport("[System]: Configuration failed!");
        return false;
    }
}