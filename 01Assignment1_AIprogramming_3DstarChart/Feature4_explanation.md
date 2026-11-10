# Feature 4 – Spaceship flight: explanation of the code

Feature 4 turns the star chart into a small space-flight game, inspired by the game Elite
mentioned in the assignment brief. You can fly a spaceship freely between the 2,000 nearest
stars, see a cockpit HUD, search for any star, watch the ship from behind, and let an autopilot
fly you to a chosen star.

All the code was written with Claude (see `AI_programming_log.md`, Entries 23–27, for every
prompt, response and critique). This page explains what each part does and how it works.

## How to use it

| Key / action | What it does |
|---|---|
| **F** | Turn flight mode on or off |
| **W / S** | Speed up / slow down (never backwards) |
| **X** | Stop the ship |
| **Up / Down arrows** | Pitch the nose down / up |
| **Left / Right arrows** | Turn left / right |
| **Q / E** | Roll left / right |
| **C** | Switch between cockpit view and chase view |
| **T** | Autopilot on / off (needs a target) |
| Search box | Type part of a name, click a match or press Enter (Esc clears) |
| Click a star in a list | Outside flight mode: fly there. In flight mode: make it the target |

## The classes

### ShipController.cs – flying the ship

- **What it holds:** the ship's own position (`ShipPosition`) and two directions, `ShipForward`
  and `ShipUp`. The ship is separate from the camera, which is what makes the chase view possible.
- **Frame timing:** it uses WPF's frame timing (`CompositionTarget.Rendering`) with a `Stopwatch`,
  so the ship moves the same speed whatever the frame rate.
- **Smooth turning:** keys are remembered while they are held (`HandleKeyDown` / `HandleKeyUp`),
  so turning is smooth instead of in steps.
- **Each frame:**
  1. **Speed:** W and S change the speed by 1 pc/s every second. It stays between 0 and 5 pc/s.
  2. **Turning:** pitch, turn and roll rotate the forward and up directions around the ship's own
     axes (right, up and forward) with a `Quaternion`. Pitch and turn go at 45° per second, roll
     at 60° per second.
  3. **Straightening:** the directions are made length 1 and exactly at right angles again, so
     small rounding errors don't build up over a long flight.
  4. **Moving:** the ship moves forward by speed × time.
  5. **Placing the camera:** `PlaceCamera()` puts the camera at the ship (cockpit view), or
     0.25 pc behind and 0.06 pc above it, looking just ahead (chase view).
- **Autopilot (T):** `FlyAutopilot()` runs instead of the keys.
  - **Turning:** it turns towards the target around the axis at right angles to both "forward"
    and "towards the target", no faster than the normal turn rate.
  - **Braking in time:** the speed is limited to √(2 × acceleration × distance left). This is
    the fastest speed from which the ship can still brake to a stop before the target.
  - **Turning before flying fast:** it flies slowly while the target is more than 45° off the
    nose, and almost stops while the target is behind. Without this it would fly wide circles.
  - **Arriving:** it stops 0.3 pc (about 1 light year) from the star, turns itself off, and
    raises `AutopilotArrived`.
  - **Taking control back:** any steering or speed key turns the autopilot off.

### ShipModel.cs – the spaceship you see in the chase view

- **Built from HelixToolkit shapes:**
  - a cylinder body with a cone nose
  - box wings and a tail fin
  - a sphere canopy
  - a glowing orange sphere engine (an emissive material, so it glows without light on it)
- **Building it:** the model is built once around (0,0,0), pointing along +X.
- **Following the ship:** `SetPose()` builds one `Matrix3D` whose rows are the ship's forward,
  up and right directions plus its position. That moves and turns the whole model in one go.
- **Visibility:** it is only shown in the chase view.

### FlightHud.cs – what the cockpit display shows

`Read()` takes the ship's position, heading, speed and target, and returns five lines of text:
- **Speed**, in pc/s and ly/s (1 pc = 3.26156 ly).
- **Heading**, as a bearing and path in degrees.
- **The nearest star** and its distance.
- **Distance from Sol.**
- **Target:** the target star, its distance, how many degrees it is off the nose
  (`Vector3D.AngleBetween`), and whether the autopilot is on.

Every distance and angle comes from `Navigation.GetRoute`, the same method the Nearby stars
list uses. So the numbers always match the rest of the app, and the maths is not written twice.
To do this, the ship's position is turned into a temporary `Star`, and the heading is the route
to a point 1 pc in front of the ship.

### StarCollection.cs – new search methods

- `GetNearestStar(x, y, z)` checks every star and returns the closest one to a point. The HUD
  uses it for the nearest star.
- `SearchByName(text, max)`:
  - finds stars whose display name contains the text, ignoring upper and lower case
  - puts names that **start** with the text first, then the stars closest to the Sun
  - returns at most `max` stars

### MainWindow – putting it together

- **Where keys go:** F toggles flight mode. In flight mode, keys go to the ship. Otherwise they
  go to the Feature 2 camera keys. While you type in the search box, all keys go into the box.
- **Choosing a star:** `ChooseStar()` decides what choosing a star does: fly there with the
  Feature 3 `CameraAnimator`, or make it the ship's target in flight mode.
- **The HUD:** it is shown, and updated every frame, only while flight mode is on.
- **The search box:** clicking a match or pressing Enter chooses that star.

## How it was checked

- **Turning directions:** checked with a small maths test before running, so a positive pitch
  lifts the nose, a positive turn goes left, and a positive roll goes right.
- **HUD numbers:** checked against `stars.csv`. At position (−0.365, −0.262, −0.787) it showed
  Proxima Centauri at 1.28 ly and Sol at 2.96 ly, which match the data.
- **Chase camera:** checked from the camera info. With the ship at (0, 0, 2), the camera was at
  (0, 0.060, 2.250), exactly 0.25 pc behind and 0.06 pc above it.
- **Autopilot:** simulated before running.
  - The simulation found a rounding bug that stopped the ship from ever "arriving", which was
    fixed.
  - After the fix it reached every target tried, including a star behind the ship and 200
    random stars from the data.

## Limits

- **Speed:** speeds are game speeds (up to 5 pc/s, about 16 ly per second), far faster than light.
- **Scale:** the ship model is about 0.09 pc long, so it is not to scale.
- **No obstacle avoidance:** the autopilot flies straight and does not avoid other stars.
- **Search:** it only matches the display name, and Greek letters must be typed as symbols
  (τ Cet).
- **Mouse in flight mode:** dragging is overwritten every frame, because the ship controls the
  camera.
