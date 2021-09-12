init()
{
    //gametype started

    // example of shorthand struct initialization
    level.tutorial = 
    {
        #hello: "hello world!",
        #var: "Skipped!",
        #arrayShorthand: [#"hashkey":"value 1", 1:"value 2", 2:"value 3"],
        #arrayVariadic: array("value 1", "value 2", "value 3")
    };
}

onPlayerConnect()
{
    //connected
    self thread waitForNotify();
}

waitForNotify()
{
    self endon(#"disconnect");
    while(true)
    {
        result = self waittill(#"example notify");
        if(!isdefined(result.action)) continue;
        if(result.action == #"killround")
        {
            level.zombie_total = 0;
            foreach(ai in getaiteamarray(level.zombie_team)) ai kill();
            self iprintln(level.tutorial.var);
        }
    }
}

onPlayerSpawned()
{
    // notice how endon now takes variadic parameters
    self endon(#"disconnect", #"spawned_player");
    level endon(#"end_game", #"game_ended");
    self thread InfiniteAmmo();
    self thread ANoclipBind();
    
    while(1)
    {
        if(self adsButtonPressed() && self useButtonPressed())
        {
            self notify(#"example notify", {#action:#"killround"});
            while(self useButtonPressed() || self adsButtonPressed()) waitframe(1);
        }

        if(self.score < 20000) self.score = 20000;
        self freezeControls(false);
        self enableInvulnerability();

        // waits a single frame
        waitframe(1);
    }
}

InfiniteAmmo()
{
    self endon(#"spawned_player", #"disconnect");
    level endon(#"end_game", #"game_ended");    
    while(true)
    {
        weapon  = self GetCurrentWeapon();
        offhand = self GetCurrentOffhand();
        if(!(!isdefined(weapon) || weapon === level.weaponNone || !isdefined(weapon.clipSize) || weapon.clipSize < 1))
        {
            self SetWeaponAmmoClip(weapon, 1337);
            self givemaxammo(weapon);
            self givemaxammo(offhand);
            self gadgetpowerset(2, 100);
            self gadgetpowerset(1, 100);
            self gadgetpowerset(0, 100);
        }
        if(isdefined(offhand) && offhand !== level.weaponNone) self givemaxammo(offhand);

        // waittill now returns a variable
        result = self waittill(#"weapon_fired", #"grenade_fire", #"missile_fire", #"weapon_change", #"melee");
    }
}

ANoclipBind()
{
    self endon(#"spawned_player", #"disconnect", #"bled_out");
    level endon(#"end_game", #"game_ended");
    self notify(#"stop_player_out_of_playable_area_monitor");
	self iprintln("[{+frag}] ^3to ^2Toggle fly mode");
	self unlink();
    if(isdefined(self.originObj)) self.originObj delete();
	while(true)
	{
		if(self fragbuttonpressed())
		{
			self.originObj = spawn("script_origin", self.origin, 1);
    		self.originObj.angles = self.angles;
			self PlayerLinkTo(self.originObj, undefined);
			while(self fragbuttonpressed()) waitframe(1);
            self iprintln("^2Enabled");
            self iprintln("[{+breath_sprint}] to fly");
			self enableweapons();
			while(true)
			{
				if(self fragbuttonpressed()) break;
				if(self SprintButtonPressed())
				{
					normalized = AnglesToForward(self getPlayerAngles());
					scaled = vectorScale(normalized, 60);
					originpos = self.origin + scaled;
					self.originObj.origin = originpos;
				}
				waitframe(1);
			}
			self unlink();
			if(isdefined(self.originObj)) self.originObj delete();
			self iprintln("^1Disabled");
			while(self fragbuttonpressed()) waitframe(1);
		}
		waitframe(1);
	}
}