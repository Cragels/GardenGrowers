using System.Collections.Generic;
using GardenGrowers.Dialogue;
using GardenGrowers.NPC;
using GardenGrowers.FX;
using GardenGrowers.Tutorial;
using GardenGrowers.Tutorial.Actions;
using GardenGrowers.Tutorial.Conditions;
using GardenGrowers.UI;
using UnityEditor;
using UnityEngine;

namespace GardenGrowers.EditorTools
{
    /// <summary>
    /// Tool 2: makes Gogo's profile, every tutorial step and the sequence as editable assets.
    /// Running it again rebuilds them and keeps their GUIDs.
    /// </summary>
    public static class TutorialContentBuilder
    {
        public const string DataFolder = "Assets/Tutorial/Data";
        public const string StepsFolder = DataFolder + "/Steps";
        public const string SequencePath = DataFolder + "/Gogo Tutorial.asset";

        private static ItemData shovel, wateringCan, fertiliser, tomatoSeeds;
        private static Sprite seedPacketIcon;

        [MenuItem("Tools/Garden Growers/Tutorial/2. Create Tutorial Content", priority = 2)]
        public static void CreateFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<TutorialSequence>(SequencePath) != null)
            {
                bool overwrite = EditorUtility.DisplayDialog("Tutorial Content",
                    "Tutorial content already exists. Rebuild it?\n\nThis overwrites any edits you made to the steps and lines.",
                    "Rebuild", "Cancel");
                if (!overwrite) return;
            }

            if (!LoadGameData()) return;

            GogoBaker.EnsureFolder(StepsFolder);
            SpeakerProfile gogoProfile = BuildSpeaker();
            TutorialSequence sequence = BuildSequence();
            UpdatePrefab(gogoProfile);

