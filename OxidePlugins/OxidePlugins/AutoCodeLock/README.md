# Auto Code Lock
 **Allows players to have a code lock added to any door they place. This code lock will have the code set by the player. The door will also have the player added to it and the door will be locked.**

### Chat Commands:
+ **/ac** - clears the playes set code lock code
+ **/ac {code}** - sets the players code lock code to the given code. EX: **/ac 1234** - will set the codelocks code to 1234.

###Plugin Config:
```
   {
    "Prefix": "[<color=yellow>Auto CodeLock</color>]", //Prefix appended in chat
    "UseCost": true, //Should we charge players for creating the code lock
    "UseItemCost": true, //Should we chare players items for the code lock
    "ItemCostList": [ //Possible Items the player could have to pay for the code lock
      {
        "lock.code": 1 //Player can have 1 code lock
      },
      { //Or 
        "metal.fragments": 100, //player can have 100 metal fragments 
        "wood": 400 //and 400 wood
      }
    ],
    "Permission": false //Does the play need to have a set permission to use this plugin
  }
```

### Note:
#### **This plugin does not save the code in plain text in the data file so admins cannot directly see your code lock codes**
