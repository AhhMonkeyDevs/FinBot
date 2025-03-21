import datetime
from typing import Optional

import discord
from discord.ext import commands

from Data import config
from main import FinBot


def is_staff_backend(member):
    return (member.guild_permissions.administrator or member.guild_permissions.manage_guild or
            member.guild_permissions.manage_roles or member.guild_permissions.manage_channels or member.id in
            config.dev_uids)


class Audit(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot: FinBot = bot

    @commands.command(pass_context=True)
    async def audit(self, ctx, command, member: Optional[discord.Member], channel: Optional[discord.TextChannel], *,
                    other_info=""):
        """
        Gets data from the audit log.
        :param ctx: The context.
        :param command: The type of entry.
        :param member: The member to get the entry for.
        :param channel: The channel to get the entry for.
        :param other_info: Any other parameters parsed in.
        """
        if not is_staff_backend(ctx.author):
            raise commands.CheckFailure
        if command.lower() == "roles":
            await self.audit_roles(ctx, member)
            return
        elif command.lower() == "overwrites":
            await self.audit_overwrites(ctx, channel)
        elif other_info == "something":
            # TODO: Write rest of audit commands...
            pass

    async def audit_overwrites(self, ctx, channel: Optional[discord.TextChannel]):
        """
        Checks audit log for channel overwrites.
        :param ctx: Context for the command.
        :param channel: The channel to check overwrites too.
        """
        if channel is None:
            await ctx.reply(self.bot.create_error_embed("No channel mentioned!"))
            return
        sent_message = await ctx.reply("Searching... check this message for updates when completed.")
        embed = await self.create_channel_updates_embed(channel, ctx.author)
        await sent_message.edit(content=None, embed=embed)
        await sent_message.add_reaction("⏩")

    @staticmethod
    async def create_channel_updates_embed(channel: discord.TextChannel, author: discord.Member):
        """
        Gets audit log data about channel updates.
        :param channel: The channel to get the audit log data on.
        :param author: The author who initiated the command.
        """
        embed = discord.Embed(timestamp=datetime.datetime.utcnow())
        embed.colour = discord.Colour.blue()
        embed.title = "Channel updates for {} - {}".format(channel.id, channel.name)
        embed.set_author(name=author.id)

    async def audit_roles(self, ctx, member: Optional[discord.Member]):
        """
        Gets role history information for a user.
        :param ctx: Context for the command.
        :param member: The member to get the data on.
        """
        if member is None:
            await ctx.reply(self.bot.create_error_embed("No member mentioned!"))
            return

        sent_message = await ctx.reply("Searching... check this message for updates when completed.")
        # noinspection PyTypeChecker
        embed = await self.create_role_changes_embed(member)
        embed.set_author(name=ctx.message.author.id)
        await sent_message.edit(content=None, embed=embed)
        await sent_message.add_reaction("⏩")

    async def create_role_changes_embed(self, member, before=None, start_index=0, after=None):
        """
        Gets the role updates for the user from `audit_roles`
        :param member: The member to get the role updates for.
        :param before: Gets the data before a certain date.
        :param start_index: The starting index.
        :param after: Gets audit data after a certain date.
        """
        embed = discord.Embed(timestamp=datetime.datetime.utcnow())
        embed.colour = discord.Colour.blue()
        embed.title = "Role changes for {} - {}".format(member.id, member.name)
        role_changes, last_time, first_time = await self.get_role_updates(member, before=before, after=after)
        role_changes.sort(key=lambda x: x.split("\n")[1], reverse=True)

        for i in range(len(role_changes)):
            update_string, human_time = role_changes[i].split("\n")
            name = "{}. {} - {}".format(start_index + i + 1, update_string.split(" ")[0], human_time)
            embed.add_field(name=name,
                            value=update_string,
                            inline=False)

        if len(role_changes) == 0:
            embed.description = "Nothing was found."
        if last_time is None:
            last_time_string = None
        else:
            last_time_string = str(last_time.timestamp())
        if first_time is None:
            first_time_string = None
        else:
            first_time_string = str(first_time.timestamp())

        embed.set_footer(text="{}\n{}".format(first_time_string, last_time_string))
        return embed

    @staticmethod
    async def get_role_updates(member: discord.Member, before=None, after=None):
        """
        :param member:
        :param before:
        :param after:
        :return:
        """
        guild = member.guild
        action = discord.AuditLogAction.member_role_update
        entries = []

        if before is None and after is None:
            audit_search = guild.audit_logs(action=action, limit=None)
        elif after is None:
            audit_search = guild.audit_logs(action=action, limit=None, before=before)
        elif before is None:
            audit_search = guild.audit_logs(action=action, limit=None, after=after)
        else:
            return [], None

        last_time = None
        first_time = None

        async for audit_log_entry in audit_search:
            if audit_log_entry.target == member:
                before_roles = audit_log_entry.changes.before.roles
                after_roles = audit_log_entry.changes.after.roles
                taken_roles = set(before_roles) - set(after_roles)
                added_roles = set(after_roles) - set(before_roles)
                human_date = audit_log_entry.created_at.strftime("%Y/%m/%d %H:%M:%S")

                if len(taken_roles) > 0 and len(added_roles) > 0:
                    update_text = "{} took {} and added {}\n{}".format(audit_log_entry.user.name,
                                                                       ', '.join([role.name for role in taken_roles]),
                                                                       ', '.join([role.name for role in added_roles]),
                                                                       human_date)
                elif len(taken_roles) > 0:
                    update_text = "{} took {}.\n{}".format(audit_log_entry.user.name,
                                                           ', '.join([role.name for role in taken_roles]),
                                                           human_date)
                elif len(added_roles) > 0:
                    update_text = "{} added {}.\n{}".format(audit_log_entry.user.name,
                                                            ', '.join([role.name for role in added_roles]),
                                                            human_date)
                else:
                    continue

                entries.append(update_text)
                last_time = audit_log_entry.created_at

                if first_time is None:
                    first_time = audit_log_entry.created_at
                if len(entries) == 10:
                    break

        return entries, last_time, first_time

    @commands.Cog.listener()
    async def on_reaction_add(self, reaction, user):
        if user == self.bot.user or reaction.message.author != self.bot.user:
            return

        message = reaction.message
        message_embeds = message.embeds

        if len(message_embeds) == 0:
            return

        embed = message_embeds[0]

        if embed.title is None:
            return

        if "Role changes" in embed.title:
            if user.id != int(embed.author.name):
                await reaction.remove(user)
                return

            if reaction.emoji == config.fast_forward_emoji:
                if embed.footer is discord.Embed.Empty:
                    return

                time_object = datetime.datetime.fromtimestamp(float(embed.footer.text.split("\n")[1]))
                last_num = int(embed.fields[-1].name.split(".")[0])
                target_id = int(embed.title.split(" ")[3])
                member = message.guild.get_member(target_id)
                new_embed = await self.create_role_changes_embed(member, before=time_object, start_index=last_num)
                new_embed.set_author(name=user.id)
                add_forward = True

                if new_embed.description is not discord.Embed.Empty and "Nothing was found." in new_embed.description:
                    new_embed.add_field(name="{}. None".format(last_num + 1), value="Nothing to see here.")
                    new_embed.set_footer(text="{}\nNone".format(
                        (time_object + datetime.timedelta(seconds=1)).timestamp()))
                    add_forward = False

                await message.edit(content=None, embed=new_embed)
                await reaction.remove(user)

                if config.rewind_emoji not in message.reactions:
                    await message.add_reaction(config.rewind_emoji)

                    if add_forward:
                        await message.remove_reaction(config.fast_forward_emoji, self.bot.user)
                        await message.add_reaction(config.fast_forward_emoji)

                if not add_forward:
                    await message.remove_reaction(config.fast_forward_emoji, self.bot.user)

            elif reaction.emoji == config.rewind_emoji:
                embed_fields = embed.fields

                if len(embed_fields) == 0:
                    return

                if embed.footer is discord.Embed.Empty:
                    return

                time_object = datetime.datetime.fromtimestamp(float(embed.footer.text.split("\n")[0]))
                last_num = int(embed_fields[0].name.partition(".")[0])
                add_back = True

                if last_num == 1:
                    return

                if last_num == 11:
                    add_back = False

                target_id = int(embed.title.split(" ")[3])
                member = message.guild.get_member(target_id)
                new_embed = await self.create_role_changes_embed(member, after=time_object, start_index=last_num - 11)
                new_embed.set_author(name=user.id)
                await message.edit(content=None, embed=new_embed)
                await reaction.remove(user)

                if not add_back:
                    await reaction.remove(self.bot.user)


def setup(bot):
    bot.add_cog(Audit(bot))
