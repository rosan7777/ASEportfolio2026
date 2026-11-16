# Advanced Software Engineering
## Semester 1 2026

# Name: Roshan Kumar Adhikari
# Student ID: 77466776
# Group: Wednesday 9-11
# Course: MSc Information Technology
# Tutor: Duncan Mullier

### [YouTube Demo Link for Assignment1](https://youtu.be/1EITdt6M2uM)

### [YouTube Demo Link for Assignment2](https://insertYourLinkHere)

Feel free to delete any of the information below to tidy up your readme, but do not delete the above information as it is used for marking and do not delete any files in the repo as some are used for building documentation for your assignments.


[Details on how to create a project in these repo directories.](https://github.com/LeedsBeckettUniversityASE/ASEresources/blob/main/VisualStudioProjects.md)

## Assignments

You must be logged into myBeckett to view the assignment spec. Sync your browswer with your LBU account.

[Assignment Specification](https://leedsbeckett-my.sharepoint.com/:b:/g/personal/d_mullier_leedsbeckett_ac_uk/IQAfMN9DgVEdQJIvbNKtkIhCAe4herw6o4nj_Iun4tXZeNU?e=JiytOR)

**See The Assignment Specification for assignment dates**. They are not repeated here becuase software engineers do not repeat things as this leads to bugs.
### [Useful Links for the Assignments](https://github.com/LeedsBeckettUniversityASE/ASEresources/blob/main/UsefulLinks.md).
These links are likely to be updated as more information is added, so check reqularly.
## Assignment 1: AI Programming 3D Star Chart
See the assignment spec for details. You are expected to implement a 3D star chart using AI programming techniques. The star chart should allow users to visualise stars in a three-dimensional space, with features such as zooming, rotating and filtering based on star properties.

You must explain your code in your demo or you will be asked to do it again/a live demo.

### My Assignment 1: Star Chart 3D

The project is in [`01Assignment1_AIprogramming_3DstarChart/StarChart3D`](01Assignment1_AIprogramming_3DstarChart/StarChart3D). It is a WPF desktop app that loads the 2,000 stars nearest to the Sun and shows them as a 3D star chart you can zoom, rotate, filter and fly through.

- **Feature 1 - Star Chart:** reads `stars.csv` into `Star` objects held in a `StarCollection`, and gives every star a display name (proper name, or a catalogue name such as tau Cet, 61 Cyg A, Gl 65A, HIP 82724).
- **Feature 2 - 3D Visualising:** draws every star as a small sphere in its spectral class colour with its name, with a switchable "only stars with a proper name" filter, and mouse and keyboard zoom, pan and rotate.
- **Feature 3 - 3D Movement:** flies to the Sun when it starts, shows dust drifting past for the illusion of motion, lists the 10 nearest stars with distance (light years), bearing and path (degrees), and flies to a star when you click it.
- **Feature 4 - Additional facilities (5):** spaceship flight mode, a cockpit HUD with crosshair, star search, a 3D ship model with a chase view, and a target lock with autopilot. See [`Feature4_explanation.md`](01Assignment1_AIprogramming_3DstarChart/Feature4_explanation.md).

#### My AI system and approach

I used Claude (Opus 5.5) through the claude.ai chat, not integrated into Visual Studio. I used two separate chats: one for programming, where every prompt, response and critique is in [`AI_programming_log.md`](01Assignment1_AIprogramming_3DstarChart/AI_programming_log.md), and one used only as an astronomy domain expert, logged in [`AI_domainExpert_log.md`](01Assignment1_AIprogramming_3DstarChart/AI_domainExpert_log.md), so that the domain facts were not mixed with the code. My approach was to ask for one small feature at a time, with clear limits in each prompt (which class to change and which classes not to touch), then build and test it myself in Visual Studio before committing, and write the critique at the time of the commit. I chose Claude because it could keep the whole project in one long conversation, it could check my real stars.csv data with scripts to give me expected values to test against, and it said clearly when it could not build or run the code, so I always tested it myself. I did not use a local LLM.

#### Environment setup

1. **Windows** with **Visual Studio 2026** and the **.NET desktop development** workload (WPF).
2. **.NET 10 SDK**. The project targets `net10.0-windows` with `UseWPF`.
3. **NuGet package:** `HelixToolkit.Core.Wpf` version **2.27.3**, for the 3D view, spheres, billboard text labels and mouse camera control. Visual Studio restores it on build. (`HelixToolkit.Wpf` was tried first but only targets .NET Framework and gave warning NU1701, see log Entry 9.)
4. **Star data:** `stars.csv`, the 2,000 stars nearest to the Sun, taken from the HYG v4.4 database. It is set to *Copy if newer* so it is copied next to the .exe on build.
5. **Git and GitHub:** one branch and one pull request per feature, merged into `main` with merge commits. I committed and pushed from PowerShell and Visual Studio.
6. **AI:** Claude Opus 5.5 at [claude.ai](https://claude.ai), used through the browser.

To run it, open `01Assignment1_AIprogramming_3DstarChart/StarChart3D/StarChart3D.slnx` in Visual Studio 2026 and press **F5**.

#### Controls

| Control | What it does |
|---|---|
| Mouse wheel / right-drag / Shift + right-drag | Zoom to the cursor / rotate / pan |
| `+` `-`, arrow keys, `W` `A` `S` `D`, `Home` | Keyboard zoom, pan, rotate and reset (when not in flight mode) |
| "Only show stars with a proper name" | Filter switch |
| Nearby stars list, search box | Click a star to fly there (or set it as the target in flight mode) |
| `F` | Flight mode on/off. In flight mode: `W`/`S` speed, `X` stop, arrows pitch/turn, `Q`/`E` roll, `C` chase view, `T` autopilot |

#### Class structure

| Class | Feature | What it does |
|---|---|---|
| `Star` | 1 | Data for one star (id, names, catalogue numbers, spectral type, distance, X/Y/Z in parsecs). Uses `init`, so a star can't be changed after loading. |
| `StarCollection` | 1, 3, 4 | Loads `stars.csv` into a read-only list. Finds the nearest stars to a star, the nearest star to a point, and searches by name. |
| `CatalogueNames` | 1 | Display name: proper name -> Bayer -> Flamsteed -> Gliese -> HD -> HIP -> HYG id, with A/B letters for binary stars. |
| `SpectralColours` | 2 | Spectral class (O B A F G K M L T and white dwarfs) to colour. Unknown is grey. |
| `StarRenderer` | 2 | Draws the stars as spheres (one mesh per colour) and the name labels, with the proper-name filter. |
| `KeyboardCameraController` | 2 | Keyboard zoom, pan, rotate and reset. |
| `Navigation` / `Route` | 3 | Route between two stars: distance in light years, bearing and path in degrees. |
| `CameraAnimator` | 3 | Smooth camera flight to a star (smoothstep easing, WPF frame timing). |
| `DustField` | 3 | 400 drifting dust points around the camera for the illusion of motion. |
| `NearbyStar` | 3 | One row of the Nearby stars list or the search results. |
| `ShipController` | 4 | Spaceship flight mode, cockpit and chase views, target and autopilot. |
| `ShipModel` | 4 | The 3D spaceship model built from HelixToolkit shapes. |
| `FlightHud` / `HudReading` | 4 | Works out the cockpit HUD text: speed, heading, nearest star, distance from Sol and target. |
| `MainWindow` | all | The window, panels and HUD. It connects the classes and decides where keyboard keys go. |

#### References

- Anthropic (2026) *Claude Opus 5.5* [Large language model]. Available at: https://claude.ai (used for all code; see the AI logs).
- Nash, D. (n.d.) *HYG Stellar Database v4.4* (astronexus). Available at: https://www.astronexus.com/projects/hyg
- Helix Toolkit (n.d.) *HelixToolkit.Core.Wpf 2.27.3*. Available at: https://www.nuget.org/packages/HelixToolkit.Core.Wpf

## Assignment 2: BOOSE Programming Language interpreter
See the assignment spec for details. You are expected to implement an interpreter for the BOOSE programming language. The interpreter should be able to parse and execute BOOSE code, handling various constructs such as variables, loops, conditionals, and functions.

You must explain your code in your demo or you will be asked to do it again/a live demo.

Edit the YouTube Links above, the part in the square brackets is the text you see and doesn't need to change. Put the url of your demo inside the round brackets and check it clicks through to your demo.

# Submitting Assignments
This applies to **both** assignments.

**You MUST do all of these steps or you will lose marks**. All details are in the **Assignment Specification**.

Apologies if this all seems a bit patronising but every year I get a handful who take longer to process than everyone else combined because they don't read instructions.
### All code must be part of the Classroom50 Classroom, using the this template repo. You should not commit after the deadline.
### You must provide a YouTube Demo that is uploaded as public but unlistable and the link must be provided in this README.md file above (edit the link). 
The demo should showcase the functionality of your assignments and demonstrate your understanding of the code. You must use a microphone and explain your code. 
See Assignment Specification and [YouTube demo software document](https://github.com/LeedsBeckettUniversityASE/ASEresources/blob/main/Recording_A_YouTube_Demo.md)
### You MUST submit links to your video, repo and upload your source code directory to **myBeckett**, this is the official submission that records your hand in time, 
If you do not do this on time it is officially not handed in/late.

[University's Flexible Submission Policy](https://drive.google.com/file/d/108-9vWaaakjDZKFBI6SffSGaSzmYbNJk/view?usp=sharing)

[University's Extenuating Circumstance Policy](https://www.leedsbeckett.ac.uk/student-information/exams-assessments-and-awards/mitigation-and-extenuating-circumstances/)

**Module Leaders cannot give out extensions, so don't ask, instead click the above link**
