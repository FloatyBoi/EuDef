using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;

public class SlashRequire
{
    public class RequireRoleAttribute : SlashCheckBaseAttribute
    {
        public string roleName;

        public RequireRoleAttribute(string roleName)
        {
            this.roleName = roleName;
        }

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            foreach (DiscordRole role in ctx.Member.Roles)
            {
                if (role.Name.ToLower() == roleName.ToLower())
                    return true;
            }
            return false;
        }
    }
}