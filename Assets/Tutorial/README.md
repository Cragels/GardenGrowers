# Tutorial: Gogo Thandi's Garden

Everything for the tutorial is in this folder. The only other files are the tutorial scene (`Assets/Scenes/Tutorial.unity`, a copy of Level) and its NavMesh (`Assets/Scenes/Tutorial/`). Level.unity is not changed.
The tutorial reads the group's systems (inventory, plots, time, bird) and never edits their scripts.

## Setup tools

Under **Tools > Garden Growers > Tutorial**:
1. **Build Gogo From Granny2**: makes `Art/Gogo/Gogo.prefab`.
2. **Create Tutorial Content**: makes the steps in `Data`, and updates Gogo's prefab.
3. **Set Up Open Scene**: adds the tutorial and Gogo to Tutorial.unity. Then bake `Tutorial NavMesh` and save.

The tutorial UI uses the group's art (`UI_Assets/Slot.png` and `Panel.png`) and Bangers. `Art/Fonts` has a static copy of Bangers made by the tools, so the tutorial's UI doesn't rely on the game's shared font asset.

## Folders

| Folder | What's in it |
| --- | --- |
| `Scripts/Tutorial` | Steps, conditions, actions and the manager that runs them |
| `Scripts/Grandma` | Gogo: movement, animation and the controller the tutorial talks to |
| `Scripts/Dialogue` | Lines, speech bubble, typing and voice |
| `Scripts/UI` | Goal panel, key prompts, marker, pop-ups and story cards |
| `Scripts/Camera`, `Core`, `FX` | Camera turn for conversations, small helpers, sounds |
| `Editor` | The setup tools and the debug window |
| `Data` | The steps, the sequence and Gogo's speaker profile |

## Steps

Each step in `Data/Steps` has a goal, a target for the marker, what Gogo says, key prompts, rules for when it's done (or skipped or failed) and actions like giving an item. `TutorialManager` runs them in the order of the sequence asset.

To add a step: right-click in `Data/Steps` > Create > Garden Growers > Tutorial > Tutorial Step, fill it in and add it to the sequence.

## Testing

- **Tools > Garden Growers > Tutorial > Debug Window** (Play mode): jump to any step, get the tools, grow the tomato or get 5 tomatoes. Open the bag (Tab) first so the mouse is free.
- **F8** skips the current step.
- To test the replant path, walk about 6 m away from the plant after the compost step and let the bird eat.

## Good to know

- The bird counts as chased off if you're within 6 m when it flees.
- Gogo follows at 3.6 m and steps aside if you get closer than 2.3 m.
- Selling at Gogo's stall also sells what you're holding. The group's sell script only counts the bag.
- `Bangers SDF.asset` (TextMesh Pro examples) is a dynamic font, so it changes by itself when the game runs. That change is safe to discard in Git.
