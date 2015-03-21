GrooveCaster
===========
The most advanced broadcasting bot for GrooveShark.  

![Screenshot of the Administration Dashboard](http://i.nofate.me/f5mKBdMZ.png)

----------

Description
--------------
GrooveCaster is a completely standalone, headless, fully automatic and unmanaged broadcasting bot for the music streaming service [GrooveShark](http://grooveshark.com), written in C# and powered by the [SharpShark](https://github.com/OrfeasZ/SharpShark) library.

GrooveCaster will automagically manage your broadcast by never letting it run out of songs, taking care of all the hassle that's needed for maintaining it manually.

Additionally, it provides a variety of management features, either via it's web-based [management interface](#the-admin-dashboard), or in the form of [chat commands](#the-chat-interface) in the broadcast itself, allowing for unattended access by users of your community.

Moreover, GrooveCaster provides an advanced [module system](#the-module-system), which allows users to very easily introduce completely new functionality (or modify existing one) .

Finally, GrooveCaster is completely standalone and headless, which means that you can run it on almost any piece of hardware without the need for a graphical output (even on Linux servers using Mono!). Additionally, an ASP.NET package is provided if you want to run it on IIS or Azure.

> **Note:**  
> While using GrooveCaster you shouldn't be using GrooveShark with the same account that's used for broadcasting, as in doing so you might cause several issues, both for yourself and your listeners.

> **Note:**  
> GrooveCaster is still at an early stage of development, so some features may still be incomplete or not fully functional. If you come across any issues, don't hesitate to let me know by opening a new issue in the [bugtracker](https://github.com/OrfeasZ/GrooveCaster/issues) for this project.

Installation
-------------

For ease-of-use, pre-built binary distributions of GrooveCaster will be available for download on each release.
However, if you want to build GrooveCaster yourself, you can clone this repository locally and build using Visual Studio 2013.

Latest Release: **1.3.0.0**

Windows Binaries: [Download](https://github.com/OrfeasZ/GrooveCaster/releases/download/1.3.0.0/GrooveCaster-1.3.0.0-Win32.zip)  
ASP.NET Package: [Download](https://github.com/OrfeasZ/GrooveCaster/releases/download/1.3.0.0/GrooveCaster-1.3.0.0-AspNet.zip)  
Mono Binaries: [Download](https://github.com/OrfeasZ/GrooveCaster/releases/download/1.3.0.0/GrooveCaster-1.3.0.0-Mono.zip)

There's no installation required.  
Simply download the binaries archive from the links above, and extract them at some location on your drive.

If you wish to upgrade GrooveCaster, simply download the latest archive, delete all previous files (except for `gcaster.sqlite`), and extract the fresh archive in the same directory.

### Windows Environments

#### Standalone
If you want to run GrooveCaster on a Windows environment you first need to make sure you have .NET Framework 4.5 installed. If you don't already have it installed, make sure to grab it from [here](http://www.microsoft.com/en-us/download/details.aspx?id=40779).

Afterwards, running GrooveCaster is as simply as double clicking GrooveCaster.exe and following the instructions on how to perform the [initial setup](#initial-setup).

If you get an error about URI reservations, you can try running GrooveCaster as an administrator, and if that doesn't help you will need to follow the on-screen instructions.

GrooveCaster doesn't currently support being ran as a service, however you could run it in daemon mode by specifying the `-d` parameter:

> \> GrooveCaster.exe -d

After GrooveCaster has started you should be able to access it's web interface at [http://localhost:42278](http://localhost:42278).

#### ASP.NET
GrooveCaster can run under IIS or Windows Azure as an ASP.NET application.
For ease-of-use, a pre-built package is provided.

Here's how you can run GrooveCaster under IIS (version >= 7.5 required with ASP.NET 4.5 enabled).

First of all, you need to extract the GrooveCaster ASP.NET package you downloaded at a folder of your preference.

After you've done that, open the IIS Manager, and add a new website with the name `GrooveCaster` (or anything else you like), and point its content directory to the directory you just extracted the ASP.NET package. Make sure to untick the `Start Website immediately` option.

![Setup IIS website](http://i.nofate.me/N5tK4n3K.png)

After you've done that, you need to configure the application pool for GrooveCaster.

In order to do that, click on the `Application Pools` tab present in the left navigation bar, find the `GrooveCaster` application pool, right click it, select `Advanced Settings`, and change the following options:

 - Enable 32-Bit Applications: **True**
 - Start Mode: **Always Running**
 - Idle Time-out (minutes): **0**

After you've done that, make sure to stop and re-start the application pool.

Finally, go back to the `Sites` tab, right-click the `GrooveCaster` website, go to `Manage Website` and then `Advanced Settings`, and change the following options:

 - Preload Enabled: **True**

You can now start the GrooveCaster website and access it from your web browser.

### Unix Environments
In Unix-based environments, GrooveCaster requires a framework called Mono.

Depending on your platform, you can find instructions on how to install Mono on the project's [website](http://www.mono-project.com/docs/getting-started/install/).  
After you've finished installing Mono, the rest is pretty much the same.

Simply open up your terminal, go to the folder you extracted GrooveCaster in, and launch it using the `-d` parameter (it's required for Linux/OSX environments):

> $ mono GrooveCaster.exe -d

In Unix-based environments you can run GrooveCaster as a service, using the [supervisor](http://supervisord.org/) software.  
The following instructions underline how to install, configure, and use supervisor in an Ubuntu machine.

First, install supervisor by executing the following command in your terminal:

> $ sudo apt-get install supervisor

Then, create a new supervisor configuration file for GrooveCaster:

> $ sudo touch /etc/supervisor/conf.d/groovecaster.conf

Then edit the contents of that configuration file with your favorite text editor:

> [program:groovecaster]  
> command=/usr/bin/mono GrooveCaster.exe -d  
> user=youruser  
> stderr_logfile=/var/log/supervisor/groovecaster.err  
> stdout_logfile=/var/log/supervisor/groovecaster.log  
> directory=/groovecaster/directory/  

Of course, you will need to replace `youruser` with the username of the user you want to run GrooveCaster under, and `/groovecaster/directory/` with the directory where you extracted GrooveCaster.

You should also verify the location of the `mono` executable by running:

> $ whereis mono

If you see `/usr/bin/mono` in there, there's nothing to worry about. Otherwise, simply update the configuration file to reflect the location of the `mono` executable (for instance it could be `/usr/local/bin/mono`).

After you're done with all that start the control manager of supervisor:

> $ sudo supervisorctl

Update the configuration, and launch GrooveCaster:

> $ supervisor\>update  
> $ supervisor\>start groovecaster

After GrooveCaster has started you should be able to access it's web interface at [http://localhost:42278](http://localhost:42278).

### Additional Options
When launching GrooveCaster you can explicitly specify the host the web interface will bind to.

By default, GrooveCaster will bind to http://localhost:42278.

You can change that behavior by specifying the `-b` command-line parameter:

> \> GrooveCaster.exe -b http://localhost:1234

Initial Setup
--------------
After you've successfully installed GrooveCaster you will have to set it up for use.

In order to do that, you first need to login to the admin dashboard by accessing the web interface which, by default, will be available at [http://localhost:42278](http://localhost:42278).

The default credentials are `admin` as a username and `admin` as a password.

After you've successfully logged in, you'll be presented with the setup wizard, where you will be required to setup your GrooveShark account, as well as the details of your broadcast.

Once you've finished the setup wizard you will be redirected to the admin dashboard, from where you can manage the settings of the bot, your song collection, special guests, and additional users with access to the admin dashboard.

It is recommended that the first thing you do is change the password of the admin account.  
To do that, simply click the `Settings` link, located at the top right corner of the dashboard, and provide your current password (`admin`), and your new desired password.

Now that your account is secured you will need to add songs to your collection.  
GrooveCaster will not start broadcasting until there's at least 2 songs in your collection.

For more instructions on how to add songs to your collection see [Song Management](#song-management).

You might also want to add yourself as a special guest via the [Guest Management](#adding-guests) page.

The Admin Dashboard
--------------------------

### Overview
The overview page provides a quick overview of the current status of GrooveCaster and your broadcast.  

This dashboard also provides you with information about the currently playing song, the number of listeners your broadcast had in the past 24 hours, the upcoming songs in your queue, some basic queue management functions, and a chat box which you can use to interact with your listeners.

GrooveCaster currently only keeps track of the last 20 chat messages.

> **Note**  
> Most information information doesn't update automatically currently, so you will have to refresh the page to see changes.

### Queue Management
The queue management page provides detailed information about the queue of your broadcast.

From this page you can really easily manage your queue, by adding or removing songs, moving them in the queue, or directly skipping to them.

Additionally, you are provided with the option to completely empty the queue, by clicking on the `Empty Queue` button.

Moreover, you can load a playlist into your broadcast by clicking on the `Load Playlist` button, which will make GrooveCaster automatically load songs from that playlist (either in their normal order, or shuffled).

If a playlist is already running, you can disable it by clicking on the `Disable` button.

For more information about playlist management, refer to [Playlist Management](#playlist-management).

### Guest Management
The guest management page provides a detailed listing of users with special guest permissions, while also allowing you to edit/delete them, add new guests, and import guests.

#### Adding Guests

To add a new guest, click on the `Add Guest` button, and fill in the required fields in the page you'll be presented with.

You can easily search for a user via the provided search-bar, which will automatically provide suggestions when you start typing a users name. However, if you're not sure you've found the correct user, have them join your broadcast and type `!ping` in chat (while having the ping module enabled). GrooveCaster will respond with a message containing their User ID, which you can then use in this page.

The User Name field is only for personal reference, and doesn't have to match the username of the GrooveShark user.

Clicking on the `Channel Permissions` button you will be presented with a choice of toggle-able special guest permissions that are granted to the user.  

The default selections will give the user suggestion approval/rejection permissions, and the permission to ban/unban users from the broadcast.

#### Importing Guests
For ease of use, GrooveCaster provides the option to import all the users your GrooveShark account follows as special guests.

In order to do that, simply click on the `Import Followed Users` button in the Guest Management page.

### Song Management
The song management page provides a detailed listing of the local collection of songs GrooveCaster will use to automatically fill the queue.

From this page you can also remove songs from the collection, add new songs, and import songs from a GrooveShark collection.

#### Adding Songs
Adding songs is really straight forward.

Simply click on the `Add Song` button present in the Song Management page, and use the search box in order to find the song you'd like to add.

Upon clicking on a song suggestion from the search box the song details fields will be automatically populated.

Do **not** edit those fields manually unless you **really** know what you're doing.

After you've found and selected the song you'd like to add, simply click on the `Add` button to add the song to the collection.

#### Importing Songs
GrooveCaster provides the ability to very easily and automatically import songs from the collection of a GrooveShark user.

To do so, simply click on the `Import from GS Collection` button present in the Song Management page, find the user whose collection you want to import using the search box, and click on the `Import` button.

By default, the User ID field will contain the ID of the current broadcast user (the user you set up GrooveCaster with).

Normally, GrooveCaster only imports songs from the users collection. If you also want to import songs from the users favorites (or only from the favorites), you can use the provided checkboxes to specify your preference.

Collection importing might take some time, so don't worry if you don't see any immediate progress.

### Playlist Management
The playlist management page provides a listing of locally available playlists, which can be used for maintaining more control over the queue.

From this page you can also remove existing playlists, or import playlists from a GrooveShark user.

#### Importing Playlists
GrooveCaster provides the ability to very easily import song playlists from another GrooveShark user.

To do so, simply click on the `Import GS Playlists` button present in the Playlist Management page, find the use whose playlists you want to import using the search box, and then choose the ones you need from the options that will be presented to you.

You can either import all playlists at once, by clicking on the `Import all Playlists` button (present at the bottom of the playlist list), or individually, by clicking the `Import` button next to each playlist.

One thing you should note is that GrooveCaster will automatically add songs that are in imported playlists to your collection (if they don't already exist).

Playlist importing might take some time, so don't worry if you don't see any immediate progress.

### Module Management
The module management page provides a detailed listing of available modules, while also allowing editing, enabling, disabling, or removing them, as well as creating entirely modules.

For more information about modules, refer to the [Module System](#the-module-system).

### Core Settings
The core settings page provides the ability to modify some core settings of GrooveCaster.

Currently available settings are:


| Option                      |  Description | 
| ----------------------      |------------- |
| Broadcast Title             | The title of the Broadcast.                                                                                                                               |
| Broadcast Description       | The description of the Broadcast.                                                                                                                         |
| Max History Songs           | The maximum number of songs to keep in history. This prevents GrooveCaster from playing the same songs. Input `0` to disable.                               |
| Song Vote Threshold         | The maximum number of votes a song has to get in order for GrooveCaster to automatically skip it. Only accepts negative values. Input `0` to disable.       |
| Command Prefix 			  | The prefix used to identify chat commands. Defaults to `!` and can only be a single non-alphanumeric character. |
| Commands without Guest 	  | Allows users to execute chat commands without requiring an active guest status in the broadcast. Defaults to true. |

### User Management
The user management page provides a detailed listing of users with access to the GrooveCaster administration dashboard.

Such users can only access the [Overview](#overview), [Guest Management](#guest-management), and [Song Management](#song-management) pages.

The Chat Interface
----------------------
GrooveCaster provides a really powerful management interface via the default GrooveShark chat.

Using a set of commands, listeners with special guest permissions can remotely manage the broadcast, greatly reducing the hustle of personally maintaining it.

By default, all users who are added as a Special Guest from the admin dashboard, have guesting permissions in the broadcast itself. However, some commands are only available to users with the required flags.

All commands start with the specified command prefix (defaults to `!`).
You can configure command execution from the [Core Settings](#core-settings) page in order to allow only active guests to use commands.

Currently, all commands are implemented in the form of modules you can selectively enable. For more information about available commands refer to [the module system](#the-module-system).

The Module System
-----------------------
GrooveCaster also provides a very powerful module system which allows users to implement custom functionality, modify, or built on-top of existing one.

All GrooveCaster modules are simple Python scripts (using [IronPython](http://ironpython.net/) syntax), and can be directly managed from the Administration Dashboard.

All changes to modules are being propagated immediately, requiring no code recompilation or restarts.

By default, GrooveCaster comes with several toggle-able built-in modules which provide custom commands for the chat interface.

The default commands are the following:

| Command         | Description                          |
|-----------------|--------------------------------------|
| `guest`		| `!guest`: Toggle special guest status. |
| `ping`		| `!ping`: Ping the GrooveCaster server. |
| `removeNext`		| `!removeNext [count]`: Removes the next `[count]` songs from the queue (`[count]` defaults to `1` if not specified). |
| `removeLast`		| `!removeLast [count]`: Removes the last `[count]` songs from the queue (`[count]` defaults to `1` if not specified). |
| `fetchByName`		| `!fetchByName <name>`: Fetches a song from the queue with a name matching `<name>` and moves it after the playing song. |
| `fetchLast`		| `!fetchLast`: Fetches the last song in the queue and moves it after the playing song. |
| `removeByName`		| `!removeByName <name>`: Removes all songs whose name matches `<name>` from the queue. |
| `queueRandom` | `!queueRandom [count]`: Adds `[count]` random songs to the end of the queue (`[count]` defaults to 1 if not specified). |
| `skip`		| `!skip`: Skips the current song. |
| `shuffle`		| `!shuffle`: Shuffles the songs in the queue. |
| `peek`		| `!peek`: Displays a list of upcoming songs from the queue. |
| `makeGuest`		| `!makeGuest <userid>`: Makes user with user ID `<userid>` a temporary special guest. |
| `addGuest`		| `!addGuest <userid>`: Makes user with user ID `<userid>` a permanent special guest. |
| `removeGuest`		| `!removeGuest <userid>`: Permanently removes special guest permissions from user with user ID `<userid>`. |
| `unguest`		| `!unguest [userid]`: Temporarily removes special guest permissions from user with user ID `[userid]`. Unguests everyone if `[userid]` is not specified. |
| `addToCollection`		| `!addToCollection`: Adds the currently playing song to the song collection. |
| `removeFromCollection`		| `!removeFromCollection`: Removes the currently playing song from the song collection. |
| `seek` 		| `!seek <second>`: Seeks to the `<second>` second of the currently playing song. |
| `setTitle`		| `!setTitle <title>`: Sets the title of the broadcast. |
| `setDescription`		| `!setDescription <description>`: Sets the description of the broadcast. |
| `about`		| `!about`: Displays information about the GrooveCaster bot. |
| `help`		| `!help [command]`: Displays detailed information about the command `[command]`. Displays all available commands if `[command]` is not specified. |
| `find`		| `!find <name>`: Finds songs in the queue whose names match `<name>`. |
| `findPlaylist`	| `!findPlaylist <name>`: Finds playlists whose name match `<name>` and displays their IDs. |
| `loadPlaylist`	| `!loadPlaylist <id>`: Loads a playlist with the specified `<id>` into the queue. |
| `disablePlaylist`	| `!disablePlaylist`: Disables the currently active playlist. |
| `queuePlaylist` | `!queuePlaylist <id>`: Queues the playlist with the specified `<id>` after the currently active playlist. |
| `playlist` | `!playlist`: Displays the name of the currently active playlist (if any). |

For more details on how to implement your own modules, sample modules, and documentation on the available APIs, please refer to the [Wiki](https://github.com/OrfeasZ/GrooveCaster/wiki).

Upcoming Features
-----------------------
This is a list of features and fixes that are currently being worked on:

 - More statistics!
 - Automatic service installation on Windows

Contributing
---------------
GrooveCaster is an open-source project, available for everyone to use and modify.

If you want to implement a new feature, modify an ex
isting one, fix a bug, or anything else, feel free to fork this respository and send me a pull request.

Approved contributions will get added to the master branch, shipped in the next binary release, and contributors will be listed below.

Contributors:

 - [Orfeas Zafeirs](https://github.com/OrfeasZ) (author)

