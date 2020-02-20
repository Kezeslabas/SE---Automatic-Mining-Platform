//Automatic Mining Platform v3.712 by Kezeslabas                        Updated up until Space Engineers v1.193.1
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//  This script manages a Rotor, Pistons and Drills to create an Automatic Mining Platform.
//  It has multiple additional features to allow the build of advanced mining systems.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Hotfix (v3.712): Fixed the issue with the Antenna, and the Transmitting Progression feature is enabled again.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Hotfix (v3.711): Transmitting Progression feature is temporary disabled, due to missing property
//                         of the Antenna in Space Engineers update v1.193.1
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Change Log (v3.71):     - Minor changes to how to transmitt the progression
//                                             - The Antenna Message Receiver script got updated (Now it can both send & receive)
//                                                     -The link is still the same: https://steamcommunity.com/sharedfiles/filedetails/?id=1705183500
//                                             - The guide on how to transmit the progression has been updated accordingly.
//                                                     - Now you have to add the Main Tag of this script to the name of the LCD that
//                                                        you want to use on the orther grid.
//                                                     - Also, you doesn't have to add the LCD to the Receiver script anymore,
//                                                        it will tries to find it by the Main Tag of this script.
//                                                     - The address of the Receiver script is automatically written out to it's Custom Data,
//                                                        so you doesn't have to use the "get address" command anymore.
//                                                     - You still have to start the listening with the "start" command on the Recevier end.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//QUICK SETUP
// 1.) Renaming
// 2.) Basic Configuration
// 3.) How to use
// 4.) (Optional) Advanced Configuration
// 5.) (Optional) Features

///////////////////////
// 1.) Renaming
//     You'll have 2+1 things to do:

//       1/1.) Rename the components of the platform that you want to use, so they'll contain the Main Tag.

//                 Main Tag default: "/Mine 01/"
//                 (You can change this in the Costum Data of the Programamble Block.)
//                 (Use 2 "/" Character in order to make sure that your tag is unique.)

//                  The Components of the platform:

//                  Basic Components:
//                      Advanced Rotor (Exactly 1)
//                      Drill(s) (1 or more)
//                      (Optional) Horizontal Piston(s) (0 or more)               <- The script works perfectly even if there is no Horizontal Piston 
//                      (Optional) Vertical Piston(s) (0 or more)                   <- The script works perfectly even if there is no Vertical Piston
//                                                                                         ( In fact, the script works without any pistons, it's just no point to do that. :D )

//      Advanced Rotor
//                  |
//                 O = Horizontal Piston(s) = O
//                                                            ||
//                                                      Vertical
//                                                     Piston(s)
//                                                            ||
//                                                        Drill(s)
//

//                  Advanced Components:
//                      (Optional) LCD/Text Panel(s) (0 or more)               <- Displays the progression.
//                      (Optional) Antenna (0 or 1)                                      <- Can broadcast the progression to another grid.
//                      (Optional) Timer Block [Basic] (0 or 1)                   <- It is started when the mining is finished.   
//                      (Optional) Cargo Module (0 or more)                      <- It can be any number of blocks that has inventory.
//                                                                                                           Their filling % is gonna be displayed.
//                                                                                                           Also, the Auto Stop/Start feature's gonna use these for reference if
//                                                                                                           there is any Cargo Blocks added, instead of the Drills.
//                      (Optional) Timer Block [Advanced] (0 or 1)             <- It's tarted when the Auto Pause applies.
//                                                                                                           In addition to the Main Tag, add the "/Adv/" tag to it as well.
//                      (Optional) Other LCD Block(s) (0 or more)              <- Blocks, like the Cockpit.                                            (NEW)
//                                                                                                           In addition to the renaming:
//                                                                                                           Write the "@Number MainTag" to the CustomData of the block,
//                                                                                                           where the Number is the screen you want to use, starting from 0.
//                                                                                                           Example: "@0 /Mine 01/"
//                                                                                                           Always write it to a new line, and do not write 
//                                                                                                           anything else to that line.
//                                                                                                           After this, use the "Refresh" command on the Programmable Block,
//                                                                                                           to apply it.

//       1/2.) Add the correct Piston Tags to the names of the Hotizontal and Vertical Pistons 
                        
//                  Piston Tags:
//                  Horizontal Piston(s): "/Hor/"
//                  Vertical Piston(s): "/Ver/"

//       1/2+1.) If you want to use a Vertical Piston in the opposite way, then add the Inverse Tag to it's name as well.

//                  Inverse Tag: "/Inv/"

//                  The Inverse mode for Horizontal Pistons is not available, but let me know if you would like it to be implemented.

//////////
//    Example component names:
//              "Advanced Rotor/Mine 01/"
//              "Drill/Mine 01/"
//              "Corner LCD Top/Mine 01/"
//              "Piston/Mine 01/Hor/"
//              "Piston/Mine 01/Ver/"
//              "Piston 2/Mine 01/Ver/Inv/"
//              etc...

//////////
//Notes:
//    The best ratios for the Horizontal Pistons/Drills (For Large Grid)
//          - 1 Horizontal Piston
//                      Resource efficient: 3 or more Drills
//                      Fastest: 4 Drills
//          - 2 Horizontal Pistons
//                      Resource efficient: 5 or more Drills
//                      Fastest: 8 Drills
//          - 3 Horizontal Pistons
//                      Resource efficient: 7 or more Drills
//                      Fastest: 12 Drills

//     If you have any other blocks outside of Pistons in the Horizontal axis, like a coveyor arm or some other structure, then
//     check the 2.) Basic Configuration section.

//    The larger your mining platform is, the higher the chance of that the Rotor is gonna act strangely. 
//    The Rotor could stop for minutes, and turn with a realy slow rate, yet the values are set correctly.
//    This is a game thing, not the script's doing. 

//    The script handles 1 Mining Platform at a time.
//    You can use multiple scripts in the same grid, if you change the Main Tag.
//    Like "/Mine 02/", "/Mine 03/", etc...

///////////////////////
// 2.) Basic Configuration
//      The system is optimized trough multiple tests, with stability and efficiency in mind.

//      You can configure the script inside the Programmable Block's Custom Data.

//                  - If you don't want to draw a full circle with the rotor:
//                              - Change the Max or Min Rotor Angle.
//                                  - Use numbers between -358 and 358. (It's for stability reasons...)
//                                  - Make Sure that the Max is always bigger than the Min Rotor Angle!
//                  - If you want to disable the Auto Pause/Start feature, or you want to costumize the Pausing and Starting thresholds:
//                              - Change the Use Auto Pause to False, and change the High and the Low Cargo Threshold. 
//                  - If you want to see additional information about the running on the LCDs and in the detailed info.
//                              - Change the Show Advanced Data to True.
//                                (It's not recommended for servers.)

//                  - If you want to disable the Color Coding for the LCDs:
//                              - Change the Use LCD Color Coding to False.
//                  - If you want to disable the Share Inertia Tensor feature for Pistons and Rotors:
//                              - Change the Use Share Inertia Tensor to False.
//                  - If you want to disable the Dynamic Rotor Tensor feature:
//                              - Change the Use Dynamic Rotor Inertia Tensor to False.

//                  - If you want to Broadcast the Progression to another Grid:
//                              - You need an Antenna properly renamed in the grid for this feature to function.
//                              - Also, you'll need a receiver in the other grid.
//                                  - Use my script called Antenna Message Sender & Receiver by Kezeslabas for that.
//                                      - Workshop Link: https://steamcommunity.com/sharedfiles/filedetails/?id=1705183500
//                                         - Load it to a Programmable Block and click Check Code
//                                         - Run it with the "start" argument to start listening for messages.
//                                         - Add the Main Tag of this mining script to an LCD's name in the Receiver grid
//                                             - Run the Receiver script with the "refresh" argument
//                                         - Open the Custom Data of the Receiver's Programmable Block
//                                             - Copy the Address of that block to this block's Custom Data, to the Transmission Receiver Address
//                                             - Run this script with the "refresh" command
//                              - Make sure, that the connected Antenna's range is large enough to reach the Antenna in the Receiver's grid.
//                              - It's Done!

//                  - If you want to use Non-Piston Blocks in the Horizontal Arm:
//                              - Change the Non-Piston Blocks in Rotating Arm in Meters.
//                                  - A Large grid block counts as 2.5 Meters. 
//                                  - The default value is 5, which represents the two Conveyor Blocks that's normaly there.
//                  - If you want to use unique extension/retraction distances for the pistons:
//                              - Change the Vertical Piston Step Length.
//                              - Change the Use Unique Horizontal Step Length to True, then change the Horizontal Pistons Step Length.
//                                  - Both Step Lengths are accumulated values.
//                   - If you want to retract the Horizontal Pistons before a Vertical Extension would take place.
//                              - Change the Retract Horizontal Piston before Vertica Step to True.

