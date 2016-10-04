# rmatesync
rmate windows client which behave as sync deamon.

## How to use it.

### 1. First, setup rmate client in your server. (I prefer python:) https://github.com/sclukey/rmate-python)

    curl -Lo ~/bin/rmate https://raw.githubusercontent.com/sclukey/rmate-python/master/bin/rmate  
    chmod a+x ~/bin/rmate

### 2. Put build result and ini file to some folder.

ex. C:/Users/yourname/ramtesync/

and place the files

- RMateSync.exe
- LiteDB.dll
- settings.ini


### 3. Edit path of "editor" and "editorargs" in settings.ini.

### 4. Launch RMateSync.exe from above folder (this program use CWD now).

### 5. ssh with -R option

    ssh -R 52698:localhost:52698 user@example.com
    
and call rmate command like

    rmate sometext.txt
    
 This will copy file and launch editor locally.
 Also, if you save local files, RMateSync.exe automatically send "save" command to the server and server file will be updated.
    
