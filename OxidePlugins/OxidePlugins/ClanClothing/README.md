# Clan Clothing
#### Allows a clans owner to set their clothing for the clan. The clan member can then claim their clan clothing.
#### The clothing item and skin are saved for every clan can save their unique look.
#### The cost of the clan clothing can be set in the config file.

### Dependencys:
#### Required: *(Only one clans plugin is required)*
+ [Rust:IO Clans for Rust](http://oxidemod.org/plugins/rust-io-clans.842/)
+ [Universal Clans](http://oxidemod.org/plugins/clans.2087/)

#### Optional:
+ [ServerRewards for Rust](http://oxidemod.org/plugins/serverrewards.1751/)
+ [Economics for Rust](http://oxidemod.org/plugins/economics.717/)

### Chat Commands:
+ **/cc_check** - Allows players in a clan to check if they can afford the clan clothing
+ **/cc_view** - Shows the player what their clan clothing is and what skin is attached the each clothing item
+ **/cc_claim** - The player claims the Clan Clothing and pays
+ **/cc_add** - *(Clan Owner Only)* - Sets the clans clothing to what the player is wearing
+ **/cc_remove** - *(Clan Owner Only)* - Removes the clans clothing

###Plugin Config:
```
   {
  "ExcludedItems": [ //Items that should not be allowed to be part of the clans clothing *(Uses item shortnames)*
    "metal.facemask",
    "metal.plate.torso",
    "roadsign.jacket",
    "roadsign.kilt"
  ],
  "Prefix": "[<color=yellow>Clan Clothing</color>]", //Prefix in chat for the plugin
  "UsePermissions": false, //If the player need to have permission to use the commands
  "WipeDataOnMapWipe": true, //Should the Clan Clothing data wipe on map wipe
  "UseCost": false, //If the player needs to pay for clan clothing
  "UseServerRewards": false, //If the player needs to pay using ServerRewards
  "ServerRewardsCost": 0, //How many Server Rewards Points it cost to claim
  "UseItems": false, //If the Player needs to pay using Items
  "ItemCostList": { //What items the player needs to have to pay for the clan clothing
    "wood": 100,
    "stones": 50,
    "metal.fragments": 25
  },
  "UseEconomics": false, //If the player needs to pay using economics
  "EconomicsCost": 0.0, //How much Economics money it costs to but the clan clothing
  "Commands": { //Change the default chat commands for the plugin
    "CheckCost": "cc",
    "ViewClanClothing": "cc_view",
    "ClaimCommand": "cc_claim",
    "AddCommand": "cc_add",
    "RemoveCommand": "cc_remove"
  }
}
```