//      If you did something wrong, you will see the "Configuration Error!" message.
//      You will also see some information about what you did wrong in the detailed info of the programmable block.

//      You can reset the script and the configuration if you 
//      delete the Custom Data and reload the script fom the workshop.

///////////////////////
// 3.) How to use
///////////
//  3/1.  Set
//  3/2. Start
//  3/3. Pause
//  3/4. Refresh
//  3/5. Advanced Set
//  3/6. Troubleshooting

// - 3/1.) Set
//            Run the script with the "Set" argument.

//            This will gather info about the components and resets some values.
//            Also, it tries to align the rotor and the pistons to a Starting Position.
//            The gathered information will be displayed in the programmable block's detailn info.
//            Also, there's gonna be some other useful information, like the estimated time to finish. 
//                  ( It's just an estimate, not a fully acurate number. )

//            Check the detailed info if the system recognized the correct number of components.

//            If it did, and you are seeing the "System: Ready to Start!" message, then you can proceed to section 3/2.) Start.
//                  ( Even if the pistons and the rotor are still moving. )
 
//            If you see the "System: Not Ready!" message, then read section - 3/6.) Troubleshooting.

// - 3/2.) Start
//            Run the script with the "Start" argument.

//            This will starts the mining sequence.
//                  (It waits until the pistons and the rotor have reached the Starting Position)
//            You can find information about the system and progression in the detailed info of the Programmable Block.

//            If the mining sequence is finished, then you'll see the "Mining Completed!" message there.
//            After the mining is finished, the Pistons are gonna be retracted and the Drills are gonna stop.
//            Also, the script stops running as well.

// - 3/3.) Pause
//            Run the script with the "Pause" argument.

//            This will pauses the Mining Sequence and stops the script as well.
//            You can continue the Mining Sequence by running the script with the "Start" argument again.

// - 3/4.) Refresh
//            Run the script with the "Refresh" argument.

//            This will refreshes the changing data and the Components of the system.
//            It will writes a summary about it in to the detailed info as well.

//            You can use the "Refresh" command while the script runs.

//            If you want to add an Advanced Component, like an LCD Panel or a Cargo Container, while the script runs or 
//            while it's paused, you can do it by renaming it proeprly, then use the "Refresh" command.
//            The list of Components will be refreshed and the added block will become functional.

//            You can safely remove an Advanced Component, by pausing the Script, removing the Main Tag from it's name, 
//            then using the "Refresh" command.
//            If you remove a Component, while the script runs, then it could crash.
//            If
//            If it does, then recompile the script, to make it functional again.

//            Do not add a new Basic Component by using "Refresh", unless you are at Step 0.

// - 3/5.) Advanced Set
//            You can use the "Set" command with an extension, to set the system a to a specific step of the Mining Sequence.

//             With the default settings, there are 40 Steps that the system goes trough until it finishes.
//                 (This max step changes, depeneding on how many pistons and drills are you using.)

//             Step: 0 - Aligns the rotor and the pistons to a starting position.
//             Step: Odd Number - Rotates the rotor (You can't Advanced Set and Odd Number)
//             Step: Even Number - Extends/Retracts piston(s) 
//             Step: Last Step - Retracts the piston(s) and turns the drill(s) off.

//             To set the system to Step: 12, then use the "Set;12" as argument.
//             You can only Set the System with an Even number. 
//             I disabled the Odd Numbers, because it's hard to predict what's going to happend if you Set one, unless 
//             you exactly know, how the script is working.

//             If you want to rotate the Rotor for some reason, then Pause the system and rotate it manually. 

// - 3/6.) Troubleshooting

//            If you are seeing the "System: Not Ready!" message:
//              - Check if you have all the components in the grid, and you've renamed them in the right way.
//                      You can find information about them in the detailed info of the programmable block.
//                      If the detailed info is disappeared, you can use the "Refresh" command to write it out again.
//              - If every block is renamed correctly, then use the "Set" command again to check it.
//           If the "System: Not Ready!" message still appeares:
//              - Click Recomplie in the programmable block, the use the "Set" command again. 

//            If you are seeing the "Command doesn't found!" message:
//              - Check if the command in the argument is correct.
//                      The only acceptable comands are "Set", "Start","Pause" and "Refresh"  ( They are not case sensitive. )

//          If you are seeing other error messages, like "Caught expection..." and other code stuff:
//                      Then a Component is probalby destroyed or is missing since the last component initialization.
//              - Recompile the script, then run it with the "Refresh" argument.
//                      This will gives you information about the Components.                       
 
//          If you have any questions or need help, you can write a reply to the Discussions in the Script's workshop page!
//                  Link: https://steamcommunity.com/sharedfiles/filedetails/discussions/1695500366
///////////////////////

//  Now you are ready to use the script.
//  If you want to experiment with the script or you want to use it in an unique way then you may check 4.) Advanced Configuration
               
///////////////////////
// 4.) Advanced Configuration
///////////   

//      If you want to use unique Piston Limits for some reason:
//                  - Change the Use Unique Piston Limits to True.
//                  - Change the Limit values.

//      If you want to use pistons that are not 2 Blocks high:
//                  - Change the Piston Body Length in Meters.
//                      - 1 Block counts as 2.5 Meters.
//                  - You should only use the same type of Piston for a Mining Platform. 

//      If you want to change the speed of the Rotation, or the speed of the Piston extensions/retractions:
//                  - Change the Speed values.
//                      - These values were defined through testing. Change these in your own behalf.

///////////////////////
// 5.) Features
///////////  

//  Adaptive Extension and Speed:
//              - Based on how many Horizontal Pistons and Drills are you using, 
//                the script adapts and finds the best configuration for the different values that it uses, 
//                like how much the Horizontal Pistons should extend or how fast the Rotor should turn.
//              - Most of this happens, when you run the "Set" command. 
//              - (You can overwrite this by setting unique values in the Configuration.)

//  Auto Pause/Start
//              - The script checks that how much the Drill is filled and Pauses the Mining, 
//                 when the High Cargo Threshold is reached.
//              - The Mining restarts, when the Low Cargo Threshold is passed.
//              - If there is any Cargo Blocks added to the script, then it will 
//                 uses these blocks as reference, instead of the Drill.

//  Show Advanced Data
//              - There will be more information shown about the running.
//              - It's not recommended for constant use in servers.

//  LCD Color Coding
//              - The font color of the Information in the LCDs will changes based on the current state of the script.

//  Broadcasting Progression
//              - You can broadcast progression informations trough an antenna to another grid's LCD.
//              - More detailed information about how to do it is found under the 2.) Basic Configuration section.

//  Share Inertia Tensor
//              - Enables the Share Inertia Tensor In-game function of Pistons and Rotors to achive more stable behavior.

// Dynamic Rotor Inertia Tensor
//              - The Rotor sometimes acts strangely if the Share Inertia Tensor is enabled.
//              - This feature will periodically enables and disables it for the Rotors, to prevent some of the issues.

//  End of Mining
//              - When the Mining is finished then the Pistons are gonna be retracted and the Drills are gonna stop.

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//SOME OTHER NOTES:

//  Starting Position:
//      A state, where the Rotor is either on it's Min or Max limit, and all the Pistons are retracted to their Min limit.

//  Mining Sequence:
//      The script rotates the rotor until it reaches it's max angle, then extends the Horizontal Pistons by a predetermined distance.
//      This rotation and extension repeats, until the Horizontal Pistons were reached thier maximum limit.
//      Then the rotor rotates once again at the maximum limit of the Horizontal Pistons, then the Vertical Pistons will be extended.
//      Rotation again, then the Horizontal Pistons are gonna retract. 
//      This repeats until the Horizontal pistons are at thier minimum limit.
//      Rotation at the minimum, then another Vertical Extension.

//      After that it's repeated from the start of the mining sequence.
//      This will continue until every bit of ore that could be reached by the pistons and the rotor are mined out.

//      If there are no Vertical or Horizontal Pistons, the script will skips the corresponding actions and functions.
//      You may use any number of Vertical and Horizontal Pistons.

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//DO NOT MODIFY ANYTHING BELOW THIS
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Configuration variables
string main_tag="/Mine 01/";

