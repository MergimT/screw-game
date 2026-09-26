# Screw Puzzle 3D

Browser 3D screw-sorting puzzle built with Three.js (vendored, no build step).

Run: `python3 -m http.server 8080` then open http://localhost:8080

- Tap an unblocked screw to unscrew it; it goes to the matching color box or the 5-slot tray.
- A box with 3 screws completes and is replaced; planks fall when all their screws are gone.
- Levels are procedurally generated and verified solvable by a greedy solver (src/logic.js).
