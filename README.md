# Mind Palace VR

## Versions
- Unity version: 6000.0.61f1 LTS
- XR Interaction Toolkit version: 3.0.9
- Inference Engine: 2.2.2

## Unity project setup steps (just for future reference):
- XR Plugin Management: Edit -> Project Settings -> XR Plugin Management -> Install XR Plugin Management.
- XR Plugin Management -> Enable Open XR Plugin-in provider for Windows and Android.
- XR Plugin Management -> OpenXR -> Add "Oculus Touch Controller Profile" to the enabled interaction profiles for both Windows and Android.
- Window -> Package Manager -> Unity Registry -> Install "XR Interaction Toolkit" -> Samples -> Import "Starter Assets".
- XR Plugin Management -> Project Validation -> Click "Fix All" for Windows and Android.


## How to run on Unity (PCVR)
1. This project uses Git LFS for the large files. It must be installed before cloning the repo.
```shell
git lfs install

# this can take a couple of minutes depending on the internet speed since the project is around 1.5 GB
git clone https://github.com/psnjk/Mind-Palace-VR.git
```
2. Open the project in Unity (this can take a couple of minutes)
3. Go to Scenes->Rooms and open the MainHub scene.
4. Make sure that the Quest device is connected to the computer via the Meta Horizon Link app.
5. Start the MainHub scene.

## How to run on a Quest device
1. This project uses Git LFS for the large files. It must be installed before cloning the repo.
```shell
git lfs install

# this can take a couple of minutes depending on the internet speed since the project is around 1.5 GB
git clone https://github.com/psnjk/Mind-Palace-VR.git
```
2. Open the project in Unity (this can take a couple of minutes)
4. Make sure that the Quest device is connected to the computer and that the Quest device is in developer mode to enable adb.
5. File -> Build Profiles -> Choose Android -> Select the Quest as "Run Device".
6. Make sure that the following scenes are selected in the "Scene list": Scenes/Rooms/MainHub, Scenes/Rooms/Room1, Scenes/Rooms/Room2, Scenes/Rooms/Room3 .
7. Click on "Switch Platform" on the top right and wait for it to finish.
8. Click on "Build and Run", then choose where to put the compiled APK file and wait for it to compile.
9. The app should run automatically on the connected Quest device.



## Controls
- Locomotion:
    - Teleportation: push right thumbstick upwards and point where you want to teleport, it is also possible to change the rotation of the teleporation by moving the right thumbstick.
    - Continuous movement: use the left thumbstick.
    - Turning: push the right thumbstick left and right.

- Creating a new room:
    - On the HUB, there are three portals.
    - These portals each have a different room type behind them.
    - To create a new room, go into on of them.
    - You will now be in the room where you can spawn notes.
    - Exit the room using it's portal, the room will autosave.
    - You will now be back in the main hub.
    - The main hub will show this new room that was created in the corridor, where there will be a portal leading to the room.

- Saving a room:
    - Rooms are saved automatically while exiting the portal.

- Changing a room's name:
    - This can be done inside the room or outside of it in the main hub corridor.
    - There is a control panel next to the portal.
    - This control panel has its own keyboard and input field where the room name appears and where it can be changed.
    - Input the new name and click on the floppy disk UI button next to the input field.

- Deleting a room:
    - Look at the control panel next to the portal.
    - There is a delete UI button at the bottom right corner.
    - Long hold this button to delete the room.
    - The deletion can be canceled by letting go before the progress around the button fills up.

- Open hand menu: click on the left controller menu button.
- Spawning a note:
    - Open the hand menu and select what size note to spawn.
    - Hold the A button the right controller to get a preview before spawning, then release it to spawn the note.

- Clicking on UI buttons: use left or right trigger.

- Interacting with a spawned note:
    - Moving a note: 
        - A handle is found at the bottom of then note.
        - Point at the handle and hold on the controller's grip trigger to move the note around.
    - Writing on the note:
        - Click on a note's text area with a trigger button, then a virtual keyboard appears.
        - Type on the keyboard with the trigger buttons of the controllers.
        - To change the keyboard's focus to another note, simply click on that other note's text area and watch the blinking caret.
    - Changing the note color: 
        - Use the UI button on the note that has a palette icon to toggle a window where the color can be changed.
        - Click again on the UI button to hide the window.
    - Changing the text size and layout: 
        - Use the UI button on the note that has a gear icon to toggle a window where the text settings can be changed.
        - To change the font size, use the two arrows.
        - The other UI buttons below the font size settings are for the text vertical and horizontal text alignments. 
        - Click again on the UI button to hide the window.
    - Pinning / unpinning a note:
        - By default, notes look at the user all the time, but this behavior can be disabled to pin a note so that it doesn't look at the user anymore.
        - Use the UI button on the note that has a pin icon to pin the note. 
        - Click it again to unpin the note.
    - Using voice input on the notes:
        - Click on a note's text area with a trigger button, then a virtual keyboard appears.
        - On the keyboard click on the microphone button on the top left, it will listen for 20 seconds and then transcribe the audio and write it on the note. The HUD will show a microphone icon while listening and then an appropriate icon for when it will transcribing.
    - Deleting a note:
        - Long press the UI button on the note that has a cross icon.
        - The note is deleted when the progress around the icon fill up.
        - The deletion can be canceled by letting go before the progress fills up entirely.
    - Linking notes:
        - Use the UI button on the note that has a chain icon.
        - Around the corners of the note, some buttons will appear.
        - Click on one of them to specify where the link should start from.
        - Other notes on the scene will now also get the same buttons around their corners.
        - Linking can be canceled here by pressing on the B button on the right controller.
        - Click on another note's corner button to specify where the link should end.
        - There will now be a link between the two notes. 
    - Deleting a link between notes:
        - When hovering over a link, it will turn gray.
        - While hovering over a link, click on the trigger button, a UI button with a cross icon should appear.
        - Click again using the trigger button on the UI buttton that just appeard to delete the link.