bool use_auto_pause=true;
float cargo_high_limit=0.9F;
float cargo_low_limit=0.5F;
bool share_inertia_tensor=true;
bool use_dynamic_rotor_tensor=true;
bool use_lcd_color_coding=true;
bool show_advanced_data=false;
long receiver_address=0;

float max_rot_angle = 358F;
float min_rot_angle = 0F;
float excess_meters = 5.0F;   
float vp_step_length=2.5F;
bool use_unique_hp_step_length=false;
float hp_step_length=3.33F;
bool always_retract_hpistons=false;

bool use_unique_piston_limits=false;
float max_vp_limit=10F;
float min_vp_limit=0F;
float max_vp_limit_inv=10F;
float min_vp_limit_inv=0F;
float max_hp_limit=10F;     
float min_hp_limit=0F;
float vp_vel=0.3F;                            
float hp_vel=0.3F;                            
float rotor_vel_at_10m=0.5F;           
float piston_length=5F;

//Other variables

float rot_speed=0;
float piston_speed=0;
double rotorAng;
string[] data;
string message="";
string status="default";

//Block Tags
string vp_tag="/Ver/";
string hp_tag="/Hor/";

//Inverse Tag
string inv_tag="/Inv/";

//Timer Advanced Tag
string adv_tag="/Adv/";

int number = 0;

string mode = "";
int progress=0;
int i=0,k=0,l=0,m=0;
int eta_h=0;
int eta_m=0;
int tensor_counter=0;

bool run_indicator=true;

float vp_range=0;
float hp_range=0;

bool use_timer=false;
bool use_timer_adv=false;
bool use_screen=false;
bool use_antenna=false;

float debug_zone_bottom=0;
float debug_zone_top=0;

bool use_cargo_to_check=false;
float cargo_curr_volume=0;

bool set_auto_pause=false;

VRage.MyFixedPoint maxvolume=0;
VRage.MyFixedPoint curvolume=0;

Color lcd_color=Color.White;
////////////////////////////////////////
//Variables stored in the Storage string

int step=0;
float max_step=1;

int vp_count=0;
int vp_stage_count=0;

int hp_count=0;
int hp_stage_count=0;

int v_stage=0;
int h_stage=0;

bool set_mp_happened=false;
bool hp_extend=true;
bool ready_to_start=false;
bool run=false;

float vpiston_goal=0;
float hpiston_goal=0;
float vpiston_goal_inv=0;

bool passed_debug_zone=false;

bool use_high_cargo_limit=true;
bool target_min_rot_limit=true;
////////////////////////////////////////
//Block defining stuff
 
List<IMyTerminalBlock> blocks;
List<IMyPistonBase> v_pistons;
List<IMyPistonBase> h_pistons;
List<IMyPistonBase> v_pistons_inv;
List<IMyShipDrill> drills;
List<IMyTextSurface> screens;
List<IMyTerminalBlock> cargos;


IMyMotorAdvancedStator rotor;
IMyPistonBase piston;
IMyShipDrill drill;
IMyTextSurface screen;
IMyTextSurfaceProvider lcd_block;
IMyRadioAntenna antenna;
IMyTimerBlock timer;
IMyTimerBlock timer_adv;

IMyTextSurface me_lcd;

public Program()
{
    load_data();

    if(!get_configuration())set_configuration();

    if(run)
    {
        Runtime.UpdateFrequency = UpdateFrequency.Update100;
        if(use_dynamic_rotor_tensor)
        {
            tensor_counter=38;
        }
    }


    if(refresh_components())
    {
        save_data();
        count_eta();
    }

    else
    {
            me_lcd = Me.GetSurface(0);
            if(me_lcd.ContentType!=ContentType.TEXT_AND_IMAGE)
            {
                me_lcd.ContentType=ContentType.TEXT_AND_IMAGE;
            }
            me_lcd.FontSize=1.2F;
    }
}

public void Save()
{
    set_configuration();
    save_data();
}

public void Main(string argument, UpdateType updateSource) 
{ 
    status="";
    if((updateSource & UpdateType.Update100)!=0)
    {
        if(ready_to_start)
        {
            if(use_dynamic_rotor_tensor)
            {
                if(tensor_counter>=40)
                {
                    tensor_counter=0;
                    if(!rotor.GetValueBool("ShareInertiaTensor"))
                    {
                        rotor.GetActionWithName("ShareInertiaTensor").Apply(rotor);
                    }
                }
                else if(tensor_counter==1)
                {
                     if(rotor.GetValueBool("ShareInertiaTensor"))
                    {
                        rotor.GetActionWithName("ShareInertiaTensor").Apply(rotor);
                    }
                }
                tensor_counter++;
            }
            if(run)
            {
                if(use_auto_pause && check_if_full(use_high_cargo_limit))
                {
                    status="Auto Paused...";
                    if(!set_auto_pause)
                    {
                        pause_moving_parts();
                        set_auto_pause=true;
                        lcd_color=Color.Orange;
                        if(use_timer_adv)timer_adv.GetActionWithName("Start").Apply(timer_adv);
                    }
                }
                else
                {
                    if(set_auto_pause)
                    {
                        set_auto_pause=false;
                    }
                    if(lcd_color!=Color.DodgerBlue)lcd_color=Color.DodgerBlue;
                    start_system();
                }
            }
        }
        else
        {
            Echo("\nSystem: Not Ready!\n");
            lcd_color=Color.Yellow;
            pause_moving_parts();
            Runtime.UpdateFrequency = UpdateFrequency.None;
            run=false;
        }
        set_message();
        list_data();
        me_lcd.FontColor=lcd_color;
        me_lcd.WriteText(message,false);
        if(use_screen)
        {
            write_screen();
        }
        if(use_antenna)send_message();
    }
    else
    {
        if(argument!="")
        {
            if(argument.Contains(';'))
            {
                mode=argument.Split(';')[0];
                mode=mode.ToLower();   
                if(!Int32.TryParse(argument.Split(';')[1],out number))
                {
                    Echo("Wrong Number after the separator!");
                    return;
                }
                else
                {
                    if(number%2==0)
                    {
                        if(number>=0)
                        {
                            if(number>max_step)
                            {
                                Echo("Wrong Number! It's higher than the Max Step!");
                                return;
                            }
                        }
                        else
                        {
                            Echo("Wrong Number! You can't use negative numbers!"); 
                            return;
                        }
                    }
                    else
                    {
                        Echo("Wrong Number! You can't use an odd number for an Advanced Set!");
                        return;
                    }
                }
            }
            else
            {
                mode=argument;
                mode=mode.ToLower();
                number=0;
            }
        }   
        else
        {
            Echo("No argument added!");
            mode="";
            return;
        }

        switch(mode)
        {
            case "set":
            {
                Echo("[Command: Set]");
                if(!get_configuration())set_configuration();
                Runtime.UpdateFrequency = UpdateFrequency.None;
                set_system(number);
                lcd_color=Color.White;
                save_data();
                break;
            }
            case "start":
            {
                Echo("[Command: Start]");
                if(ready_to_start)
                {
                    if(step<max_step)
                    {
                        run=true;
                        use_high_cargo_limit=true;
                        Runtime.UpdateFrequency = UpdateFrequency.Update100;
                        lcd_color=Color.DodgerBlue;

                        if(use_dynamic_rotor_tensor && rotor.GetValueBool("ShareInertiaTensor"))
                        {
                            rotor.GetActionWithName("ShareInertiaTensor").Apply(rotor);
                        }
                    }
                }
                else
                {
                    lcd_color=Color.Red;
                    Echo("Error! System is not Ready!");
                    Echo("You must Set the system first");
                    Echo("to be able to Start.");
                }
                break;
            }
            case "pause":
            {
                Echo("[Command: Pause]");
                if(ready_to_start)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    lcd_color=Color.Yellow;
                    run=pause_moving_parts();
                    status="Paused";
                    save_data();
                    if(use_dynamic_rotor_tensor && !rotor.GetValueBool("ShareInertiaTensor"))
                    {
                        rotor.GetActionWithName("ShareInertiaTensor").Apply(rotor);
                    }
                }
                else
                {
                    lcd_color=Color.Red;
                    Echo("Error! System is not Ready!");
                    Echo("You must Set the system first");
                    Echo("to be able to Pause.");
                }
                break;
            }
            case "refresh":
            {
                Echo("[Command: Refresh]");
                if(!get_configuration())set_configuration();
                lcd_color=Color.White;
                refresh_components();
                save_data();
                break;
            }
            default:
            {
                Echo("Command doesn't found!");
                lcd_color=Color.Red;
                break;
            }
        }
        set_message();
        list_data();
        me_lcd.FontColor=lcd_color;
        me_lcd.WriteText(message,false);
        if(use_screen)write_screen();
        if(use_antenna)send_message();
    }
}

