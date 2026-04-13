# Goo Ascension MVP Draft

## Goal

Ship one playable exhibit demo of Goo Ascension that proves the core loop across 3 scenes:

1. Scene 1 introduces the world with a forest-like opening
2. Scene 2 teaches the robot/rock enemy space in the cave
3. Scene 3 delivers the airship finale after a teleport transition
4. The player uses transformation, traversal, and weapon switching through the slice
5. The demo ends with a short teaser cutscene rather than a full ending

This draft excludes map scene art polish and map UI/minimap work, per request.

## What Already Exists

### Core player loop is already in code

- Slime to human transformation gated by a code fragment/pedestal-style collectible
- Human movement kit: jump, dash, dash i-frames, form switching
- Water interaction differences between slime and human
- Sword combo attacks, charged sword attack, gun mode, charged shot, parry flow
- Health and energy systems with energy gain on enemy kill

### Enemies and boss are already partly MVP-capable

- Sentinel minion AI with patrol, chase, telegraphed attacks, parry window, stagger
- Boss AI with melee, laser, stomp, phase 2 behavior, and health bars

### Content plumbing exists

- NPC dialogue system with typing, portraits, pause handling, and interaction prompts
- Scene transition trigger script exists

### Important scope correction

- The deleted Sunken Spires generator should not be treated as part of the MVP plan
- The current repo does not show scene files in the workspace snapshot, so scene flow still needs to be concretely authored and wired in Unity

## What Is Missing For An MVP

These are the highest-value needs outside map scene building and map UI.

### 1. A real 3-scene start-to-finish game state flow

Needed:

- Reliable spawn/bootstrap flow in Scene 1, Scene 2, and Scene 3
- Clear objective state progression:
  - scene 1 introduction
  - scene 2 cave progression and enemy teaching
  - teleport transition into airship
  - scene 3 climax and teaser ending
- Clear fail/retry behavior beyond simple scene reload on death

Why it matters:

The individual mechanics exist, but the demo still needs a dependable scene-to-scene progression controller so it feels authored instead of a collection of working systems.

### 2. Teaser ending and final resolution

Needed:

- Final trigger for the end of Scene 3
- Short teaser cutscene or story card
- Return to menu or exhibit loop reset after the teaser

Why it matters:

Right now there is no visible exhibit-complete state tied to the end of the slice.

### 3. Progression scope cut or replacement

Currently absent in code:

- XP and level-up system
- Talent unlocks / build choices

Confirmed MVP recommendation:

- Cut XP and talents from MVP
- Replace them with fixed unlock progression:
  - transformation unlock if needed for the story beat
  - weapon switching stays in MVP

Why it matters:

Trying to finish RPG-style progression now will slow the slice without improving the proof of fun.

### 4. Scene-specific encounter scripting and gating

Needed:

- Scene 1 onboarding gates
- Scene 2 cave combat/traversal teaching beats
- Teleport trigger from Scene 2 to Scene 3
- Scene 3 climax setup and teaser trigger
- At least one checkpoint or gameplay save/load path

Why it matters:

The combat AI exists, but encounter framing is what turns mechanics into a coherent level flow.

### 5. Lightweight gameplay save/load or checkpoint support

Needed:

- Save system for gameplay state, not just settings
- Or a checkpoint system that covers exhibit flow cleanly
- Persist minimal state such as:
  - current scene
  - checkpoint spawn
  - unlocked abilities or forms
  - possibly current weapon state

Why it matters:

The existing Save button currently saves settings only. For an exhibit, players need a reliable resume or retry path.

### 6. Combat readability and feel pass

Needed:

- Finalize enemy and boss damage numbers
- Finalize energy costs and regen pacing
- Tighten attack timing, hit-stop, knockback, and telegraph readability
- Verify weapon switching is readable and not abusable
- Verify rock mobs and robot mobs have distinct silhouettes and behaviors in Scene 2

Why it matters:

The mechanics are present, but MVP success depends more on feel and clarity than on adding more systems.

### 7. Audio and feedback minimum pass

Needed:

- One reliable music loop per menu / level / boss state
- Core SFX coverage for:
  - transform
  - dash
  - sword hit
  - gun charge/fire
  - parry success
  - enemy death
  - boss attacks / death
- Minimum particles / screenshake on important hits

Why it matters:

Without this, the slice will feel unfinished even if the mechanics work.

### 8. Scene hookup and validation

Needed:

- Verify all inspector references, tags, layers, colliders, and prefabs are wired correctly
- Confirm scene transitions, teleport destination, spawn points, and teaser trigger are wired correctly
- Confirm boss, NPC, projectile, and health bar prefabs are production-ready

