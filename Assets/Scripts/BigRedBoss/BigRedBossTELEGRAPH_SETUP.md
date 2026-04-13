# Enemy Attack Telegraph Implementation Guide

## Overview
This system adds visual telegraph warnings that appear before enemy attacks, giving players time to react and dodge incoming attacks.

## Components Added

### 1. AttackTelegraph.cs
A new component that handles displaying the telegraph visual effect. It:
- Shows a pulsing red indicator at the attack point
- Fades in and scales up, then fades out
- Can be customized with color and duration

### 2. Updated EnemyAI.cs
Modified to:
- Reference and trigger the telegraph effect
- Show the telegraph before starting the attack animation
- Includes a `telegraphDelay` setting (customizable in inspector)

## Setup Instructions

### For Each Enemy Attack Point in Your Scene:

1. **Find the Attack Point GameObject**
   - Navigate to your enemy prefab
   - Locate the child object used as the attack point (e.g., "AttackPoint" or similar)

2. **Add the AttackTelegraph Component**
   - Select the attack point in the hierarchy
   - In the Inspector, click "Add Component"
   - Search for and add "AttackTelegraph"

3. **Configure the Telegraph (Optional)**
   - In the AttackTelegraph component, you can customize:
     - **Telegraph Duration**: How long the telegraph animation lasts (default: 0.5s)
     - **Telegraph Color**: The color of the telegraph warning (default: red semi-transparent)

4. **Ensure Attack Point Has a Sprite**
   - The attack point should have a SpriteRenderer component
   - You can use any simple sprite (circle, square, etc.) as the telegraph visual
   - If no sprite exists, the AttackTelegraph component will auto-create a SpriteRenderer
   - Note: The sprite will be replaced with the telegraph color during attacks

## How It Works

1. When an enemy enters attack range, the `TriggerAttack()` method is called
2. The telegraph shows up with a pulsing animation (0.5 seconds by default)
3. While the telegraph is displaying, the attack animation plays
4. After both complete, the enemy can attack again

## Customization Options

In **EnemyAI Inspector**:
- `telegraphDelay`: Additional delay between telegraph and damage (currently not used in DamageTarget, but ready for implementation)

In **AttackTelegraph Inspector**:
- `telegraphDuration`: Duration of the telegraph pulse effect
- `telegraphColor`: Color and transparency of the telegraph warning

## Visual Effect Details

The telegraph uses a pulse animation that:
- Scales the sprite up by 20% while fading in slightly
- Then shrinks back down while fading out completely
- Total duration customizable (default 0.5 seconds)

This gives players a clear visual warning that an attack is imminent!