public void step_analisis(int num)
{
//Calculates the v_stage and h_stage variables, based on the step
    if(num%2==1)
    {
        num--;
    }
    if(num==0 || num==max_step)
    {
        v_stage=0;
        h_stage=0;
    }     
    else
    {
        num=num/2;
                    
        v_stage=(int)Math.Floor(num/(double)(hp_stage_count+1));
        if(num%(hp_stage_count+1)==0)
        {
            h_stage=0;
        }
        else 
        {
            h_stage=(num%(hp_stage_count+1));
        }
    }
}

public bool step_completed()
{
//Check if everything is in their correct place, based on the current step

    double rotorAng=(180/Math.PI)*rotor.Angle;
    rotorAng=Math.Round(rotorAng,MidpointRounding.AwayFromZero);

    if(step%2==1)
    {
        if(passed_debug_zone)
        {
            if(target_min_rot_limit)
            {
                if(rotorAng==min_rot_angle)
                {
                    passed_debug_zone=false;
                    return true;
                }
            }
            else
            {
                if(rotorAng==max_rot_angle)
                {
                    passed_debug_zone=false;
                    return true;
                }
            }
        }
        else
        {
            if(!test_if_in_debug_zone())
            {
                set_moving_parts(step);
            }
        }
        return false;
    }
    else
    {
        foreach(IMyPistonBase pis in h_pistons) 
        {
            if(Math.Round(pis.CurrentPosition,2,MidpointRounding.AwayFromZero)!=Math.Round(hpiston_goal,2,MidpointRounding.AwayFromZero))
            {
               return false;
            }
        }
        if(always_retract_hpistons)
        {
            foreach(IMyPistonBase pis in v_pistons_inv)
            {
                if(!pis.Enabled)pis.Enabled=true;
            }
            foreach(IMyPistonBase pis in v_pistons)
            {
                if(!pis.Enabled)pis.Enabled=true;
            }
        }
        foreach(IMyPistonBase pis in v_pistons) 
        {
            if(Math.Round(pis.CurrentPosition,2,MidpointRounding.AwayFromZero)!=Math.Round(vpiston_goal,2,MidpointRounding.AwayFromZero))
            {
                return false;
            }
        }
        foreach(IMyPistonBase pis in v_pistons_inv) 
        {
            if(Math.Round(pis.CurrentPosition,2,MidpointRounding.AwayFromZero)!=Math.Round(vpiston_goal_inv,2,MidpointRounding.AwayFromZero))
            {
                return false;
            }
        }
        foreach(IMyPistonBase pis in h_pistons) 
        {
            if(Math.Round(pis.CurrentPosition,2,MidpointRounding.AwayFromZero)!=Math.Round(hpiston_goal,2,MidpointRounding.AwayFromZero))
            {
               return false;
            }
        }
        if(step==0)
        {
            if(target_min_rot_limit)
            {
                if(rotorAng!=min_rot_angle)
                {
                    return false;
                }
            }
            else
            {
                if(rotorAng!=max_rot_angle)
                {
                    return false;
                }
            }
            passed_debug_zone=false;
            rotor.Enabled=false;
            rotor.SetValueFloat("UpperLimit",max_rot_angle);
            rotor.SetValueFloat("LowerLimit",min_rot_angle);
        }
        target_min_rot_limit=!target_min_rot_limit;
        return true;
    }
}

public bool set_moving_parts(int step_f, bool first=false)
{
//Sets the rotor and the pistons to their correct angle, distance and speed.
     
    rotorAng=(180/Math.PI)*rotor.Angle;
    rotorAng=Math.Round(rotorAng,MidpointRounding.AwayFromZero);

    set_piston_goals(step_f);

    if(step_f==0 || first)
    {
        if(vp_count>0)
        {
            piston_speed=vp_vel/vp_count;

            foreach(IMyPistonBase a in v_pistons) 
            {
                set_piston(a,vpiston_goal);
            }
            foreach(IMyPistonBase a in v_pistons_inv) 
            {
                set_piston(a,vpiston_goal_inv);
            }       
        }
        if(hp_count>0)
        {
            piston_speed=hp_vel/hp_count;
            foreach(IMyPistonBase a in h_pistons) 
            {
                set_piston(a,hpiston_goal);
            }
        }
        rot_speed=(float)(rotor_vel_at_10m*10);
        if(hp_count>0)
        {
            if(hp_extend)
            {
                rot_speed=(float)(rot_speed/((hp_count*piston_length)+(h_stage*hp_step_length)+excess_meters));
            }
            else
            {
                rot_speed=(float)(rot_speed/((hp_count*piston_length)+excess_meters+(hp_stage_count-h_stage)*hp_step_length));
            }  
        }   
        else
        {
            rot_speed=(float)(rot_speed/excess_meters);
        }
        rotor.SetValueFloat("UpperLimit",max_rot_angle);
        rotor.SetValueFloat("LowerLimit",min_rot_angle);
        rotor.SetValueFloat("BrakingTorque",1000000000);
        if(rotorAng==min_rot_angle)
        {  
            rotor.Enabled=false;
            rotor.SetValueFloat("Velocity",rot_speed);
            target_min_rot_limit=true;
        }
        else if(rotorAng==max_rot_angle)
        {
            rotor.Enabled=false;
            rotor.SetValueFloat("Velocity",-rot_speed); 
            target_min_rot_limit=false;         
        }
        else if(rotorAng==360F)
        {
            rotor.Enabled=false;
            if(min_rot_angle==0F)
            {
                target_min_rot_limit=true;
                rotor.SetValueFloat("Velocity",rot_speed);
            }
            if(max_rot_angle==0F)
            {
                target_min_rot_limit=false;
                rotor.SetValueFloat("Velocity",-rot_speed);
            }
        }
        else if(rotorAng<min_rot_angle)
        {
            if(Math.Abs(-rotorAng-max_rot_angle)<Math.Abs(min_rot_angle-rotorAng))
            {
                rotor.Enabled=true;
                rotor.SetValueFloat("UpperLimit",max_rot_angle);
                rotor.SetValueFloat("LowerLimit",max_rot_angle);
                rotor.SetValueFloat("Velocity",-rot_speed);
                target_min_rot_limit=false;
            }
            else
            {
                rotor.Enabled=true;
                rotor.SetValueFloat("UpperLimit",min_rot_angle);
                rotor.SetValueFloat("LowerLimit",min_rot_angle);
                rotor.SetValueFloat("Velocity",rot_speed);
                target_min_rot_limit=true;
            }
        }
        else if(rotorAng>max_rot_angle)
        {
            if(Math.Abs(max_rot_angle-rotorAng)<Math.Abs(-rotorAng-min_rot_angle))
            {
                rotor.Enabled=true;
                rotor.SetValueFloat("UpperLimit",max_rot_angle);
                rotor.SetValueFloat("LowerLimit",max_rot_angle);
                rotor.SetValueFloat("Velocity",-rot_speed);
                target_min_rot_limit=false;
            }
            else
            {
                rotor.Enabled=true;
                rotor.SetValueFloat("UpperLimit",min_rot_angle);
                rotor.SetValueFloat("LowerLimit",min_rot_angle);
                rotor.SetValueFloat("Velocity",rot_speed);
                target_min_rot_limit=true;
            }
        }
        else if(Math.Abs(max_rot_angle-rotorAng)>Math.Abs(min_rot_angle-rotorAng))
        {
            rotor.Enabled=true;
            rotor.SetValueFloat("Velocity",-rot_speed);
            target_min_rot_limit=true;
        }
        else
        {
            rotor.Enabled=true;
            rotor.SetValueFloat("Velocity",rot_speed);
            target_min_rot_limit=false;
        }
    }
    else
    {
        if(step%2==0)
        {
            if(step!=0)
            {
                rotor.Enabled=false;
                if(vp_count>0)
                {
                    piston_speed=vp_vel/vp_count;
                    if(always_retract_hpistons)
                    {
                        foreach(IMyPistonBase a in v_pistons) 
                        {
                            set_piston(a,vpiston_goal);
                            a.Enabled=false;
                        }
                        foreach(IMyPistonBase a in v_pistons_inv) 
                        {
                            set_piston(a,vpiston_goal_inv);
                            a.Enabled=false;
                        }  
                    }
                    else
                    {
                        foreach(IMyPistonBase a in v_pistons) 
                        {
                            set_piston(a,vpiston_goal);
                        }
                        foreach(IMyPistonBase a in v_pistons_inv) 
                        {
                            set_piston(a,vpiston_goal_inv);
                        }   
                    }
                }
                if(hp_count>0)
                {
                    piston_speed=hp_vel/hp_count;
                    foreach(IMyPistonBase a in h_pistons) 
                    {
                        set_piston(a,hpiston_goal);
                    }
                }
            }
        }
        else
        {
            rot_speed=(float)(rotor_vel_at_10m*10);
            if(hp_count>0)
            {
                if(hp_extend)
                {
                    rot_speed=(float)(rot_speed/((hp_count*piston_length)+excess_meters+h_stage*hp_step_length));
                }
                else
                {
                    rot_speed=(float)(rot_speed/((hp_count*piston_length)+excess_meters+(hp_stage_count-h_stage)*hp_step_length));
                } 
            }
            else
            {
                rot_speed=(float)(rot_speed/excess_meters);
            }
            rotor.Enabled=true;


            if(target_min_rot_limit)
            {
                if(rotorAng<min_rot_angle)
                {
                    rotor.SetValueFloat("Velocity",-rot_speed);
                }
                else if(rotorAng>=max_rot_angle)
                {
                    rotor.SetValueFloat("Velocity",-rot_speed);
                }
            }
            else
            {
                if(rotorAng>max_rot_angle)
                {
                    rotor.SetValueFloat("Velocity",rot_speed);
                }
                else if(rotorAng>=min_rot_angle)
                {
                    rotor.SetValueFloat("Velocity",rot_speed);
                }
                else
                {
                    rotor.SetValueFloat("Velocity",-rot_speed);
                }
            }

            foreach(IMyPistonBase pis in v_pistons) 
            {
                pis.Enabled=false;
            }
            foreach(IMyPistonBase pis in h_pistons) 
            {
                pis.Enabled=false;
            }
        }
        count_eta();
    }
    return true;
}

