# SixNimmtBot
Six Nimmt Bot for Telegram using C#

[![Build Status](https://rmsprodscussu1.vsrm.visualstudio.com/A8fc85661-2b89-43ad-bd3c-9a5d8da7d34c/_apis/public/Release/badge/b8752e7a-9c6a-4fdc-822b-495b2d264ee0/1/1)](https://github.com/jeffffc/SixNimmtBot)
[![Build status](https://ci.appveyor.com/api/projects/status/4chp9oc2meoscgl4?svg=true)](https://ci.appveyor.com/project/jeffffc/sixnimmtbot)

## Requirements
- .Net Framework 4.5.2
- SQL Server
- Windows Server (Or you can run it locally)
- Visual Studio 2017

## Preparation
1. Create a bot and get the bot token from [`@botfather`](https://t.me/botfather) on telegram.
2. Create a SQL database using `SixNimmtBot.sql` _(to be updated)_, name it `SixNimmt`.
3. Open the Registry Editor (`regedit.exe`), find `HKEY_LOCAL_MACHINE\SOFTWARE`, then create a new `Key` under that folder and name it `SixNimmtBot`. Within the key, create 2 `String Value`s:

    |Key Name | Key Value |
    |---------|-----------|
    |`BotToken`|(Paste your token here, e.g.: `123456789:botABCDEFGHIJKLMNOPQRSTUVWXYZ`|
    |`DbConnectionString`|(Paste your SQL Connection String here, example see below)|

    - SQL Connection String
        - Example: `metadata=res://*/SixNimmtModel.csdl|res://*/SixNimmtModel.ssdl|res://*/SixNimmtModel.msl;provider=System.Data.SqlClient;provider connection string="data source=LOCALHOST,1433;initial catalog=sixnimmt;user id=LOGINNAME;password=LOGINPASSWORD;MultipleActiveResultSets=True;App=EntityFramework"`
        - Of course replace the server address (`LOCALHOST`), login username (`LOGINNAME`) and login password (`LOGINPASSWORD`)
4. Open `Constants.cs`, update the values for `Dev`, and `LogGroup`.
    - For `Dev`, it is an array of Telegram's User Id. This is different from your `username` like `@jeffffc`. You can easily obtain an user's ID by forwarding a message to [`@userinfobot`](https://t.me/userinfobot).
    - For `LogGroupId`, it is the Chat ID of your desinated Log Group. You can get the ID by using command `/chatid` after running your bot for the first time and adding it into the group.
5. Set the project to `DEBUG` mode and click `Start` at the top, a console window will pop up, the title of the console should show you your bot's name, ID and username.
6. If everything is fine, you can fire the build using `RELEASE` build and then go to the build folder for the `SixNimmtBot.exe`.
7. There you go!

## License
This project is using GPLv3 license. You are free to fork and amend the project but you have to keep reference to this initial project and stay open sourced.
Details can be found [here](https://github.com/jeffffc/SixNimmtBot/blob/master/LICENSE).
