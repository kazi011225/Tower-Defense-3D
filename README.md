# Tower Defense 3D - Assignment 2

CSCI 381, Video Game Design and Development, Fall 2026
Kazi Anwar

Made with Unity 6 (Universal 3D / URP). Open the scene in `Assets/Scenes` and press Play.

## About the game
Enemies walk along a zig-zag road from the green circle to the red circle. You build towers next to the road to stop them. Towers shoot cannon balls that fly in a real gravity arc, and they aim at where the enemy is going to be, not where it is now. If even one enemy reaches the red circle, you lose. Clear all 5 waves to win.

I started from scratch instead of the class project, like the assignment suggested. The whole game is built from code: the `GameManager` script makes the ground, road, towers, enemies, and on-screen text when you press Play. That way the scene only needs one empty GameObject, and all the game settings are in one place in the Inspector.

## How to play
- Press **1**, **2**, or **3** to pick a tower type
- Move the mouse over the grass. The preview tower turns **green** if you can build there and **red** if you can't (too close to the road, too close to another tower, or out of towers). The text box also says why.
- **Left-click** to build
- Press **SPACE** to send the next wave
- **P** = switch aim mode between PREDICTIVE and DIRECT
- **Q / E** = turn the camera around the map
- **R** = restart

## C grade requirements
- **Player can place towers.** A ray goes from the camera through the mouse to the ground, and the tower is built at that spot if it passes the placement rules. (`GameManager.UpdatePlacement`, `GameManager.CheckSpot`)
- **Towers fire continuously at a set rate.** Each tower has a cooldown timer. It shoots, waits `fireCooldown` seconds, and shoots again as long as an enemy is in range. (`Tower.Update`)
- **Towers aim directly at an enemy.** Press P to switch to DIRECT mode. The tower aims at the enemy's current position, and its head turns to point the same way the ball will fly. (`Tower.AimAt`)
- **Enemies follow a set path.** The path is a list of waypoints. Each enemy moves toward the next point, and when it gets there it turns to the next one. (`Enemy.FixedUpdate`)
- **Game ends when an enemy finishes the path.** The enemy tells the GameManager, the game freezes, and a GAME OVER screen shows up. (`Enemy.FixedUpdate`, `GameManager.EndGame`)

## B grade requirements
- **3 tower types with different stats** (rate of fire, projectile speed, and tower height):

  | Tower | Shoots every | Ball speed | Height | Damage | Range |
  |---|---|---|---|---|---|
  | Cannon | 1.0 s | 16 | 2.5 | 2 | 14 |
  | Rapid | 0.3 s | 13 | 1.5 | 1 | 10 |
  | Sniper | 2.2 s | 30 | 6 | 5 | 24 |

- **Text overlay explains how to place towers.** A help box in the top left shows the 3 steps, the stats of each tower, which one is picked, and why a spot is blocked. (`GameManager.BuildHelpText`)
- **Limited number of towers.** You start with 5. You get 1 more after each wave you clear, so you have to choose where to build carefully. (`startingTowers`, `bonusTowersPerWave`)
- **Path is not a straight line.** The road has 8 points and 6 turns. (`GameManager.pathPoints`)

## A grade requirements
- **3 enemy types with different speed and HP:**

  | Enemy | Speed | HP | Look |
  |---|---|---|---|
  | Scout | 5.5 | 3 | small yellow capsule |
  | Soldier | 3.5 | 6 | red capsule |
  | Tank | 2.0 | 20 | big gray box |

  Each wave is a string of letters (S, M, T), so it's easy to change the mix. (`GameManager.waves`)
- **Non-basic assets.** 3D models from the Kenney Tower Defense Kit (being added, see Credits). Any tower or enemy type can use a model by dragging it into the Model slot in the Inspector. If the slot is empty, it uses a simple shape.
- **Predictive aiming.** The tower uses the enemy's current position and velocity to guess where it will be when the ball arrives (`Tower.AimAt`):
  1. Work out how long the ball takes to reach the enemy's current spot.
  2. Move the aim point forward: `position + velocity * flightTime`.
  3. Work out the flight time to the new point and repeat. After 4 rounds the guess stops changing.

  You can see the difference by pressing P during a wave. In DIRECT mode, shots land behind fast Scouts. In PREDICTIVE mode, they hit.

## How the physics works
- **Projectiles** are spheres with a Rigidbody and real gravity. The tower doesn't move them; it just gives them a starting velocity and the physics engine does the rest.
- **Launch math** (`Ballistics.Solve`). To hit a target at a given launch speed, the tower solves a quadratic for the flight time, then builds the launch velocity from it. This is based on the class example's `GetLaunchVector3D`, but I changed it to also return the flight time, because predictive aiming needs it. It uses the low arc so shots land faster.
- **Hits** use trigger colliders. When a ball touches an enemy, the enemy loses HP. When it touches the ground, it pops and goes away.
- **Enemies** use a kinematic Rigidbody moved with `MovePosition`, so they stay exactly on the path but still work with the physics system. Each enemy keeps track of its own velocity so towers can read it.

## Other things I added
- 5 waves, and you press SPACE when you're ready for the next one
- Towers target the enemy that is furthest along the path (the most dangerous one)
- Green / red preview tower when placing
- HP numbers over every enemy
- Hit effect when something gets hit, and a bigger one when an enemy dies
- Camera can turn with Q / E
- R restarts the whole game without reloading the scene
- UI scales to fit any screen size

## Scripts (`Assets/Scripts`)
- `GameManager.cs` builds the map, handles input, waves, tower placement, win / lose, and the on-screen text
- `Tower.cs` picks a target, aims (direct or predictive), and shoots
- `Ballistics.cs` launch velocity and flight time math
- `Enemy.cs` follows the path, tracks HP and velocity
- `Projectile.cs` the physics cannon ball and hit detection
- `HitEffect.cs` small pop animation

All the numbers (tower stats, enemy stats, waves, path points, number of towers) can be changed in the Inspector on the GameManager object.

## Credits
- 3D models: Tower Defense Kit by Kenney (kenney.nl), CC0 license
- Ballistics formula based on Professor Steinberg's class example (Tower-Projectiles)
EOF