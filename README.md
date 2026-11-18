# Tower Defense 3D (CSCI 381, Assignment 2)

A 3D tower defense game made in Unity 6 with real physics projectiles.

## How to play
- Press **1**, **2**, or **3** to pick a tower type
- Move the mouse over the grass. Green preview = you can build, red = you can't
- **Left-click** to build the tower
- Press **SPACE** to send the next wave
- **P** = switch aim mode (Predictive / Direct)
- **Q / E** = turn the camera
- **R** = restart

You lose if any enemy reaches the red circle. You win if you clear all 5 waves.

## How it meets the requirements

**C grade**
- Player places towers with the mouse
- Towers fire at a fixed rate (`fireCooldown`)
- Towers aim at an enemy (press P for DIRECT mode to see plain aiming)
- Enemies follow a set path of waypoints
- Game ends when an enemy reaches the end of the path

**B grade**
- 3 tower types with different rate of fire, projectile speed, and tower height (Cannon, Rapid, Sniper)
- On-screen text box explains how to place towers
- Limited number of towers (start with 5, +1 after each cleared wave)
- Path is a zig-zag, not a straight line

**A grade**
- 3 enemy types with different speed and HP (Scout, Soldier, Tank)
- Non-basic assets: imported 3D models (being added, see Credits)
- Predictive aiming: towers use the enemy's current position and velocity, plus the projectile's flight time under gravity, to aim where the enemy will be (`Tower.AimAt` + `Ballistics.Solve`)

**Extra**
- Projectiles fly in a real gravity arc (Rigidbody), and the launch math solves for the arc
- Wave system with 5 waves
- Tower placement preview (green / red ghost) with rules (not on the road, not on other towers)
- HP shown over each enemy, hit effects, camera rotation, restart

## Scripts (Assets/Scripts)
- `GameManager.cs` builds the map, handles input, waves, placement, and UI
- `Tower.cs` targeting, aiming (direct + predictive), shooting
- `Ballistics.cs` launch velocity + flight time math
- `Enemy.cs` path following, HP, velocity
- `Projectile.cs` physics ball, damage on hit
- `HitEffect.cs` small hit animation

## Credits
- 3D models: Tower Defense Kit by Kenney (kenney.nl), CC0 license
- Ballistics formula based on the class example project