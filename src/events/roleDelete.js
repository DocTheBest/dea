const db = require('../database');

module.exports = (client) => {
  client.on('roleDelete', async (role) => {
    const dbGuild = await db.guildRepo.getGuild(role.guild.id);

    if (dbGuild.roles.rank.some((v) =>  v.id === role.id)) {
      return db.guildRepo.upsertGuild(role.guild.id, new db.updates.Pull('roles.rank', { id: role.id }));
    }

    if (dbGuild.roles.mod.some((v) =>  v.id === role.id)) {
      return db.guildRepo.upsertGuild(role.guild.id, new db.updates.Pull('roles.mod', { id: role.id }));
    }
  });
};
