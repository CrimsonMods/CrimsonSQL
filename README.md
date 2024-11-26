# CrimsonSQL
`Server side only` framework plugin for accessing a MySQL Database

This is a framework dependency for other mods to utilize MySQL (and other flavors of MySQL) to sync information between servers or for external tools.

For example; [CrimsonBanned](https://thunderstore.io/c/v-rising/p/skytech6/CrimsonBanned/) uses this dependency to sync bans across different servers. 

## Installation
* Install [BepInEx](https://v-rising.thunderstore.io/package/BepInEx/BepInExPack_V_Rising/)
* Extract _CrimsonSQL.dll_ into _(VRising server folder)/BepInEx/plugins_

I recommend setting your MySQL database to version _8.0.22_ if possible or other MySQL 8 versions. May work with other versions 7 & 9 and may work with other flavors of SQL such as MariaDB, but no gurantees. 

## Config

```
## The name of your MySQL database.
# Setting type: String
# Default value: 
DatabaseName = crimsonbans_db
```
You need to setup your MySQL server with an empty database. The tables will be automatically generated in this database by mods that use CrimsonSQL.

```
## The host address of your MySQL database.
# Setting type: String
# Default value: 
Host = 20.140.81.44
```
The IP Address of your SQL Server.

```
## The port of your database server.
# Setting type: Int32
# Default value: 3306
Port = 3306
```
3306 is the default port of MySQL, but if yours uses another it will need to be supplied here.

```
## The login username for your database.
# Setting type: String
# Default value: 
Username = crimsonbans

## The login password for your database.
# Setting type: String
# Default value: 
Password = zebraApple32%
```
The login credentials for your SQL Server. It doesn't need to be root/admin, but it does need read and write permissions to the specified database.

```
## Some variations of MySQL require additional parameters on the connection string; such as "CharSet=utf8mb4;Convert Zero Datetime=True;Allow Zero Datetime=True;" put those here if needed.
# Setting type: String
# Default value: 
AdditionalParameters = 
```

If you're having issues with connection to the database, it is possible the connection string is missing additional parameters. This is likely to occur when you use different flavors of MySQL such as MariaDB. Consult documentation or your server provider for what parameters you need to add. I will not help with setting up the connection to your version of SQL.

## Verify Install

In your _(VRising server folder)/BepInEx/LogOutput.log_ file you will see either a successful connection message
```
[Info   :   BepInEx] Loading [CrimsonSQL 0.1.10]
[Info   :CrimsonSQL] Connected to MySQL database.
```

Or an error message that prints out information from SQL on what is wrong. Refer to the [SQL Error Documentation](https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html) for the output error code. 

## Integration with Other Mods
This mod was created alongside [CrimsonBanned](https://thunderstore.io/c/v-rising/p/skytech6/CrimsonBanned/), but is in no way only usable with that one mod. Any mod can integrate with CrimsonSQL. Check out the [wiki section for documentation](https://thunderstore.io/c/v-rising/p/skytech6/CrimsonSQL/wiki/) on how to integrate this mod as an optional dependency. 

## Support

Want to support my V Rising Mod development? 

Donations Accepted

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/skytech6)

Or buy/play my games! 

[Train Your Minibot](https://store.steampowered.com/app/713740/Train_Your_Minibot/) 

[Boring Movies](https://store.steampowered.com/app/1792500/Boring_Movies/) **FREE TO PLAY**

**This mod was a paid creation. If you are looking to hire someone to make a mod for any Unity game reach out to me on Discord! (skytech6)**