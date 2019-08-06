# Augmented Reality for Visually Impaired People (AR For VIPs)

http://arvips.squarespace.com/

AR for VIPs is an application designed to turn the Microsoft HoloLens 1 into an assistive device for visually impaired people. It does so by sonifying obstacles via the spatial mesh that the HoloLens creates, and using text recognition and text to speech algorithms from Google to allow blind users to read text at a distance.  

It was created by University of California, Berkeley, School of Information graduate students Dylan Fox, Alyssa Li, Anu Pandey, and Rohan Kar, and Virtual Reality at Berkeley undergraduates Rajandeep Singh, Manish Kondapalu, Teresa Pho, and Elliot Choi.

For information on HoloLens basics, application setup, and voice commands, see this Google document: https://docs.google.com/document/d/1kNmwy0BvAHv7f46hym_HN8SThqGqnV9dunIzMdukwFk/edit?usp=sharing

For information on getting your Unity environment setup, including download links for HoloToolkit 2017.4.3.0, see this Google document: https://docs.google.com/document/d/1bbTqdkJNIzG7EznGzk95gs6i77JLNr2ljUWSFLjtj6E/edit?usp=sharing

The text detection was largely based off of Jonathan Huang's paper - https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0210630. Github Link - https://github.com/eacooper/HoloLensSignARApp

The remainder of this ReadMe will focus on the components in Github and what they do.

## Unity Hierarchy

These are the components that appear in the upper left “Hierarchy” panel in Unity when you start the Mesh-Manipulation project.

#### MixedRealityCameraParent
This is the standard Camera as set by the HoloToolkit Mixed Reality>Apply Mixed Reality Settings option, with one adjustment: the MixedRealityCamera’s audio listener component was disabled and a new CameraAudioListener added that is rotated 180 degrees, in order to counteract the audio reversal we encountered.

#### Managers
These objects don’t appear physically in the scene, but they have important scripts attached that affect the project.

##### Input Manager
This is the default Input Manager as set by the HoloToolkit. It captures user inputs from many different sources. We made no modifications.

##### Speech Input Source
This component contains a “Keywords” list that includes all voice command keywords the application will listen for. To enter a new keyword, use the plus button in the bottom right of this component in the Inspector, then add it to the Speech Input Handler component in the ScriptManager (below). 

#### Script Manager
This component holds several vital scripts: Control Script, Speech Input Handler, and Obstacle Beacon Script. See “Script” section below.

##### Text Manager
This component holds the scripts related to image capture, text recognition and text to speech: Camera Manager, Icon Manager, Settings Manager, Text Reco, and Text to Speech Google. See “Scripts” below.

##### Spatial Processing
This component holds scripts related to spatial mapping and mesh processing: Play Space Manager, Surface Meshes to Planes, and Remove Surface Vertices. See “Scripts” below.

#### Default Cursor
This is the cursor as placed by HoloToolkit. No modifications.

#### Spatial Mapping
This component controls the spatial mapping scripts and serves as the parent object for the spatial mesh when the app is launched. By modifying the Spatial Mapping Observer values for Triangles per Cubic Meter and Time Between Updates, you can control the speed and fidelity of obstacle meshing.

#### Directional Light
A light to illuminate the beacons and other holograms. We did not modify it.

#### Objects
This holds the non-mesh objects that can be seen in the application.

##### Obstacle Beacon Manager
This holds all obstacle and wall beacons.

##### Text Beacon Manager
This holds all text beacons.

##### Test Cube
A cube that changes color based on input. Currently disabled. 

##### Debug Window
This is a collection of three pieces of debug information: Spotlight Text and Proximity Text, which indicate whether Spotlight and Proximity modes are on respectively; and Debug Text, which prints a copy of all text sent to the Debug window for in-app debugging. This object is Tagalong and Billboard, which means it follows the user and stays rotated towards them. It can be toggled on or off with the “Toggle Debug” command, and cleared with the “Clear Debug” command.

## Scripts
#### Control Script
This script serves as the central nervous system of the application in several ways. It connects many of the important scripts, and converts input from the user into activation of the core scripts. It needs to be assigned the Text Manager, Text Beacon Manager, Sample Text, and Debug Text objects from the inspector.  It works closely with the Obstacle Beacon Script and the TextToSpeechGoogle script.

It has the following method regions:

**Initialization**
Sets up variables, sets up gesture and debug message events. 

