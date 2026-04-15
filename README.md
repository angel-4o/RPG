# Madbox Game Developer Test: RPG "Game Feel"

## Overview

The goal was to take the base prototype and improve its combat loop through better feedback, timing, and enemy behavior — making interactions feel more deliberate and satisfying.

---

## Hero Changes

**Two-Phase Charging Mechanic**
Replaced instant attacks with a charge system. Holding the joystick builds charge; releasing triggers the attack. The longer the charge, the greater the weapon's size, range, and damage output.

This introduces a risk/reward dynamic — the player must manage positioning while charging, and lands a more impactful hit as a reward.

Additional feedback: HP bar and camera shake on damage to reinforce the stakes of combat.

---

## Enemy Changes

**Predatory Lunge Behavior**
Enemies now follow a three-phase state machine:

1. **Chase** — approaches the player to close distance
2. **Lunge** — locks onto the player's position and dashes at high speed
3. **Recovery** — briefly frozen after the lunge, creating an opening for the player

This creates a readable rhythm: dodge the lunge, then punish the recovery window with a charged attack.

---

## Time Breakdown

| Area | Time |
|---|---|
| Hero (charge mechanic, visuals, UI, camera shake) | 2h |
| Enemies (lunge state machine, tuning, particles) | 1.5h |
| Polish & bug fixing | 30m |

---

## Notes on the Base Project

The architecture was clean and easy to extend — the MVC separation made it straightforward to add new logic without touching unrelated systems.

One personal preference: for larger projects I tend to favor Dependency Injection over Service Locator for more explicit dependency graphs. For a prototype of this scale though, the existing setup worked well.

---

## If I Had More Time

- **Knockback** — physics-based enemy reactions on hit to reinforce weapon impact
- **Swarm director** — escalating enemy patterns (scouts → coordinated attacks) instead of uniform spawning
- **Weapon variety** — different weapon archetypes (heavy vs. fast) to stress-test the charging mechanic
- **Animation polish** — smoother transitions between charge phases