public void list_data(bool counts=false)
{
    if(counts)
    {
        Echo("Vertical Pistons Detected: "+vp_count);
        Echo("Horizontal Pistons Detected: "+hp_count);
        if(drills!=null && drills.Count>=1)
        {
            Echo("Drills Detected: "+drills.Count);
        }
        else
        {
            Echo("Drill: Not found!");
        }
        if(rotor!=null)
        {
            Echo("Rotor: Detected!");
        }
        else
        {
            Echo("Rotor: Not found!");
        }
        if(use_screen)
        {
            if(screens!=null && screens.Count>=1)
            {
                Echo("Screens Detected: "+screens.Count);
            }
            else
            {
                Echo("Screen: Not found!");
            }
        }      
    }
    else
    {
        Echo(message);
    }
}

public void save_data()
{
    Storage=""
// 0
            +step+";"                               
// 1            
            +max_step+";"                      
// 2            
            +h_stage+";"                         
// 3            
            +hp_stage_count+";"
// 4            
            +v_stage+";"
// 5            
            +vp_stage_count+";"
// 6            
            +vpiston_goal+";"
// 7            
            +hpiston_goal+";"
// 8            
            +vp_count+";"
// 9           
            +hp_count+";"
// 10            
            +set_mp_happened+";"
// 11            
            +hp_extend+";"
// 12            
            +ready_to_start+";"
// 13            
            +run+";"
//14
            +vpiston_goal_inv+";"
//15
            +passed_debug_zone+";"
//16
            +use_high_cargo_limit+";"
//17
            +target_min_rot_limit;
        
    Echo("Data Saved!");
}

public void load_data()
{
    data=Storage.Split(';');
    if(data.Length==18)
    {
        if(!Int32.TryParse(data[0], out step)){Echo("Converting |step| failed!");}
        if(!Single.TryParse(data[1], out max_step)){Echo("Converting |max_step| failed!");}
        if(!Int32.TryParse(data[2], out h_stage)){Echo("Converting |h_stage| failed!");}
        if(!Int32.TryParse(data[3], out hp_stage_count)){Echo("Converting |hp_stage_count| failed!");}
        if(!Int32.TryParse(data[4], out v_stage)){Echo("Converting |v_stage| failed!");}
        if(!Int32.TryParse(data[5], out vp_stage_count)){Echo("Converting |vp_stage_count| failed!");}
        if(!Single.TryParse(data[6], out vpiston_goal)){Echo("Converting |vpiston_goal| failed!");}
        if(!Single.TryParse(data[7], out hpiston_goal)){Echo("Converting |h_piston_goal| failed!");}
        if(!Int32.TryParse(data[8], out vp_count)){Echo("Converting |vp_count| failed!");}
        if(!Int32.TryParse(data[9], out hp_count)){Echo("Converting |hp_count| failed!");}
        if(!Boolean.TryParse(data[10], out set_mp_happened)){Echo("Converting |set_mp_happened| failed!");}
        if(!Boolean.TryParse(data[11], out hp_extend)){Echo("Converting |hp_extend| failed!");}
        if(!Boolean.TryParse(data[12], out ready_to_start)){Echo("Converting |ready_to_start| failed!");}
        if(!Boolean.TryParse(data[13], out run)){Echo("Converting |run| failed!");}
        if(!Single.TryParse(data[14], out vpiston_goal_inv)){Echo("Converting |v_piston_goal_inv| failed!");}
        if(!Boolean.TryParse(data[15], out passed_debug_zone)){Echo("Converting |passed_debug_zone| failed!");}
        if(!Boolean.TryParse(data[16], out use_high_cargo_limit)){Echo("Converting |use_high_cargo_limit| failed!");}
        if(!Boolean.TryParse(data[17], out target_min_rot_limit)){Echo("Converting |target_min_rot_limit| failed!");}

        Echo("Data Loaded!");
    }
    else
    {
        Echo("Load Failed!");
    }
}

public bool set_system(int number)
{
    bool result=true;

    set_mp_happened=false;  
    hp_extend=true;
    step=number; 
    run=false;
    use_high_cargo_limit=true;
    
    result=refresh_components();

    if(result)
    {
        vp_stage_count=(int)Math.Ceiling(((vp_range)*vp_count)/vp_step_length);
        hp_stage_count=(int)Math.Ceiling(((hp_range)*hp_count)/hp_step_length);

        max_step=2+((1+vp_stage_count)*hp_stage_count*2)+(vp_stage_count*2);

        if(step>max_step)
        {
            step=(int)max_step;
        }
        else if(step==max_step)
        {
            step_analisis(step);                                   

            set_moving_parts(0,true);

            foreach(IMyShipDrill a in drills) 
            {
                if(a.Enabled)a.Enabled=false;
            }
        }
        else
        {
            step_analisis(step);                                   

            set_moving_parts(step,true);

            foreach(IMyShipDrill a in drills) 
            {
                if(!a.Enabled)a.Enabled=true;
            }
        }
        status="Aligning Starting Position...";
        Echo("\nSystem: Ready to Start!");

        count_eta();
        check_if_full();
    } 
    return result;
}

public void start_system()
{
    step_analisis(step);

    foreach(IMyShipDrill a in drills) 
    {
        if(!a.Enabled)a.Enabled=true;
    }

    if(set_mp_happened)
    {
        if(step_completed())
        {
            if(step!=max_step-1)
            {
                status="Step Completed!";
                step++;
                set_mp_happened=false;
            }
            else
            {
                step++;
                status="Mining Completed!";
                lcd_color=Color.Lime;
                v_stage=0;
                h_stage=0;
                hp_extend=true;
                set_moving_parts(0,true);

                foreach(IMyShipDrill a in drills) 
                {
                    a.Enabled=false;
                }
                if(use_timer)
                {
                    timer.Enabled=true;
                    timer.GetActionWithName("Start").Apply(timer);
                }
                if(use_dynamic_rotor_tensor && !rotor.GetValueBool("ShareInertiaTensor"))
                {
                    rotor.GetActionWithName("ShareInertiaTensor").Apply(rotor);
                }
                run=false;
                Runtime.UpdateFrequency = UpdateFrequency.None;
                count_eta();
            }
        }
        else
        {
            status="Aligning...";
        }
    }
    else
    {
        status="Setting Moving Parts...";
        set_mp_happened=set_moving_parts(step);
    }     
}

public bool pause_moving_parts()
{
    foreach(IMyPistonBase a in v_pistons) 
    {
        if(a.Enabled){a.Enabled=false;}
    }
    foreach(IMyPistonBase a in h_pistons) 
    {
         if(a.Enabled){a.Enabled=false;}
    }
    foreach(IMyShipDrill a in drills) 
    {
        if(a.Enabled){a.Enabled=false;}
    }
    if(rotor.Enabled){rotor.Enabled=false;}

    set_mp_happened=false;

    return false;
}