Why it matters:

This project already depends heavily on inspector wiring. MVP stability will mostly fail on setup issues before it fails in code.

## Recommended MVP Scope

### Keep in MVP

- Slime and human forms
- Dash
- Sword combat
- Gun weapon switch
- Charged attacks only if stable in playtest
- Parry for minions and boss if it is readable enough
- Three scenes total
- One teleport transition from Scene 2 to Scene 3
- Two or three NPC conversations max
- One final set-piece or boss fight in Scene 3
- One teaser cutscene/card at the end
- One gameplay save/load or checkpoint path
- Health and energy UI only

### Cut from MVP

- XP / level-up
- Talent tree or talent unlock menu
- Additional regions beyond the 3 exhibit scenes
- Advanced water system expansion beyond current traversal use
- Extra NPC quest chains
- Multiple bosses
- Deep save system

### Optional keep-if-stable

- Boss screen-space HP bar
- One checkpoint before boss
- One short post-boss narrative beat

## MVP Deliverables

For a clean exhibit build, I would target these deliverables:

1. Scene 1 starts reliably and teaches the premise fast
2. Scene 2 establishes robot/rock enemies and cave traversal clearly
3. Teleport from Scene 2 to Scene 3 works every run
4. Scene 3 reaches a satisfying climax and ends with a teaser
5. Weapon switching works reliably through the whole demo
6. Save/load or checkpoint flow is intentional and testable
7. Dialogue, audio, and HUD are good enough for exhibit players

## Suggested Build Order

1. Lock the 3-scene route and transition points
2. Implement gameplay save/load or checkpoint behavior
3. Add the final teaser cutscene trigger
4. Validate prefab and inspector hookups in each scene
5. Tune combat and weapon switching readability
6. Add minimum audio and impact feedback
7. Run short playtests and cut unstable features

## Current Recommendation

If the target is a March 25 exhibit MVP, the fastest path is:

- keep the transformation loop only if it supports the 3-scene story
- keep weapon switching
- skip XP and talents entirely
- build only 3 scenes
- use one clean teleport from Scene 2 to Scene 3
- end with a teaser cutscene/card
- implement lightweight gameplay save/load or a checkpoint system

That gives you a legitimate vertical slice without overbuilding systems the current codebase does not yet support.

## Finalized Scope Decisions

1. Player state persists across scenes: health, energy, unlocks, and weapon state
2. Scene 1 includes easy introductory enemies
3. Scene 3 should aim for boss fight plus teaser/cutscene; if schedule slips, boss fight plus cutscene is the fallback
4. Save system target is both autosave and manual save
5. Save/load should support a loader with at least 10 save slots
6. Transformation stays gated behind collecting the code fragment

## Final MVP Structure

### Scene 1: Forest Introduction

- Establish tone and basic controls
- Open with a cutscene
- Start with exploration, NPC contact, and traversal while the player is still slime-only
- Place the code fragment mid-scene after meeting the NPC
- No enemies appear before the fragment is obtained
- Introduce a small number of easy enemies only after transformation becomes available
- Introduce movement, transformation, and first combat onboarding in one scene
- End with a transition into the cave section

### Scene 2: Cave / Robot and Rock Mobs

- Teach the main enemy language for the exhibit
- Use traversal and combat together
- End with a teleport event into Scene 3

### Scene 3: Airship Finale

- Carry over player state from prior scenes
- Deliver the final set-piece
- Preferred ending: boss fight followed by teaser cutscene
- Acceptable fallback: boss fight directly into teaser cutscene

## Required Systems For This MVP

### Must build

- Multi-scene progression controller
- Persistent player state across scenes
- Code fragment unlock flow
- Teleport transition from Scene 2 to Scene 3
- Gameplay save system with:
  - autosave
  - manual save
  - load menu with at least 10 slots
- End-of-demo teaser cutscene trigger

### Must validate

- Weapon switching reliability across all scenes
- Enemy onboarding after the Scene 1 fragment unlock
- Transformation persistence after unlock
- Boss or final encounter completion path
- Save/load restoring the correct scene and player progression state

## Suggested Save Data Minimum

To keep scope realistic, the first save format should only track:

- current scene name
- player position or named spawn/checkpoint id
- current health
- current energy
- current weapon mode
- whether code fragment was collected
- any critical scene progression flags
- save slot metadata such as timestamp and slot name

Save UI decision:

- 10 numbered manual save slots with timestamps
- 1 separate hidden autosave slot

Do not expand into a deep RPG-style save system for this exhibit.

## Remaining Questions

No blocking scope questions remain. The MVP is finalized for planning purposes.