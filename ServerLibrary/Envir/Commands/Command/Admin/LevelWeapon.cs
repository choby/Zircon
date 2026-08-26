using Library;
using Library.Network.ServerPackets;
using Server.DBModels;
using Server.Models;

namespace Server.Envir.Commands.Command.Admin
{
    class LevelWeapon : AbstractCommand<IAdminCommand>
    {
        public override string VALUE => "LEVELWEAPON";

        public override void Action(PlayerObject player)
        {
            UserItem weapon = player.Equipment[(int)EquipmentSlot.Weapon];

            if (weapon == null)
            {
                player.Connection.ReceiveChat("你没有装备武器。", MessageType.System);
                return;
            }

            int maxLevel = Globals.WeaponExperienceList.Count;

            if (weapon.Level >= maxLevel)
            {
                player.Connection.ReceiveChat($"{weapon.Info.ItemName} 已达到最高等级。", MessageType.System);
                return;
            }

            weapon.Level++;
            weapon.Experience = 0;

            if (weapon.Level < maxLevel)
                weapon.Flags |= UserItemFlags.Refinable;

            player.Enqueue(new ItemExperience
            {
                Target = new CellLinkInfo { GridType = GridType.Equipment, Slot = (int)EquipmentSlot.Weapon },
                Experience = weapon.Experience,
                Level = weapon.Level,
                Flags = weapon.Flags,
            });

            string level = weapon.Level == maxLevel ? "Max" : weapon.Level.ToString();
            player.Connection.ReceiveChat($"{weapon.Info.ItemName} 已提升至武器等级 {level}。", MessageType.System);
        }
    }
}