public void write_screen(bool log=false)
{
    if(!log)
    {
        if(use_lcd_color_coding)
        {
            foreach(IMyTextSurface a in screens)
            {
                a.FontColor=lcd_color;
                a.WriteText(message,false);
            }  
        }
        else
        {
            foreach(IMyTextSurface a in screens)
            {
                a.WriteText(message,false);
            }  
        }
    }
    else
    {        
        foreach(IMyTextPanel a in screens)
        {
            a.WriteText(message,true);
        }
    }
 
}

public bool refresh_components()
{    
    ready_to_start=true;
    use_timer=false;
    use_timer_adv=false;
    use_screen=false;
    use_antenna=false;
    use_cargo_to_check=false;

    blocks = new List<IMyTerminalBlock>();
    v_pistons = new List<IMyPistonBase>();
    h_pistons = new List<IMyPistonBase>();
    v_pistons_inv = new List<IMyPistonBase>();
    drills = new List<IMyShipDrill>();
    screens = new List<IMyTextSurface>();
    cargos = new List<IMyTerminalBlock>();
    i=0;k=0;l=0;m=0;
    int n=0;

    me_lcd = Me.GetSurface(0);
    if(me_lcd.ContentType!=ContentType.TEXT_AND_IMAGE)
    {
        me_lcd.ContentType=ContentType.TEXT_AND_IMAGE;
    }
    me_lcd.FontSize=1.2F;
    
    GridTerminalSystem.SearchBlocksOfName(main_tag,blocks);
    foreach(IMyTerminalBlock a in blocks)
    {
        if(a is IMyPistonBase)
        {
            piston=a as IMyPistonBase;
            if(piston.CustomName.Contains(vp_tag))
            {
                if(piston.CustomName.Contains(inv_tag)) v_pistons_inv.Add(piston);
                else v_pistons.Add(piston);    
            }
            else if(piston.CustomName.Contains(hp_tag))
            {
                h_pistons.Add(piston);
            }
            if(share_inertia_tensor)
            {
                if(!piston.GetValueBool("ShareInertiaTensor"))
                {
                    piston.GetActionWithName("ShareInertiaTensor").Apply(piston);
                }
            }
            else
            {
                if(piston.GetValueBool("ShareInertiaTensor"))
                {
                    piston.GetActionWithName("ShareInertiaTensor").Apply(piston);
                }
            }
        }
        else if(a is IMyShipDrill)
        {
            drill=a as IMyShipDrill;
            drills.Add(drill);
        }
        else if(a is IMyTextPanel)
        {
            screen=a as IMyTextSurface;
            if(screen.ContentType!=ContentType.TEXT_AND_IMAGE)
            {
                screen.ContentType=ContentType.TEXT_AND_IMAGE;
            }
            screens.Add(screen);
        }
        else if(a is IMyTextSurfaceProvider && a.EntityId!=Me.EntityId)
        {
            if(a.HasInventory)
            {
                cargos.Add(a);
            }
            data=a.CustomData.Split('\n');
            string subs="";
            foreach(string s in data)
            {
                if(s.StartsWith("@"))
                {
                    subs=s.Substring(1);
                    if(subs.Contains(main_tag))
                    {
                        subs=subs.Replace(main_tag,"");
                        if(Int32.TryParse(subs, out n))
                        {
                            lcd_block=a as IMyTextSurfaceProvider;
                            if(lcd_block.SurfaceCount>=n)
                            {
                                screen=lcd_block.GetSurface(n);
                                if(screen.ContentType!=ContentType.TEXT_AND_IMAGE)
                                {
                                    screen.ContentType=ContentType.TEXT_AND_IMAGE;
                                }
                                screens.Add(screen);
                            }
                        }
                    }
                }
            }
        }
        else if(a is IMyMotorAdvancedStator)
        {
            rotor = a as IMyMotorAdvancedStator;
            if(share_inertia_tensor)
            {
                if(!rotor.GetValueBool("ShareInertiaTensor"))
                {
                    rotor.GetActionWithName("ShareInertiaTensor").Apply(rotor);
                }
            }
            else
            {
                if(rotor.GetValueBool("ShareInertiaTensor"))
                {
                    rotor.GetActionWithName("ShareInertiaTensor").Apply(rotor);
                }
            }
            i++;
        }
        else if(a is IMyTimerBlock)
        {
            if(a.CustomName.Contains(adv_tag))
            {
                timer_adv = a as IMyTimerBlock;
                m++;
            }
            else
            {
                timer = a as IMyTimerBlock;
                k++;
            }
        }
        else if(a is IMyRadioAntenna)
        {
            antenna = a as IMyRadioAntenna;
            l++;
        }
        else if(a.HasInventory)
        {
            cargos.Add(a);
        }
    }

    vp_count=v_pistons.Count+v_pistons_inv.Count;
    if(vp_count>0)
    {
        Echo("Vertical Pistons: "+vp_count+" ( " +v_pistons_inv.Count+" Inverted )");
    }
    else
    {
        Echo("Vertical Pistons: None");
    }

     hp_count=h_pistons.Count;
    if(hp_count>0)
    {
        Echo("Horizontal Pistons: "+hp_count);
    }
    else
    {
        Echo("Horizontal Pistons: None");
    }

    if(drills.Count>=1)
    {
        Echo("Drills Detected: "+drills.Count);
    }
    else
    {
        Echo("Drill: Not Found!");
        ready_to_start=false;
    }
    
    if(i==1)
    {
        Echo("Rotor: Detected!");
    }
    else if(i<1)
    {
        Echo("Rotor: Not Found!");
        ready_to_start=false;
    }
    else
    {
        Echo("Rotor: Too Many Rotors!");
        ready_to_start=false;
    }

    if(screens.Count>=1)
    {
        Echo("Screens Detected: "+screens.Count);
        use_screen=true;
    }

    if(k==1)
    {
        Echo("Basic Timer: Detected!");
        use_timer=true;
    }
    else if(k>1)
    {
        Echo("Basic Timer: Too Many Timers!");
    }

    if(m==1)
    {
        Echo("Advanced Timer: Detected!");
        use_timer_adv=true;
    }
    else if(m>1)
    {
        Echo("Advanced Timer: Too Many Timers!");
    }
    if(l==1)
    {
        Echo("Antenna: Detected!");
        use_antenna=true;
        antenna.EnableBroadcasting=true;
    }
    else if(l>1)
    {
        Echo("Antenna: Too Many Antennas!");
    }


    if(cargos.Count>=1)
    {
        use_cargo_to_check=true;
        Echo("Cargo Module: "+cargos.Count);
    }


    if(ready_to_start)
    {
        Echo("System: Components Ready!");
        if(!use_unique_piston_limits)
        {
            if(vp_count>0)
            {
                if(v_pistons.Count>0)
                {
                    min_vp_limit=v_pistons[0].LowestPosition;
                    max_vp_limit=v_pistons[0].HighestPosition;
                    min_vp_limit_inv=min_vp_limit;
                    max_vp_limit_inv=max_vp_limit;
                }
                else
                {
                    min_vp_limit=v_pistons_inv[0].LowestPosition;
                    max_vp_limit=v_pistons_inv[0].HighestPosition;
                    min_vp_limit_inv=min_vp_limit;
                    max_vp_limit_inv=max_vp_limit;
                }
                if(max_vp_limit<vp_step_length)
                {
                    vp_step_length=max_vp_limit;
                }
            }
            if(hp_count>0)
            {
                min_hp_limit=h_pistons[0].LowestPosition;
                max_hp_limit=h_pistons[0].HighestPosition;
                if(max_hp_limit<hp_step_length)
                {
                    hp_step_length=max_hp_limit;
                }
                if(max_hp_limit==2F)
                {
                    piston_length=1F;
                }
            }
        }
        hp_range=max_hp_limit-min_hp_limit;
        vp_range=max_vp_limit-min_vp_limit;

        set_step_length();

        debug_zone_bottom=min_rot_angle+((max_rot_angle-min_rot_angle)*0.25F);
        debug_zone_top=max_rot_angle-((max_rot_angle-min_rot_angle)*0.25F);
    }
    else 
    {
        Echo("\nSystem: Components Not Ready!");
        Runtime.UpdateFrequency = UpdateFrequency.None;
        run=false;
    }
    return ready_to_start;
}