**Gesture Controls**
Currently set to use the tap gesture to turn obstacle mode on or off. Formerly I experimented with using tap and hold or manipulation to trigger other commands, but the different gestures interfered with one another too much.

**Core Commands**
These are primarily triggered by voice commands, as set in the Speech Input Source and Speech Input Handler objects. They are used to turn obstacles on or off, trigger the Text Manager’s capture text process, or to trigger the Read Text Routine.

Note that Read Text must use a coroutine because A) there is a delay in getting the text files back from Google Text to Speech, and B) the method needs to be able to wait for the current string to be read before reading the next.

If using Read All Text, it goes through every beacon in the Text Beacon Manager; if using Read Text, it performs a conecast and reads only the text that the user is pointing their head at. The size and angle of this may still need adjusting.

StopPlayback, nextPlayback, and repeatPlayback all serve to make the read text function more smoothly, allowing user to stop playback, skip to the next string, or repeat the last played string. Increase and decrease Speed allow the user to adjust the speed of audio playback to their preference.

**Clear Beacons** 
This allows the user to clear obstacle beacons (which also turns obstacle mode off) or clear text beacons. WARNING: Clearing text beacons can cause serious errors in some circumstances when combined with the “Repeat” command. (exact circumstances unknown)

**Adjust Obstacle Beacon Cone**
This adjusts the obstacle and wall beacons that are triggered at once. You can adjust the number of beacons placed on each refresh, and adjust the deviation of the cone (low = tight cone, high = all around user).

**Mesh Processing**
Stops scanning to create wall, ceiling, and floor planes; or restarts scanning.

**HoloLens Debug UI**
This section writes debug content to the debug text window, and also allows user to clear it and toggle it on and off.

#### Help Attribute
This script simply helps display the help text in the inspector for Script Manager.

#### Obstacle Recognition Scripts
**Obstacle Beacon Script**
This is the second most important script in the application after Control Script. It manages the functions & part of the interfaces for all obstacle and wall beacons.

**Shoot Beacon**
This can be used to shoot a single beacon. Primarily used for testing purposes.

**ConeCastExtension**
This is used to turn spherecast into "conecast," making it more faithful to the "spotlight" metaphor. Script taken from https://github.com/walterellisfun/ConeCast 

#### Text Recognition Scripts

#### Mesh Processing Scripts
**PlaySpaceManager**
This script controls the surface observer object that creates the spatial mesh. You can use it to adjust various components of the meshing process, including the ability to process the mesh. Note that it works with the "Spatial Processing" script in the HoloToolkit.

#### Audio Scripts
**ObstacleAudio** 
This script is currently not used, but is intended to adjust the pitch of obstacles based on their height compared to the user.


## Prefabs
#### Obstacle Beacon (Linear and Logarithmic)
These are the beacons used for basic obstacle detection. The only difference between Linear and Logarithmic is in the 3d Sound Settings distance dropoff curve; linear is tuned to be audible from farther away, whereas logarithmic makes it easier to tell as you get closer to a beacon.

Note that the Sphere Collider Radius can be adjusted to determine how many beacons will pack into an area.

Note that the Audio Clip attached to this object is a very important part of sound design!

#### Wall Beacon (Linear and Logarithmic) 
Same as Obstacle Beacon, but uses a more subtle sound clip. Placed on meshes identified as walls.

#### Text Beacon
These are placed whenever text is identified at the approximate location of the text. The "Text Instance Script" attached it to it holds the text that the beacon represents.

#### Spatial Processing
This object goes with the managers and contains scripts to perform spatial processing.

#### Spatial Mapping (1)
This is the default mesh that is used in the Unity preview window. If running the application in the HoloLens, it will be ignored.

#### Text Camera
This camera is instantiated whenever Capture Text is called so that the application can keep track separately of what was in view when the command was called.


## Audio Samples



## Scenes

#### Control Test
The main scene used for our application. Includes all of the objects listed in the Unity Hierarchy above.

## Resources

#### HoloToolkit
A collection of important assets for HoloLens functioning in Unity. See: 
https://github.com/Microsoft/MixedRealityToolkit-Unity/releases/tag/2017.4.3.0-Refresh

#### HoloToolkit Examples
Examples of how assets in the HoloToolkit can be used.

#### Mixed Reality Toolkit (Audio Manager)
The audio manager had to be taken from a separate version of the HoloLens toolkit.

#### Simple JSON
Used to help parse the JSON received as part of text recognition.


For more information, contact dylan dot r dot fox at berkeley dot edu.