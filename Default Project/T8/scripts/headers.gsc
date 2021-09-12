#include scripts\core_common\struct;
#include scripts\core_common\callbacks_shared;
#include scripts\core_common\clientfield_shared;
#include scripts\core_common\math_shared;
#include scripts\core_common\system_shared;
#include scripts\core_common\util_shared;
#include scripts\core_common\hud_util_shared;
#include scripts\core_common\hud_message_shared;
#include scripts\core_common\hud_shared;
#include scripts\core_common\array_shared;
#include scripts\core_common\flag_shared;

#namespace clientids_shared;

//required
autoexec __init__sytem__()
{
	system::register("clientids_shared", &__init__, undefined, undefined);
}

//required
__init__()
{
    callback::on_start_gametype(&init);
    callback::on_connect(&onPlayerConnect);
    callback::on_spawned(&onPlayerSpawned);
}