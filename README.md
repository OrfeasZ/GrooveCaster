GrooveCaster
===========
The most advanced broadcasting bot for GrooveShark.

----------

Description
--------------
GrooveCaster is a completely standalone, headless, fully automatic and unmanaged broadcasting bot for the music streaming service [GrooveShark](http://grooveshark.com), written in C# and powered by the [SharpShark](https://github.com/OrfeasZ/SharpShark) library.

GrooveCaster will automagically manage your broadcast by never letting it run out of songs, taking care of all the hassle that's needed for maintaining it manually.

Additionally, it provides a variety of management features, either via it's web-based management interface, or in the form of chat commands in the broadcast itself, allowing for unattended access by users of your community.

Moreover, GrooveCaster is completely standalone and headless, which means that you can run it on almost any piece of hardware without the need for a graphical output (even on Linux servers using Mono!).

> **Note:**  
> While using GrooveCaster you shouldn't be using GrooveShark with the same account that's used for broadcasting, as in doing so you might cause several issues, both for yourself and your listeners.

> **Note:**  
> GrooveCaster is still at an early stage of development, so some features may still be incomplete or not fully functional. If you come across any issues, don't hesitate to let me know by opening a new issue in the [bugtracker](https://github.com/OrfeasZ/GrooveCaster/issues) for this project.

Installation
-------------

For ease-of-use, pre-built binary distributions of GrooveCaster will be available for download on each release.
However, if you want to build GrooveCaster yourself, you can clone this repository locally and build using Visual Studio 2013.

Latest Release: **1.0.0.0**

Windows Binaries: [Download](https://github.com/OrfeasZ/GrooveCaster/releases/download/1.0.0.0/GrooveCaster-1.0.0.0-Win32.zip)
Mono Binaries: [Download](https://github.com/OrfeasZ/GrooveCaster/releases/download/1.0.0.0/GrooveCaster-1.0.0.0-Mono.zip)

There's no installation required.  
Simply download the binaries archive from the links above, and extract them at some location on your drive.

### Windows Environments
If you want to run GrooveCaster on a Windows environment you first need to make sure you have .NET Framework 4.5 installed. If you don't already have it installed, make sure to grab it from [here](http://www.microsoft.com/en-us/download/details.aspx?id=40779).

Afterwards, running GrooveCaster is as simply as double clicking GrooveCaster.exe and following the instructions on how to perform the [initial setup](#initial-setup).

If you get an error about URI reservations, you can try running GrooveCaster as an administrator, and if that doesn't help you will need to follow the on-screen instructions.

GrooveCaster doesn't currently support being ran as a service, however you could run it in daemon mode by specifying the `-d` parameter:

> \> GrooveCaster.exe -d

After GrooveCaster has started you should be able to access it's web interface at [http://localhost:42278](http://localhost:42278).

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

After you've successfully logged in, you'll be presented with the setup wizard, where you will be required to setup your GrooveShark account, as long as the details of your broadcast.

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
The overview page provides a quick overview of the current status of GrooveCaster.  
Currently, there's not much information except for the status of the current broadcast.

This information doesn't update automatically currently, so you will have to refresh the page to see changes.

### Guest Management
The guest management page provides a detailed listing of users with special guest permissions, while also allowing you to edit/delete them, add new guests, and import guests.

The process of adding guests via the admin dashboard is currently relatively manual, but will be improved in the future.

#### Adding Guests

To add a new guest, click on the `Add Guest` button, and fill in the required fields in the page you'll be presented with.

For now, the easiest way of getting the User ID of a user is by having them join your broadcast and type `!ping` in chat. GrooveCaster will respond with a message containing their User ID.

The User Name field is only for personal reference, and doesn't have to match the username of the GrooveShark user.

The Channel Permissions field outlines the special guest permissions that are granted to the user.  
For now, you should leave this to the default value (`6`).

This will give the user suggestion approval/rejection permissions, and the permission to ban/unban users from the broadcast.

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

Collection importing might take some time, so don't worry if you don't see any immediate progress.

### Core Settings
The core settings page provides the ability to modify some core settings of GrooveCaster.

Currently available settings are:


| Option                      |  Description | 
| ----------------------      |------------- |
| Broadcast Title             | The title of the Broadcast.                                                                                                                               |
| Broadcast Description       | The description of the Broadcast.                                                                                                                         |
| Max History Songs           | The maximum number of songs to keep in history. This prevents GrooveCaster from playing the same songs. Input 0 to disable.                               |
| Song Vote Threshold         | The maximum number of votes a song has to get in order for GrooveCaster to automatically skip it. Only accepts negative values. Input 0 to disable.       |

### User Management
The user management page provides a detailed listing of users with access to the GrooveCaster administration dashboard.

Such users can only access the [Overview](#overview), [Guest Management](#guest-management), and [Song Management](#song-management) pages.

The Chat Interface
----------------------
GrooveCaster provides a really powerful management interface via the default GrooveShark chat.

Using a set of commands, listeners with special guest permissions can remotely manage the broadcast, greatly reducing the hustle of personally maintaining it.

By default, all users who are added as a Special Guest from the admin dashboard, have guesting permissions in the broadcast itself. However, some commands are only available to users with the required flags.

Currently, all commands start with an exclamation mark (`!`) and can be used regardless if the user invoking them is currently a special guest in the broadcast or not.

In the future, the ability to provide custom aliases for commands will be implemented.

The following commands are currently available:

| Command         | Description                          |
|-----------------|--------------------------------------|
| `guest`		| `!guest`: Toggle special guest status. |
| `ping`		| `!ping`: Ping the GrooveCaster server. |
| `removeNext`		| `!removeNext [count]`: Removes the next `[count]` songs from the queue (`[count]` defaults to `1` if not specified). |
| `removeLast`		| `!removeLast [count]`: Removes the last `[count]` songs from the queue (`[count]` defaults to `1` if not specified). |
| `fetchByName`		| `!fetchByName <name>`: Fetches a song from the queue with a name matching `<name>` and moves it after the playing song. |
| `fetchLast`		| `!fetchLast`: Fetches the last song in the queue and moves it after the playing song. |
| `removeByName`		| `!removeByName <name>`: Removes all songs whose name matches `<name>` from the queue. |
| `skip`		| `!skip`: Skips the current song. |
| `shuffle`		| `!shuffle`: Shuffles the songs in the queue. |
| `peek`		| `!peek`: Displays a list of upcoming songs from the queue. |
| `makeGuest`		| `!makeGuest <userid>`: Makes user with user ID `<userid>` a temporary special guest. |
| `addGuest`		| `!addGuest <userid>`: Makes user with user ID `<userid>` a permanent special guest. |
| `removeGuest`		| `!removeGuest <userid>`: Permanently removes special guest permissions from user with user ID `<userid>`. |
| `unguest`		| `!unguest [userid]`: Temporarily removes special guest permissions from user with user ID `[userid]`. Unguests everyone if `[userid]` is not specified. |
| `addToCollection`		| `!addToCollection`: Adds the currently playing song to the song collection. |
| `removeFromCollection`		| `!removeFromCollection`: Removes the currently playing song from the song collection. |
| `setTitle`		| `!setTitle <title>`: Sets the title of the broadcast. |
| `setDescription`		| `!setDescription <description>`: Sets the description of the broadcast. |
| `about`		| `!about`: Displays information about the GrooveCaster bot. |
| `help`		| `!help [command]`: Displays detailed information about the command `[command]`. Displays all available commands if `[command]` is not specified. |

Upcoming Features
-----------------------
This is a list of features and fixes that are currently being worked on:

 - Graceful Broadcast resuming on bot restart
 - Better management interface for Guest permissions
 - More user-friendly interface for Guest addition
 - Custom aliases for chat commands
 - Broadcast statistics in dashboard
 - Dynamic status updates in Broadcast description/title
 - Automatic service installation on Windows
 - More advanced logging

Contributing
---------------
GrooveCaster is an open-source project, available for everyone to use and modify.

If you want to implement a new feature, modify an existing one, fix a bug, or anything else, feel free to fork this respository and send me a pull request.

Approved contributions will get added to the master branch, shipped in the next binary release, and contributors will be listed below.

Contributors:

 - [Orfeas Zafeirs](https://github.com/OrfeasZ) (author)

