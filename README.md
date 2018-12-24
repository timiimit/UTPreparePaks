# UTPreparePaks
Every time before UT4 starts, start this program and it will prepare PAKs for you so that UT4 and custom match screen both load much faster.

# Install
First install .NET Core from here https://dotnet.microsoft.com/download. without this it wont work.

Download release and unzip it where ever you want. I prefer `%userprofile%\Documents\UnrealTournament\AllPaks`.
Put all your maps from `%userprofile%\Documents\UnrealTournament\Saved\Paks\DownloadedPaks` to appropriate
folder in `%userprofile%\Documents\UnrealTournament\AllPaks`.

For example:

`AS-U-Party-WindowsNoEditor.pak` -> `AllPaks\AS`

`CTF-Bleak-v4-WindowsNoEditor.pak` -> `AllPaks\CTF`

`FR-Ozone-WindowsNoEditor.pak` -> `AllPaks\FR`

Right click on `UTStart.bat` and Edit the file. Uncomment 2nd line (by removing "rem " from the start of line) and set directory where UT4 is installed between the quotes.
Now create a shortcut to UTStart.bat on your desktop or where ever you want and when opened this will launch UTPreparePaks and then UT4 after.

# Will this program waste reads and writes on my hard drive?
Not much because UTPreparePaks won't copy PAKs to wanted location every time. It will only create junctions (links) to where paks are located.