            AssetDatabase.SaveAssets();
            Selection.activeObject = sequence;
            EditorGUIUtility.PingObject(sequence);
            EditorUtility.DisplayDialog("Tutorial Content", "Created " + sequence.steps.Count + " steps in\n" + StepsFolder, "Nice");
        }

        private static bool LoadGameData()
        {
            shovel = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Tools/Shovel.asset");
            wateringCan = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Tools/WateringCan.asset");
            fertiliser = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Tools/Fert.asset");
            tomatoSeeds = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Data/Tools/Seeds/Tomato Seeds.asset");
            seedPacketIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI_Assets/SeedPacket.png");

            if (shovel != null && wateringCan != null && fertiliser != null && tomatoSeeds != null) return true;
            EditorUtility.DisplayDialog("Tutorial Content",
                "Couldn't find the Shovel, WateringCan, Fert or Tomato Seeds assets in Assets/Data/Tools. Did they move?", "OK");
            return false;
        }

        //Gogo

        private static SpeakerProfile BuildSpeaker()
        {
            SpeakerProfile profile = LoadOrCreate<SpeakerProfile>(GogoBaker.SpeakerPath);
            profile.displayName = "Gogo Thandi";
            profile.voicePitch = 1.08f;
            profile.pitchVariation = 0.12f;
            profile.charactersPerSecond = 34f;
            profile.voiceVolume = 0.45f;
            profile.textColor = Color.white;
            profile.nameColor = new Color(1f, 0.61f, 0f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        /// <summary>Gives Gogo's prefab her profile, the bubble's font and art, and the default animation settings.</summary>
        private static void UpdatePrefab(SpeakerProfile profile)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GogoBaker.PrefabPath) == null) return;

            GameObject contents = PrefabUtility.LoadPrefabContents(GogoBaker.PrefabPath);
            try
            {
                var speaker = contents.GetComponent<DialogueSpeaker>();
                if (speaker != null)
                {
                    var serialized = new SerializedObject(speaker);
                    serialized.FindProperty("profile").objectReferenceValue = profile;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                var bubble = contents.GetComponentInChildren<SpeechBubble>(true);
                if (bubble != null) bubble.Configure(TutorialArt.Font(), TutorialArt.Bar);

                var animator = contents.GetComponentInChildren<GrandmaAnimator>(true);
                if (animator != null) animator.UseDefaultSettings();

                PrefabUtility.SaveAsPrefabAsset(contents, GogoBaker.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        //The steps

        private static TutorialSequence BuildSequence()
        {
            var steps = new List<TutorialStep>();
            TutorialStep S(string file) => LoadOrCreate<TutorialStep>(StepsFolder + "/" + file + ".asset");

            // Create every asset first so steps can point at each other.
            TutorialStep welcome = S("00 Welcome");
            TutorialStep findSpade = S("01 Find The Spade");
            TutorialStep holdSpade = S("02 Hold The Spade");
            TutorialStep dig = S("03 Dig The Soil");
            TutorialStep plant = S("04 Plant The Seeds");
            TutorialStep findCan = S("05 Find The Watering Can");
            TutorialStep water = S("06 Water The Seeds");
            TutorialStep sunset = S("07 Sunset And Rest");
            TutorialStep weeds = S("08 Pull The Weeds");
            TutorialStep waterAgain = S("09 Water Again");
            TutorialStep compost = S("10 Compost");
            TutorialStep bird = S("11 Chase The Bird");
            TutorialStep harvest = S("12 Harvest");
            TutorialStep sell = S("13 Sell At The Stall");
            TutorialStep buySeeds = S("14 Buy Seeds");
            TutorialStep walkHome = S("15 Walk Home");
            TutorialStep seedTin = S("16 The Seed Tin");
            TutorialStep replant = S("R1 Replant");
            TutorialStep rewater = S("R2 Water Again");
            TutorialStep recompost = S("R3 Compost Twice");

            //ACT 1: why we grow

            welcome.designNote = "Hook and theme first. Gogo walks up, the camera watches her. Food insecurity is shown through the family's situation, not a lecture.";
            welcome.gogo = GogoDirective.ApproachPlayer;
            welcome.onStart = Actions(
                new SetClockSpeedAction { gameMinutesPerSecond = 0.5f }, // slow day, so it stays light while you learn
                new HideBirdAction(),
                new SetTargetActiveAction { targetId = "sellCounter", active = false });
            welcome.intro = Talk(
                L("Sawubona, mzukulu! You're up early.", Mood.Happy, GestureType.Wave),
                L("I checked the kitchen. Only a little mealie meal is left.", Mood.Worried),
                L("Food costs more every month. Our money doesn't stretch like it used to.", Mood.Sad),
                L("But look at this yard. Before the rubbish came, it fed our whole family.", Mood.Thinking),
                L("Let's make it grow again. You and me!", Mood.Excited));

            findSpade.objective = "Find Gogo's old spade";
            findSpade.designNote = "Teaches moving, looking and picking up, without saying 'press W'. The marker and the goal do the teaching.";
            findSpade.targetId = "spade";
            findSpade.intro = Talk(L("First we need my old spade. I left it by the garden beds.", Mood.Neutral, GestureType.Point));
            findSpade.hints = Hints(("WASD", "Walk"), ("Mouse", "Look"), ("E", "Pick up"));
            findSpade.completeWhen = new HasItemCondition { item = shovel };
            findSpade.nudge = Say(L("The spade is by the garden beds. Follow the marker!", Mood.Neutral, GestureType.Point));
            findSpade.success = Say(L("That's the one! We dug many gardens together, that spade and me.", Mood.Happy));
            findSpade.celebrate = true;

            holdSpade.objective = "Hold the spade";
            holdSpade.designNote = "The bag is the least obvious system in the game, so it gets its own step, pulsing slot and prompts.";
            holdSpade.intro = Say(L("A spade in a bag can't dig! Take it out and hold it.", Mood.Happy));
            holdSpade.hints = Hints(("Tab", "Open your bag"), ("Click", "Take the spade"), ("Tab", "Close your bag"));
            holdSpade.highlightInBag = shovel;
            holdSpade.stuckSeconds = 12f;
            holdSpade.completeWhen = new HoldingToolTypeCondition { toolType = EquipmentData.ToolType.Shovel };
            holdSpade.nudge = Say(L("Open your bag and take out the spade, my child.", Mood.Neutral));
            holdSpade.success = Say(L("Yebo! Now you look like a real farmer.", Mood.Proud));

            dig.objective = "Dig up a garden bed";
            dig.designNote = "Any bed counts, so players choose. The marker only suggests the nearest one.";
            dig.targetId = "plot";
            dig.intro = Talk(L("See the rocks in that garden bed? Dig them out so the soil can breathe.", Mood.Neutral, GestureType.Point));
            dig.hints = Hints(("E", "Dig"));
            dig.completeWhen = new PlotStatusCondition { atLeast = PlotScript.PlotStatus.Dug };
            dig.nudge = Say(L("Walk up to a bed with rocks and dig.", Mood.Neutral, GestureType.Point));
            dig.success = Say(L("Look at that soil! Soft and dark. Plants will love it.", Mood.Excited));
            dig.celebrate = true;

            plant.objective = "Plant the tomato seeds";
            plant.designNote = "First 'test' of the bag: prompts only appear if the player gets stuck (Portal style: teach, then test). Seeds carry emotional weight.";
            plant.targetId = "@activePlot";
            plant.onStart = Actions(new GiveItemAction { item = tomatoSeeds, amount = 1 });
            plant.intro = Talk(
                L("These are my last tomato seeds. I kept them for a special day.", Mood.Proud),
                L("Today is that day. Plant them in your soil.", Mood.Happy, GestureType.Point));
            plant.hints = Hints(("Tab", "Hold the seeds"), ("E", "Plant"));
            plant.hintTiming = HintTiming.WhenStuck;
            plant.stuckSeconds = 10f;
            plant.highlightInBag = tomatoSeeds;
            plant.completeWhen = new PlotStatusCondition { atLeast = PlotScript.PlotStatus.Planted };
            plant.nudge = Say(L("Hold the seeds like you held the spade. Then plant them.", Mood.Thinking));
            plant.success = Say(L("One little seed can grow food for many meals.", Mood.Proud));

            findCan.objective = "Find the watering can";
            findCan.designNote = "Pure test: pick-up was already taught. Hints only if stuck.";
            findCan.targetId = "wateringCan";
            findCan.intro = Say(L("Plants are like us. No water, no life. My watering can is by the house.", Mood.Neutral, GestureType.Point));
            findCan.hints = Hints(("E", "Pick up"));
            findCan.hintTiming = HintTiming.WhenStuck;
            findCan.stuckSeconds = 12f;
            findCan.completeWhen = new HasItemCondition { item = wateringCan };
            findCan.nudge = Say(L("Look near the house for my watering can.", Mood.Neutral, GestureType.Point));

            water.objective = "Water the seeds";
            water.targetId = "@activePlot";
            water.intro = Say(L("Now give the seeds a good drink!", Mood.Happy, GestureType.Point));
            water.hints = Hints(("Tab", "Hold the can"), ("E", "Water"));
            water.hintTiming = HintTiming.WhenStuck;
            water.stuckSeconds = 10f;
            water.highlightInBag = wateringCan;
            water.completeWhen = new PlotStatusCondition { atLeast = PlotScript.PlotStatus.Watered };
            water.nudge = Say(L("Hold the watering can and water the bed.", Mood.Neutral));
            water.success = Say(L("Halala! Our first garden bed! You did that all by yourself.", Mood.Excited));
            water.celebrate = true;

            //ACT 2: time and trouble

            sunset.objective = "Rest in the house until morning";
            sunset.designNote = "Shows the day cycle with a real time-lapse of the group's TimeManager, then teaches the bed.";
            sunset.targetId = "bed";
            sunset.intro = Talk(L("Seeds don't grow in one day. They need time... and a good night's sleep.", Mood.Thinking));
            sunset.afterIntro = Actions(
                new TimeLapseAction { hour = 18, minute = 5, realSeconds = 6f, banner = "The day goes by..." },
                // A slow evening, so nobody misses bedtime (the group's bed only works from 18:00 to midnight).
                new SetClockSpeedAction { gameMinutesPerSecond = 0.1f },
                new GogoSayAction { lines = Say(L("It's getting dark. Let's go inside and rest.", Mood.Neutral, GestureType.Point)), waitUntilFinished = false });
            sunset.hints = Hints(("E", "Rest"));
            sunset.completeWhen = new DayPassedCondition { days = 1, mustBeMorning = true };
            sunset.onComplete = Actions(
                new FadeFlashAction { caption = "The next morning..." },
                new SetClockSpeedAction { gameMinutesPerSecond = 0.5f });
            sunset.nudge = Say(L("The bed is inside the house. Go on, rest.", Mood.Neutral, GestureType.Point));

            weeds.objective = "Pull out the weeds";
            weeds.designNote = "First emergency event. Skipped if the group's 80% weed roll didn't spawn any.";
            weeds.targetId = "@activePlot";
            weeds.skipIf = new NotCondition { condition = new WeedsPresentCondition() };
            weeds.intro = Talk(
                L("Hayi! Weeds grew in the night!", Mood.Worried, GestureType.Point),
                L("Weeds steal the water and food our plant needs. Pull them out!", Mood.Worried));
            weeds.hints = Hints(("E", "Pull weeds"));
            weeds.completeWhen = new WeedsClearedCondition();
            weeds.nudge = Say(L("Walk to the bed and pull those weeds.", Mood.Neutral, GestureType.Point));
            weeds.success = Say(L("Much better. Now our plant can breathe.", Mood.Happy));
            weeds.celebrate = true;

            waterAgain.objective = "Water your plant";
            waterAgain.designNote = "Test: watering runs out every day. No prompts unless stuck.";
            waterAgain.targetId = "@activePlot";
            waterAgain.intro = Say(L("Look, it grew while we slept! But it's thirsty again.", Mood.Excited, GestureType.Point));
            waterAgain.hints = Hints(("Tab", "Hold the can"), ("E", "Water"));
            waterAgain.hintTiming = HintTiming.WhenStuck;
            waterAgain.stuckSeconds = 12f;
            waterAgain.highlightInBag = wateringCan;
            waterAgain.completeWhen = new PlotStatusCondition { atLeast = PlotScript.PlotStatus.Watered };
            waterAgain.nudge = Say(L("What do plants need every single day?", Mood.Thinking));
            waterAgain.success = Say(L("Water every day, like breakfast. You remembered!", Mood.Proud));

            compost.objective = "Feed the soil with compost";
            compost.designNote = "Introduces fertiliser as compost made from scraps (waste becomes food), tying in the littering theme.";
            compost.targetId = "@activePlot";
            compost.onStart = Actions(new GiveItemAction { item = fertiliser, amount = 1, popupHeading = "Gogo gave you" });
            compost.intro = Talk(
                L("This is compost. I made it from our peels and scraps. Nothing goes to waste in this house.", Mood.Proud),
                L("Feed it to the soil and watch what happens!", Mood.Excited));
            compost.hints = Hints(("Tab", "Hold the compost"), ("E", "Feed"));
            compost.hintTiming = HintTiming.WhenStuck;
            compost.stuckSeconds = 8f;
            compost.highlightInBag = fertiliser;
            compost.completeWhen = new PlantFullyGrownCondition();
            compost.nudge = Say(L("Hold the compost and give it to the plant.", Mood.Neutral));
            compost.success = Say(L("Look at those tomatoes! Big and red!", Mood.Excited));
            compost.celebrate = true;

            bird.objective = "Chase the bird away!";
            bird.designNote = "Second emergency event, a real test with a fair timer. Failing is safe: it branches to a short replant.";
            bird.targetId = "@bird";
            // Skip if there's no bird, or the player already harvested (nothing left to defend).
            bird.skipIf = new AnyOfCondition
            {
                conditions = new List<TutorialCondition> { new NoBirdCondition(), new NotCondition { condition = new PlantFullyGrownCondition() } }
            };
            bird.onStart = Actions(new BirdAttackAction { spawnTargetId = "birdSpawn", eatDuration = 7f, speed = 4 });
            bird.intro = Say(L("Hayi! A bird! It wants our tomatoes. Chase it away!", Mood.Worried, GestureType.Shoo));
            bird.hints = Hints(("WASD", "Run at it"), ("Shift", "Sprint"));
            bird.stuckSeconds = 6f;
            // Safety net: if the bird never comes (for example no plant it can see), move on after 35s.
            bird.completeWhen = new AnyOfCondition
            {
                conditions = new List<TutorialCondition> { new BirdScaredCondition(), new TimerCondition { seconds = 35f } }
            };
            bird.failIf = new PlantEatenCondition();
            bird.onFailGoTo = replant;
            bird.onComplete = Actions(new HideBirdAction { delay = 2.5f });
            bird.success = Say(L("Ha! Off you go, bird! Find your own lunch!", Mood.Excited));
            bird.celebrate = true;

            harvest.objective = "Harvest the tomatoes";
            harvest.targetId = "@activePlot";
            harvest.intro = Say(L("They're ready! Pick them gently.", Mood.Happy, GestureType.Point));
            harvest.hints = Hints(("E", "Harvest"));
            harvest.hintTiming = HintTiming.WhenStuck;
            harvest.stuckSeconds = 6f;
            harvest.completeWhen = new HarvestedCondition();
            harvest.success = Say(L("Look at all these tomatoes! Some for supper, and some to share.", Mood.Proud));
            harvest.celebrate = true;

            //ACT 3: sharing and the hook

            sell.objective = "Sell your harvest at Gogo's stall";
            sell.designNote = "Community angle: selling at fair prices to neighbours whose kids go to school hungry. Gogo runs the stall.";
            sell.targetId = "stall";
            sell.gogo = GogoDirective.GoToTarget;
            sell.gogoTargetId = "stallSpot";
            sell.onStart = Actions(new SetTargetActiveAction { targetId = "sellCounter", active = true });
            sell.intro = Talk(
                L("Our neighbours need food too. Many children on our street go to school hungry.", Mood.Sad),
                L("Come to my stall. We'll sell some at a fair price, for everyone.", Mood.Happy, GestureType.Point));
            sell.hints = Hints(("E", "Sell (look at Gogo)"));
            sell.hintTiming = HintTiming.WhenStuck;
            sell.stuckSeconds = 10f;
            sell.completeWhen = new EarnMoneyCondition { amount = 1 };
            sell.nudge = Say(L("Meet me at the stall, my child. Bring the tomatoes.", Mood.Neutral));
            sell.onComplete = Actions(new SetTargetActiveAction { targetId = "sellCounter", active = false });
            sell.success = Say(L("R{earned}! That's bread and milk for the week. Siyabonga!", Mood.Excited));
            sell.celebrate = true;

            buySeeds.objective = "Buy seeds for next time";
            buySeeds.designNote = "Closes the loop: grow, sell, reinvest.";
            buySeeds.targetId = "seeds";
            buySeeds.gogo = GogoDirective.StayPut;
            buySeeds.intro = Say(L("Always keep some money for seeds. Seeds today, food tomorrow.", Mood.Proud, GestureType.Point));
            buySeeds.hints = Hints(("E", "Buy"));
            buySeeds.hintTiming = HintTiming.WhenStuck;
            buySeeds.stuckSeconds = 8f;
            buySeeds.completeWhen = new GainItemCondition { anySeed = true, amount = 1 };
            buySeeds.success = Say(L("Clever! A real farmer always thinks ahead.", Mood.Happy));
            buySeeds.celebrate = true;

            walkHome.objective = "Walk home with Gogo";
            walkHome.designNote = "A calm walk at sunset before the reveal.";
            walkHome.targetId = "porch";
            walkHome.intro = Say(L("Come, walk home with me. I want to show you something special.", Mood.Thinking));
            walkHome.afterIntro = Actions(new TimeLapseAction { hour = 17, minute = 40, realSeconds = 8f, waitUntilDone = false });
            walkHome.completeWhen = new ReachTargetCondition { targetId = "porch", radius = 4f };
            walkHome.hintTiming = HintTiming.Never;

            seedTin.designNote = "The hook: legacy and mystery. Mkhulu's garden once fed the whole street. The note sets up Phase 4.";
            seedTin.gogo = GogoDirective.StayPut;
            seedTin.intro = Talk(
                L("You did so well today, mzukulu. Now I want to show you something.", Mood.Happy),
                L("This old tin belonged to your grandfather. Mkhulu.", Mood.Sad));
            seedTin.afterIntro = Actions(
                new LockPlayerAction { locked = true },
                new StoryCardAction
                {
                    title = "Mkhulu's Seed Tin",
                    body = "An old, rusty tin. Inside are seeds you have never seen before, and a folded piece of paper.",
                    icon = seedPacketIcon,
                    waitForKey = true
                },
                new GogoSayAction
                {
                    lines = Talk(
                        L("Long ago, before the rubbish came, Mkhulu grew a garden that fed our whole street.", Mood.Proud),
                        L("No child went to bed hungry. Not one.", Mood.Proud),
                        L("These are his seeds. You won't find them in any shop.", Mood.Thinking),
                        L("And look... he left a note.", Mood.Thinking))
                },
                new StoryCardAction { title = "Mkhulu's Note", body = "<i>\"Plant them where the water sleeps.\"</i>", waitForKey = true },
                new GogoSayAction
                {
                    lines = Talk(
                        L("I never understood what it meant.", Mood.Worried, GestureType.ShakeHead),
                        L("But you, my clever child... I think you can find out.", Mood.Happy, GestureType.Nod))
                },
                new BannerAction { text = "NEW GOAL: Find Mkhulu's lost garden", seconds = 4f },
                new WaitAction { seconds = 2.5f },
                new RestoreClockSpeedAction(),
                new RestoreBirdAction(),
                new EndTutorialAction { title = "GARDEN GROWERS", subtitle = "To be continued...", nextScene = "MainMenu" });
            seedTin.endsTutorial = true;
            seedTin.hintTiming = HintTiming.Never;

            //Try-again branch (only if the bird eats the tomatoes)

            replant.objective = "Plant again";
            replant.designNote = "Safe failure: Gogo comforts the player and tops up seeds and compost. Teaches resilience.";
            replant.targetId = "@activePlot";
            replant.onStart = Actions(
                new HideBirdAction(),
                new TopUpItemAction { item = tomatoSeeds, atLeast = 1 },
                new TopUpItemAction { item = fertiliser, atLeast = 2 });
            replant.intro = Talk(
                L("Eish... the bird got them. It happens to every farmer.", Mood.Sad, GestureType.ShakeHead),
                L("But a farmer never gives up. Let's plant again. I'll keep watch this time!", Mood.Happy));
            replant.hints = Hints(("E", "Dig"), ("Tab", "Hold the seeds"), ("E", "Plant"));
            replant.hintTiming = HintTiming.WhenStuck;
            replant.stuckSeconds = 10f;
            replant.completeWhen = new PlotStatusCondition { atLeast = PlotScript.PlotStatus.Planted };
            replant.success = Say(L("That's the spirit!", Mood.Proud));

            rewater.objective = "Water the seeds";
            rewater.targetId = "@activePlot";
            rewater.intro = Say(L("Now some water.", Mood.Neutral));
            rewater.hints = Hints(("Tab", "Hold the can"), ("E", "Water"));
            rewater.hintTiming = HintTiming.WhenStuck;
            rewater.stuckSeconds = 8f;
            rewater.completeWhen = new PlotStatusCondition { atLeast = PlotScript.PlotStatus.Watered };

            recompost.objective = "Feed the soil until the tomatoes are ripe";
            recompost.targetId = "@activePlot";
            recompost.intro = Say(L("Use the compost twice. Pull out any weeds in between.", Mood.Thinking));
            recompost.hints = Hints(("E", "Feed"), ("E", "Pull weeds"));
            recompost.completeWhen = new PlantFullyGrownCondition();
            recompost.success = Say(L("Ripe again! See? We never give up.", Mood.Excited));
            recompost.celebrate = true;
            recompost.nextOverride = harvest;

            steps.AddRange(new[]
            {
                welcome, findSpade, holdSpade, dig, plant, findCan, water,
                sunset, weeds, waterAgain, compost, bird, harvest,
                sell, buySeeds, walkHome, seedTin,
                replant, rewater, recompost
            });

            foreach (TutorialStep step in steps) EditorUtility.SetDirty(step);

            TutorialSequence sequence = LoadOrCreate<TutorialSequence>(SequencePath);
            sequence.title = "Gogo's Garden";
            sequence.steps = steps;
            EditorUtility.SetDirty(sequence);
            return sequence;
        }

        //Small helpers

        private static DialogueLine L(string text, Mood mood = Mood.Neutral, GestureType gesture = GestureType.None)
        {
            return new DialogueLine(text, mood, gesture);
        }

        /// <summary>A conversation: camera turns to Gogo, the player presses E between lines.</summary>
        private static DialogueSequence Talk(params DialogueLine[] lines) => DialogueSequence.Conversation(lines);

        /// <summary>A quick comment: lines move on by themselves and the player keeps control.</summary>
        private static DialogueSequence Say(params DialogueLine[] lines) => DialogueSequence.Comment(lines);

        private static List<ControlHint> Hints(params (string key, string action)[] items)
        {
            var list = new List<ControlHint>();
            foreach ((string key, string action) in items) list.Add(new ControlHint(key, action));
            return list;
        }

        private static List<TutorialAction> Actions(params TutorialAction[] actions) => new List<TutorialAction>(actions);

        /// <summary>Load an asset, reset it to defaults, or create it. Keeps the GUID stable.</summary>
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            var fresh = ScriptableObject.CreateInstance<T>();
            if (existing == null)
            {
                AssetDatabase.CreateAsset(fresh, path);
                return fresh;
            }

            string keepName = existing.name;
            EditorUtility.CopySerialized(fresh, existing);
            existing.name = keepName;
            Object.DestroyImmediate(fresh);
            return existing;
        }
    }
}