public bool check_if_full(bool state=false)
{
    maxvolume=0;
    curvolume=0;
    if(use_cargo_to_check)
    {
        foreach(IMyTerminalBlock a in cargos)
        {
            maxvolume += a.GetInventory(0).MaxVolume;   
            curvolume += a.GetInventory(0).CurrentVolume;
        }
    }
    else
    {
        maxvolume = drills[0].GetInventory(0).MaxVolume;   
        curvolume = drills[0].GetInventory(0).CurrentVolume;
    }
    cargo_curr_volume=(float)curvolume/(float)maxvolume;

    if(state)
    {
        if(cargo_curr_volume>=cargo_high_limit)
        {
            cargo_curr_volume=(float)Math.Round(cargo_curr_volume*100);
            use_high_cargo_limit=false;
            return true;
        }
        else
        {
            cargo_curr_volume=(float)Math.Round(cargo_curr_volume*100);
            return false;
        }
    }
    else
    {
        if(cargo_curr_volume<=cargo_low_limit)
        {
            cargo_curr_volume=(float)Math.Round(cargo_curr_volume*100);
            use_high_cargo_limit=true;
            return false;
        }
        else
        {
            cargo_curr_volume=(float)Math.Round(cargo_curr_volume*100);
            return true;
        }
    }
    
}

public void set_step_length()
{ 
    if(use_unique_hp_step_length)
    {
        vp_step_length=vp_step_length*10;
        if(vp_step_length%1!=0)
        {
             vp_step_length=vp_step_length*10;
            if(vp_step_length%1==0)
            {
                vp_step_length+=0.1F;
            }
            vp_step_length=(float)(Math.Ceiling(vp_step_length)/100);
        }
        else
        {
            vp_step_length=vp_step_length/10;
        }

        hp_step_length=hp_step_length*10;
        if(hp_step_length%1!=0)
        {
             hp_step_length=hp_step_length*10;
            if(hp_step_length%1==0)
            {
                hp_step_length+=0.1F;
            }
            hp_step_length=(float)(Math.Ceiling(hp_step_length)/100);
        }
        else
        {
            hp_step_length=hp_step_length/10;
        }
    }
    else if(hp_count>0)
    {
        hp_step_length=(float)Math.Ceiling((hp_count*(hp_range))/((drills.Count*2.5)+0.84));
        hp_step_length=(float)(Math.Ceiling(((hp_range)/hp_step_length)*100)/100);
        hp_step_length=hp_count*hp_step_length;
    }
}

public void count_eta()
{
    float result=0;
    
    if(step==max_step)
    {
        eta_h=0;
        eta_m=0;
    }
    else
    {
        rot_speed=(float)(rotor_vel_at_10m*10);
    
        if(hp_count==0)
        {
            result+=((1/((float)(rot_speed/excess_meters))));
            result=result*(vp_stage_count+1-v_stage);
        }
        else
        {
            float length=(hp_count*piston_length)+excess_meters;
            for(i=0;i<hp_stage_count+1;i++)
            {
                result+=1/((float)(rot_speed/(length+i*hp_step_length)));
            }

            result=result*(vp_stage_count+1-v_stage);

            if(hp_extend)
            {
                for(i=0;i<h_stage;i++)
                {
                    result-=1/((float)(rot_speed/(length+i*hp_step_length)));
                }
            }
            else
            {
                for(i=0;i<h_stage;i++)
                {
                    result-=1/((float)(rot_speed/(length+(hp_stage_count-i)*hp_step_length)));
                }
            }
        }   
        
        result=result*60*((max_rot_angle-min_rot_angle)/360);
   
        if(vp_count!=0)
        {
            result+=(vp_stage_count-v_stage)*(vp_range*vp_count/vp_vel/vp_stage_count);
        }
        if(hp_count!=0)
        {
            result+=(vp_stage_count+1-v_stage-h_stage)*(hp_range*hp_count/hp_vel/hp_stage_count);
            if(always_retract_hpistons)
            {
                result+=(hp_range*hp_count/hp_vel)*(vp_stage_count+1-v_stage);
            }
        }
        
        result+=(max_step-step)*2F;

        result=(float)Math.Ceiling(result);
  
        eta_h=(int)Math.Floor((double)result/3600);
        eta_m=(int)Math.Ceiling(((double)result%3600)/60);
    }
}

public bool test_if_in_debug_zone()
{
    rotorAng=(180/Math.PI)*rotor.Angle;
    rotorAng=Math.Round(rotorAng,MidpointRounding.AwayFromZero);

    if(rotorAng>=debug_zone_bottom && rotorAng<=debug_zone_top)
    {
        passed_debug_zone=true;
        return true;
    }
    return false;
}

public void set_piston_goals(int num)
{
    if(vp_count>0)
    {
        vpiston_goal=min_vp_limit+((vp_step_length*v_stage)/vp_count);
        vpiston_goal=(float)(Math.Round((double)vpiston_goal,2,MidpointRounding.AwayFromZero));
        vpiston_goal_inv=max_vp_limit_inv+min_vp_limit-vpiston_goal;
        if(vpiston_goal>max_vp_limit)
        {
            vpiston_goal=max_vp_limit;
        }
        if(vpiston_goal_inv<min_vp_limit_inv)
        {
            vpiston_goal_inv=min_vp_limit_inv;
        }    
    }
    if(hp_count>0)
    {
        if((num/2)%(hp_stage_count+1)==0 && !always_retract_hpistons)
        {
            if(v_stage%2==0)
            {
                hp_extend=true;
            }
            else
            {
                hp_extend=false;
            }
        }
        if(hp_extend)
        {
            hpiston_goal=min_hp_limit+((hp_step_length*h_stage)/hp_count);
            hpiston_goal=(float)(Math.Round((double)hpiston_goal,2,MidpointRounding.AwayFromZero));

            if(hpiston_goal>max_hp_limit)
            {
                hpiston_goal=max_hp_limit;
            }
        }
        else
        {
            hpiston_goal=max_hp_limit-((hp_step_length*h_stage)/hp_count);
            hpiston_goal=(float)(Math.Round((double)hpiston_goal,2,MidpointRounding.AwayFromZero));
            if(hpiston_goal<min_hp_limit)
            {
                hpiston_goal=min_hp_limit;
            }
        }
    }
}

public void set_piston(IMyPistonBase a,float p_goal)
{
    if(!a.Enabled)a.Enabled=true;
    a.SetValueFloat("LowerLimit",p_goal);
    a.SetValueFloat("UpperLimit",p_goal);
    if(a.CurrentPosition>a.GetValueFloat("UpperLimit"))
    {
        a.SetValueFloat("Velocity",-piston_speed);
    }
    else
    {
        a.SetValueFloat("Velocity",piston_speed);
    }
}

public void send_message()
{
    if(antenna.Enabled && antenna.EnableBroadcasting)
    {
        IGC.SendUnicastMessage(receiver_address,main_tag,message);
        Echo("Message Sent!");    
    }
    else
    {
        Echo("Error! Antenna isn't broadcasting!");
    }
}

public void set_message()
{
    progress=(int)Math.Round(((step/max_step)*100));
    if(run_indicator){
        message="[-/-/-/] ";
    }
    else{    
        message="[/-/-/-] ";
    }
    run_indicator=!run_indicator;
    message+=status+"\n";
    message+="Step: "+step+"/"+max_step+" | "+main_tag+" | ETA: "+eta_h+"h "+eta_m+"m\n";;

    float progr=progress/2.5F;
    message+="Progress: [";
    for(i=0;i<Math.Round(progr);i++)
    {
        message+="|";
    }   
    while(i<40)
    {
        message+="'";
        i++;
    }
    message+="] "+progress+"%\n";

    if(use_cargo_to_check)
    {
        progr=cargo_curr_volume/2.5F;
        message+="Cargo:      [";
        for(i=0;i<Math.Round(progr);i++)
        {
            message+="|";
        }   
        while(i<40)
        {
            message+="'";
            i++;
        }
        message+="] "+cargo_curr_volume+"%\n";
    }
    if(show_advanced_data)
    {
        if(ready_to_start)
        {
            rotorAng=(180/Math.PI)*rotor.Angle;
            rotorAng=Math.Round(rotorAng,MidpointRounding.AwayFromZero);

            message+="HStage: "+h_stage+"/"+hp_stage_count+" | "+"VStage: "+v_stage+"/"+vp_stage_count+"\n";
            message+="VP Goal: "+vpiston_goal+ "m|VP Goal Inv: "+vpiston_goal_inv+"m\n";
            message+="HP Goal: "+hpiston_goal+"m|HP Step Length: "+hp_step_length+"m\n";  
            message+="Rotor Vel: "+Math.Round(rotor.GetValueFloat("Velocity"),2)+" | Rot Angle: "+rotorAng+"°\n";
            message+="Set Mp Hap: "+set_mp_happened+" | HP Extend: "+hp_extend+"\n";
            message+="Ready to Start: "+ready_to_start+" | Run: "+run+"\n";
            message+="Rot Debug Zone: "+debug_zone_bottom+" - "+debug_zone_top+"\n";
            message+="Passed DBZ: "+passed_debug_zone+" TMRL: "+target_min_rot_limit+"\n";
        }
        else
        {
            message+="System Not Ready!\n Advanced Data cannot be shown!\n";
        }
    }
}

