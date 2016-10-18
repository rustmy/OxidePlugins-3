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
+ **/clanclothing check** - Allows players in a clan to check if they can afford the clan clothing
+ **/clanclothing view** - Shows the player what their clan clothing is and what skin is attached the each clothing item
+ **/clanclothing claim** - The player claims the Clan Clothing and pays
+ **/clanclothing add** - *(Clan Owner Only)* - Sets the clans clothing to what the player is wearing
+ **/clanclothing remove** - *(Clan Owner Only)* - Removes the clans clothing
+ **/clanclothing** - Displays the Clan Clothing help text

###Plugin Config:
```
   {
  "ExcludedItems": [
    "metal.facemask",
    "metal.plate.torso",
    "roadsign.jacket",
    "roadsign.kilt"
  ],
  "Prefix": "[<color=yellow>Clan Clothing</color>]",
  "WipeClanClothingOnMapWipe": false,
  "UseCost": false,
  "ServerRewardsCost": 0,
  "EconomicsCost": 0.0,
  "UseItems": false,
  "ItemCostList": {
    "wood": 100,
    "stones": 50,
    "metal.fragments": 25
  },
  "ChatCommand": "clanclothing"
}
```
