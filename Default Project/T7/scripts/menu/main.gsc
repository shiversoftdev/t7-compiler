init()
{
}

on_player_connect()
{
}

on_player_spawned()
{
	self endon("spawned_player");

    while(true)
    {
        self enableInvulnerability();
        self iPrintLnBold("Injected! Compiler by serious");
        wait 1;
    }
}