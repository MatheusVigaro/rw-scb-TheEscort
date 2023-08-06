﻿using System;
using TheEscort;
using UnityEngine.PlayerLoop;
using SlugBase;
using static TheEscort.Eshelp;
using static RWCustom.Custom;
using static TheEscort.EscortTutorial;

namespace TheEscort;

public class EscortRoomScript
{
    public static void Attach()
    {
        On.RoomSpecificScript.AddRoomSpecificScript += Escort_Add_Room_Scripts;
    }

    private static void Escort_Add_Room_Scripts(On.RoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
    {
        orig(room);
        //Ebug("SCRIPTADDER HERE LOL");
        if (room?.game?.session is null) return;
        if (room.game.session is StoryGameSession storyGameSession && Eshelp_IsMe(storyGameSession.saveState.saveStateNumber, false))
        {
            string name = room.abstractRoom.name;
            if (name is null) return;
            if (storyGameSession.saveState.cycleNumber < 2 && (name is "CC_SHAFT02" or "CC_CLOG" or "SU_B07" or "SI_D01" or "SI_D03" or "DM_LEG02" or "GW_TOWER15" or "LF_A10" or "LF_E03" or "CC_A10" or "HR_AP01") && !storyGameSession.saveState.deathPersistentSaveData.Etut(SuperWallFlip))
            {
                Ebug("Get the fucking tutorial bro");
                room.AddObject(new TellPlayerToDoASickFlip(room));
            }
            if (storyGameSession.saveState.cycleNumber == 0 && name is "CC_SUMP02" && storyGameSession.saveState.denPosition is "CC_SUMP02")
            {
                Ebug("Start Escort cutscene!");
                room.AddObject(new DefaultWatchesAPupFall(room));
            }
        }
    }

    private class TellPlayerToDoASickFlip : UpdatableAndDeletable
    {
        private int waitForSpawn = 120;
        public TellPlayerToDoASickFlip(Room room)
        {
            this.room = room;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (room?.game?.session is null) return;
            if (room.game.session is StoryGameSession storyGameSession && !storyGameSession.saveState.deathPersistentSaveData.Etut(SuperWallFlip))
            {
                waitForSpawn--;
                for (int i = 0; i < room.game.Players.Count && (ModManager.CoopAvailable || i == 0); i++)
                {
                    AbstractCreature abstractPlayer = room.game.Players[i];
                    if (abstractPlayer.realizedCreature is Player player && player.room == room)
                    {
                        //Ebug(player, "Player detected!");
                        if (room.abstractRoom.name switch {
                            "CC_SHAFT02" => player.mainBodyChunk.pos.y > 2340 && player.mainBodyChunk.pos.y < 2830,
                            "CC_CLOG" => true,
                            "SU_B07" => player.mainBodyChunk.pos.x > 932 && player.mainBodyChunk.pos.x < 1540,
                            "SI_D01" => player.mainBodyChunk.pos.x > 573 && player.mainBodyChunk.pos.x < 750 && player.mainBodyChunk.pos.y > 733 && player.mainBodyChunk.pos.y < 1062,
                            "SI_D03" => player.mainBodyChunk.pos.x > 3380 && player.mainBodyChunk.pos.x < 3700,
                            "DM_LEG02" => player.mainBodyChunk.pos.x > 113 && player.mainBodyChunk.pos.x < 339 && player.mainBodyChunk.pos.y > 996 && player.mainBodyChunk.pos.y < 1250,
                            "GW_TOWER15" => player.mainBodyChunk.pos.x > 2313 && player.mainBodyChunk.pos.x < 2800 && player.mainBodyChunk.pos.y < 666,
                            "LF_A10" => player.mainBodyChunk.pos.y > 96 && player.mainBodyChunk.pos.y < 167,
                            "LF_E03" => player.mainBodyChunk.pos.x > 3010 && player.mainBodyChunk.pos.x < 4640 && player.mainBodyChunk.pos.y > 120 && player.mainBodyChunk.pos.y < 188,
                            "CC_A10" => player.mainBodyChunk.pos.x > 275 && player.mainBodyChunk.pos.y > 389 && player.mainBodyChunk.pos.x < 311 && player.mainBodyChunk.pos.y < 572 && waitForSpawn <= 0,
                            "HR_AP01" => player.mainBodyChunk.pos.y > 725,
                            _ => false
                        })
                        {
                            Ebug(player, "SHOW TUTORIAL");
                            this.room.game.cameras[0].hud.textPrompt.AddMessage(rainWorld.inGameTranslator.Translate("flippounce_tutorial"), 20, 500, true, true);
                            storyGameSession.saveState.deathPersistentSaveData.Etut(SuperWallFlip, true);
                            //Destroy();
                            break;
                        }
                    }
                }
            }
        }
    }


    private class DefaultEscortSwimsOutOfTheG : UpdatableAndDeletable
    {
        int cutsceneTimer;
        Phase phase;
        bool foodMeterInit;
        StartController startController;
        bool initDone, swimaroundDone, surfaceDone, moveupDone, stareatpupDone, endDone;

        public Player player
        {
            get
            {
                AbstractCreature firstAlivePlayer = this.room.game.FirstAlivePlayer;
                if (this.room.game.Players.Count > 0 && firstAlivePlayer?.realizedCreature is not null)
                {
                    return firstAlivePlayer.realizedCreature as Player;
                }
                return null;
            }
        }

        public class Phase : ExtEnum<DefaultEscortSwimsOutOfTheG.Phase>
        {
            public Phase(string value, bool register = false) : base(value, register)
            {
            }
            public static readonly Phase Init = new("DESOOTGInit", true);
            public static readonly Phase SwimAround = new("DESOOTGSwimAround", true);
            public static readonly Phase Surface = new("DESOOTGSurface", true);
            public static readonly Phase MoveUp = new("DESOOTGMoveUp", true);
            public static readonly Phase StareAtPup = new("DESOOTGStareAtPup", true);
            public static readonly Phase End = new("DESOOTGEnd", true);

        }

        public class StartController : Player.PlayerController
        {
            private readonly DefaultEscortSwimsOutOfTheG owner;
            public StartController(DefaultEscortSwimsOutOfTheG owner)
            {
                this.owner = owner;
            }

            public override Player.InputPackage GetInput()
            {
                return this.owner.GetInput();
            }
        }

        public DefaultEscortSwimsOutOfTheG(Room room)
        {
            this.room = room;
            this.phase = Phase.Init;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.phase == Phase.Init)
            {
                if (!initDone)
                {
                    Ebug("Cutscene init!", 2);
                    initDone = true;
                }
                if (this.player is not null)
                {
                    if (!foodMeterInit && this.room?.game?.cameras[0] is not null)
                    {
                        if (this.room.game.cameras[0].hud is null)
                        {
                            room.game.cameras[0].FireUpSinglePlayerHUD(player);
                        }
                        foodMeterInit = true;
                        this.room.game.cameras[0].hud.foodMeter.NewShowCount(this.player.FoodInStomach);
                        this.room.game.cameras[0].hud.foodMeter.visibleCounter = 0;
                        this.room.game.cameras[0].hud.foodMeter.fade = 0f;
                        this.room.game.cameras[0].hud.foodMeter.lastFade = 0f;
                        this.room.game.cameras[0].followAbstractCreature = this.player.abstractCreature;
                    }
                    this.startController = new StartController(this);
                    this.player.controller = this.startController;
                    this.phase = Phase.SwimAround;
                }
                return;
            }
            else
            {
                if (this.player is null)
                {
                    return;
                }
                cutsceneTimer++;
                if (phase == Phase.SwimAround)
                {
                    if (!swimaroundDone)
                    {
                        Ebug("Cutscene swimaround!", 2);
                        swimaroundDone = true;
                    }
                    if (cutsceneTimer > 560)
                    {
                        if (player.bodyMode == Player.BodyModeIndex.Swimming)
                        {
                            phase = Phase.Surface;
                            cutsceneTimer = 0;
                        }
                        else
                        {
                            phase = Phase.MoveUp;
                            cutsceneTimer = 0;
                        }
                        return;
                    }
                }
                if (phase == Phase.Surface)
                {
                    if (!surfaceDone)
                    {
                        Ebug("Cutscene forcesurface!", 2);
                        surfaceDone = true;
                    }
                    if (cutsceneTimer > 300)
                    {
                        player.mainBodyChunk.pos = new UnityEngine.Vector2(564.785f, 965.6f);
                        player.standing = true;
                        phase = Phase.MoveUp;
                        cutsceneTimer = 0;
                        return;
                    }
                }
                if (phase == Phase.MoveUp)
                {
                    if (!moveupDone)
                    {
                        Ebug("Cutscene Move UP!", 2);
                        moveupDone = true;
                    }
                    if (cutsceneTimer > 320)
                    {
                        phase = Phase.StareAtPup;
                        cutsceneTimer = 0;
                        return;
                    }
                }
                if (phase == Phase.StareAtPup)
                {
                    if (!stareatpupDone)
                    {
                        Ebug("Cutscene stare!", 2);
                        stareatpupDone = true;
                    }
                    // Spawn slugpup to drop down
                    if (cutsceneTimer > 200)
                    {
                        phase = Phase.End;
                        cutsceneTimer = 0;
                        return;
                    }
                }
                if (phase == Phase.End)
                {
                    if (!endDone)
                    {
                        Ebug("Cutscene end!", 2);
                        endDone = true;
                    }
                    if (this.player is not null)
                    {
                        player.controller = null;
                    }
                    Destroy();
                }
            }
        }

        public Player.InputPackage GetInput()
        {
            if (this.player is null)
            {
                return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 0, 0, false, false, false, false, false);
            }
            int x, y;
            x = y = 0;
            bool jmp, pckp, thrw;
            jmp = pckp = thrw = false;
            if (phase == Phase.SwimAround)
            {
                if (cutsceneTimer < 480)
                {
                    x = cutsceneTimer switch
                    {
                        >=129 and <=173 => 1,
                        >=193 and <=194 => 1,
                        >=196 and <=215 => -1,
                        >=232 and <=239 => 1,
                        >=243 and <=251 => -1,
                        >=253 and <=263 => 1,
                        >=264 and <=278 => -1,
                        >=288 and <=309 => 1,
                        >=342 and <=365 => 1,
                        >=394 and <=396 => -1,
                        >=461 and <=467 => -1,
                        _ => 0
                    };
                    y = cutsceneTimer switch
                    {
                        >=112 and <=133 => -1,
                        >=174 and <=324 => 1,
                        >=412 and <=456 => 1,
                        _ => 0
                    };
                }
                else
                {
                    x = player.mainBodyChunk.pos.x switch
                    {
                        >596.5f => -1,
                        <592 => 1,
                        _ => 0
                    };
                    y = 1;
                }
                jmp = cutsceneTimer switch
                {
                    >=525 and <=547 => true,
                    >=560 and <=571 => true,
                    _ => false
                };
            }
            if (phase == Phase.Surface)
            {
                if (player.animation == Player.AnimationIndex.DeepSwim)
                {
                    x = player.mainBodyChunk.pos.x switch
                    {
                        >503 => -1,
                        <505 => 1,
                        _ => 0
                    };
                    y = 1;
                }
                else
                {
                    x = player.mainBodyChunk.pos.x switch
                    {
                        >596.5f => -1,
                        <592 => 1,
                        _ => 0
                    };
                    y = 1;
                    jmp = cutsceneTimer switch
                    {
                        >=270 and <=280 => true,
                        >=290 and <=300 => true,
                        _ => false
                    };
                }
            }
            if (phase == Phase.MoveUp)
            {
                x = cutsceneTimer switch
                {
                    >=9 and <=27 => -1,
                    >=140 and <=164 => 1,
                    >=194 and <=201 => -1,
                    >=301 and <=308 => 1,
                    _ => 0
                };
                y = cutsceneTimer switch
                {
                    >=89 and <=113 => 1,
                    >=177 and <=181 => 1,
                    >=198 and <=203 => 1,
                    >=216 and <=237 => 1,
                    >=244 and <=276 => 1,
                    >=294 and <=301 => 1,
                    _ => 0
                };
                pckp = cutsceneTimer switch
                {
                    >=40 and <=46 => true,
                    >=56 and <=62 => true,
                    >=183 and <=189 => true,
                    _ => false
                };
                jmp = cutsceneTimer switch
                {
                    >=88 and <=102 => true,
                    >=222 and <=234 => true,
                    >=293 and <=303 => true,
                    _ => false
                };
                thrw = cutsceneTimer >= 103 && cutsceneTimer <= 107;
            }


            return new Player.InputPackage(false, Options.ControlSetup.Preset.None, x, y, jmp, thrw, pckp, false, false);
        }
    }


    private class DefaultWatchesAPupFall : UpdatableAndDeletable
    {
        StartController startController;
        int cutsceneTimer;
        Phase phase;
        bool initDone, spawnPupDone, endDone, pupInit, foodMeterInit;

        public Player Playr
        {
            get
            {
                AbstractCreature firstAlivePlayer = this.room.game.FirstAlivePlayer;
                if (this.room.game.Players.Count > 0 && firstAlivePlayer?.realizedCreature is Player p && p.playerState.playerNumber == 0)
                {
                    return p;
                }
                else
                {
                    foreach (AbstractCreature ac in room.game.Players)
                    {
                        if (ac.realizedCreature is Player pl && pl.playerState.playerNumber == 0)
                        {
                            return pl;
                        }
                    }
                }
                return null;
            }
        }

        public class Phase : ExtEnum<Phase>
        {
            public Phase(string value, bool register = false) : base(value, register)
            {
            }
            public static readonly Phase Init = new("DWAPFInit", true);
            public static readonly Phase SpawnPup = new("DWAPFSpawnPup", true);
            public static readonly Phase End = new("DWAPFEnd", true);

        }


        public class StartController : Player.PlayerController
        {
            private readonly DefaultWatchesAPupFall owner;

            public StartController(DefaultWatchesAPupFall owner)
            {
                this.owner = owner;
            }

            public override Player.InputPackage GetInput()
            {
                return new Player.InputPackage(false, Options.ControlSetup.Preset.None, 0, 0, false, false, false, false, false);
            }
        }


        public DefaultWatchesAPupFall(Room room)
        {
            this.room = room;
            this.phase = Phase.Init;
        }


        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.phase == Phase.Init)
            {
                if (!initDone)
                {
                    Ebug("Cutscene init!", 2);
                    initDone = true;
                }
                if (this.Playr is not null)
                {
                    if (Playr.mainBodyChunk.pos.y > 1068)
                    {
                        cutsceneTimer++;
                    }
                    if (cutsceneTimer > 100)
                    {
                        if (this.Playr.playerState.playerNumber != 0)
                        {
                            Ebug("No player 0 Escort found!", 1);
                            Destroy();
                        }
                        if (!foodMeterInit && this.room?.game?.cameras[0] is not null)
                        {
                            if (this.room.game.cameras[0].hud is null)
                            {
                                room.game.cameras[0].FireUpSinglePlayerHUD(Playr);
                            }
                            foodMeterInit = true;
                            this.room.game.cameras[0].hud.foodMeter.NewShowCount(this.Playr.FoodInStomach);
                            this.room.game.cameras[0].hud.foodMeter.visibleCounter = 0;
                            this.room.game.cameras[0].hud.foodMeter.fade = 0f;
                            this.room.game.cameras[0].hud.foodMeter.lastFade = 0f;
                            this.room.game.cameras[0].followAbstractCreature = this.Playr.abstractCreature;
                        }
                        this.startController = new StartController(this);
                        this.Playr.controller = this.startController;
                        this.phase = Phase.SpawnPup;
                        cutsceneTimer = 0;
                        return;
                    }
                }
            }
            if (this.phase == Phase.SpawnPup)
            {
                cutsceneTimer++;
                if (!spawnPupDone)
                {
                    Ebug("Cutscene spawnpup!", 2);
                    spawnPupDone = true;
                }
                if (this.Playr is not null && !pupInit && Plugin.eCon.TryGetValue(Playr, out Escort e))
                {
                    Plugin.SpawnThePup(ref e, room, room.LocalCoordinateOfNode(0), Playr.abstractCreature.ID);
                    e.SocksAliveAndHappy.SuperHardSetPosition(new UnityEngine.Vector2(560, 1970));
                    if (room.game.session is StoryGameSession sgs)
                    {
                        sgs.saveState.miscWorldSaveData.Esave().EscortPupEncountered = true;
                    }
                    Plugin.pupAvailable = true;
                    pupInit = true;
                }
                if (cutsceneTimer > 150)
                {
                    phase = Phase.End;
                    return;
                }
            }
            if (this.phase == Phase.End)
            {
                if (!endDone)
                {
                    Ebug("Cutscene end!", 2);
                    endDone = true;
                }
                if (this.Playr is not null)
                {
                    Playr.controller = null;
                }
                Destroy();
            }
        }

    }
}