public bool get_configuration()
{
    if(Me.CustomData.StartsWith("@Configuration"))
    {
        string[] config=Me.CustomData.Split('|');
        if(config.Length==64)
        {
            bool result=true;
            main_tag=config[2];

            if(!Boolean.TryParse(config[4], out use_auto_pause)){Echo("Getting use_auto_pause failed!");result=false;}
            if(!Single.TryParse(config[6], out cargo_high_limit)){Echo("Getting cargo_high_limit failed!");result=false;}
            if(!Single.TryParse(config[8], out cargo_low_limit)){Echo("Getting cargo_low_limit failed!");result=false;}
            if(!Boolean.TryParse(config[10], out show_advanced_data)){Echo("Getting show_advanced_data failed!");result=false;}
            if(!Boolean.TryParse(config[12], out use_lcd_color_coding)){Echo("Getting use_lcd_color_coding failed!");result=false;}
            if(!Boolean.TryParse(config[14], out share_inertia_tensor)){Echo("Getting share_inertia_tensor failed!");result=false;}
            if(!Boolean.TryParse(config[16], out use_dynamic_rotor_tensor)){Echo("Getting use_dynamic_rotor_tensor failed!");result=false;}
            if(!Int64.TryParse(config[18], out receiver_address)){Echo("Getting receiver_address failed!");result=false;}

            if(!Single.TryParse(config[20], out max_rot_angle)){Echo("Getting max_rot_angle failed!");result=false;}
            if(!Single.TryParse(config[22], out min_rot_angle)){Echo("Getting min_rot_angle failed!");result=false;}
            if(!Single.TryParse(config[24], out excess_meters)){Echo("Getting excess_meters failed!");result=false;}
            if(!Single.TryParse(config[26], out vp_step_length)){Echo("Getting vp_step_length failed!");result=false;}
            if(!Boolean.TryParse(config[28], out use_unique_hp_step_length)){Echo("Getting use_unique_hp_step_length failed!");result=false;}
            if(!Single.TryParse(config[30], out hp_step_length)){Echo("Getting hp_step_length failed!");result=false;}
            if(!Boolean.TryParse(config[32], out always_retract_hpistons)){Echo("Getting always_retract_hpistons failed!");result=false;}

            if(!Boolean.TryParse(config[34], out use_unique_piston_limits)){Echo("Getting use_unique_piston_limits failed!");result=false;}
            if(!Single.TryParse(config[36], out max_vp_limit)){Echo("Getting max_vp_limit failed!");result=false;}
            if(!Single.TryParse(config[38], out min_vp_limit)){Echo("Getting min_vp_limit failed!");result=false;}
            if(!Single.TryParse(config[40], out max_vp_limit_inv)){Echo("Getting max_vp_limit_inv failed!");result=false;}
            if(!Single.TryParse(config[42], out min_vp_limit_inv)){Echo("Getting min_vp_limit_inv failed!");result=false;}
            if(!Single.TryParse(config[44], out max_hp_limit)){Echo("Getting max_hp_limit failed!");result=false;}
            if(!Single.TryParse(config[46], out min_hp_limit)){Echo("Getting min_hp_limit failed!");result=false;}
            if(!Single.TryParse(config[48], out vp_vel)){Echo("Getting vp_vel failed!");result=false;}
            if(!Single.TryParse(config[50], out hp_vel)){Echo("Getting hp_vel failed!");result=false;}
            if(!Single.TryParse(config[52], out rotor_vel_at_10m)){Echo("Getting rotor_vel_at_10m failed!");result=false;}
            if(!Single.TryParse(config[54], out piston_length)){Echo("Getting piston_length failed!");result=false;}

            hp_tag=config[56];
            vp_tag=config[58];
            inv_tag=config[60];
            adv_tag=config[62];
            if(result)
            {
                Echo("Configuration Done!");
                return true;
            }
            else
            {
                Echo("Configuration Error!");
                return false;
            }
        }
        else
        {
            Echo("Getting Configuration failed!");
            return false;
        }
    }
    else
    {
        Echo("Getting Configuration failed!");
        return false;
    }
}

public void set_configuration()
{
    Me.CustomData="@Configuration\n"+
                    "You can configure the script right below here,\n"+
                    "by changing the values between then | characters.\n\n"+

                    "The configuration will be loaded if you click Check Code\n"+
                    "in the Code Editor inside the Programmable Block,\n"+
                    "when the game Saves/Loads or if you use the\n"+
                    "Set or the Refresh command.\n\n"+

                    "There is a detailed explanation about what's what, inside the script.\n\n"+

                    "Main Tag: |"+main_tag+"|\n\n"+   //2 string

                    "///////////////////////////////////////////\n"+
                    "2.) Basic Configuration\n\n"+

                    "- You can change these at any point:\n\n"+

                    "Use Auto Pause: |"+use_auto_pause+"|\n"+   //4 bool
                    "High Cargo Threshold: |"+cargo_high_limit+"|\n"+   //6 float
                    "Low Cargo Threshold: |"+cargo_low_limit+"|\n"+   //8  float
                    "Show Advanced Data: |"+show_advanced_data+"|\n"+   //10 bool
                    "Use LCD Color Coding: |"+use_lcd_color_coding+"|\n"+    //12 bool
                    "Use Share Inertia Tensor: |"+share_inertia_tensor+"|\n"+   //14 bool
                    "Use Dynamic Rotor Inertia Tensor: |"+use_dynamic_rotor_tensor+"|\n"+   //16 bool
                    "Transmission Receiver Address: |"+receiver_address+"|\n\n"+  //18 long int

                    "- Don't change these while a mining is in progress:\n\n"+

                    "Max Rotor Angle: |"+max_rot_angle+"|\n"+ //20 float
                    "Min Rotor Angle: |"+min_rot_angle+"|\n"+ //22 float
                    "Non-Piston Blocks in Rotating Arm in Meters: |"+excess_meters+"|\n"+   //24 float
                    "Vertical Piston Step Length: |"+vp_step_length+"|\n"+  //26 float
                    "Use Unique Horizontal Step Length: |"+use_unique_hp_step_length+"|\n"+    //28 bool
                    "Horizontal Piston Step Length: |"+hp_step_length+"|\n"+    //30 float
                    "Retract Horizontal Piston before Vertical Step: |"+always_retract_hpistons+"|\n"+  //32 bool
                    "///////////////////////////////////////////\n"+
                    "4.) Advanced Configuration\n\n"+

                    "- Don't change these while a mining is in progress:\n\n"+

                    "Use Unique Piston Limits: |"+use_unique_piston_limits+"|\n"+   //34    bool
                    "Max Vertical Piston Limit: |"+max_vp_limit+"|\n"+  //36    float
                    "Min Vertical Piston Limit: |"+min_vp_limit+"|\n"+  //38    float
                    "Max Vertical Inv Piston Limit: |"+max_vp_limit_inv+"|\n"+  //40    float
                    "Min Vertical Inv Piston Limit: |"+min_vp_limit_inv+"|\n"+  //42    float
                    "Max Horizontal Piston Limit: |"+max_hp_limit+"|\n"+  //44    float
                    "Min Horizontal Piston Limit: |"+min_hp_limit+"|\n"+  //46    float
                    "Vertical Piston Speed: |"+vp_vel+"|\n"+    //48    float
                    "Horizontal Piston Speed: |"+hp_vel+"|\n"+    //50    float
                    "Rotor Rotation Speed at 10m: |"+rotor_vel_at_10m+"|\n"+    //52    float
                    "Piston Body Length in Meters: |"+piston_length+"|\n\n"+    //54    float

                    "Horizontal Piston Tag: |"+hp_tag+"|\n"+  //56 string
                    "Vertical Piston Tag: |"+vp_tag+"|\n"+  //58 string
                    "Inverted Piston Tag: |"+inv_tag+"|\n"+  //60 string
                    "Timer Advanced Tag: |"+adv_tag+"|";   //62 string

    Echo("Configuration Set to Custom Data!");
}
