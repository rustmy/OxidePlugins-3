# Survey Info
**When a player throws a survey charge If it produces Items it will display them to the player**
**The information from the survey charge is saved in a file and can be displayed again using a GUI**

**The GUI provides many functions including:**
+ Allowing the player to lookup past survey charges
+ Giving the data from the survey charge to another player
+ Allowing the player to remove the survey charge data

*Each survey charge is given a score as a percent. Giving players the ability to easily see how that survey charge ranks.*

### Optional Dependencies:
+ [Gather Manager for Rust](http://oxidemod.org/plugins/gather-manager.675/)
+ ***(If gather manager is detetcted the best possible survey score will be updated based on the config from GatherManager)***

### Chat Commands:
+ **/si** - opens the survey info GUI

###Plugin Config:
```
{
  "Prefix": "[<color=yellow>Survey Info</color>]", //Prefix to use in chat
  "SaveIntervalInSeconds": 600.0, //How often the data file should be saved
  "SurveyIdDisplayLengthInSeconds": 150.0, //How long the Id over the survey charge should be displayed
  "UiColors": { //The colors used by the UI
    "SurveyDataContainer": ".3 .3 .3 .825",
    "GiveToContainer": ".3 .3 .3 .825",
    "Label": ".9 .9 .9 .9",
    "ButtonClose": ".66 .66 .66 .9",
    "ButtonNextPage": ".66 .66 .66 .9",
    "ButtonPrevPage": ".77 .77 .77 .9",
    "ButtonLookup": "0 .8 0 .9",
    "ButtonRemove": ".8 0 0 .9",
    "ButtonGiveTo": ".66 .66 .66 .9",
    "ButtonPlayers": ".66 .66 .66 .9",
    "IdDisplay": "1 0 0 1"
  },
  "SurveyUiConfig": { //UI config for the data UI
    "SurveyDataTextSize": 13,
    "SurveyDataRecordsPerPage": 18,
    "SurveyDataRecordYSpacing": 0.05,
    "SurveyDataContainerMin": ".15 .15",
    "SurveyDataContainerMax": ".6 .9",
    "SurveyDataTopHeadingYMin": 0.96,
    "SurveyDataTopHeadingYMax": 0.99,
    "SurveyDataMiddleHeadingYMin": 0.935,
    "SurveyDataMiddleHeadingYMax": 0.965,
    "SurveyDataBottomHeadingYMin": 0.91,
    "SurveyDataBottomHeadingYMax": 0.94,
    "SurveyDataRecordStartingYMin": 0.87,
    "SurveyDataRecordStartingYMax": 0.91,
    "LabelIdXMin": 0.01,
    "LabelIdXMax": 0.12,
    "LabelStonesXMin": 0.13,
    "LabelStonesXMax": 0.2,
    "LabelMetalOreXMin": 0.21,
    "LabelMetalOreXMax": 0.28,
    "LabelMetalFragXMin": 0.29,
    "LabelMetalFragXMax": 0.36,
    "LabelSulfurOreXMin": 0.37,
    "LabelSulfurOreXMax": 0.44,
    "LabelHqmXMin": 0.45,
    "LabelHqmXMax": 0.52,
    "LabelScoreXMin": 0.53,
    "LabelScoreXMax": 0.6,
    "ButtonLookupXMin": 0.67,
    "ButtonLookupXMax": 0.77,
    "ButtonRemoveXMin": 0.78,
    "ButtonRemoveXMax": 0.88,
    "ButtonGiveToXMin": 0.89,
    "ButtonGiveToXMax": 0.99,
    "ButtonCloseMin": ".89 .94",
    "ButtonCloseMax": ".99 .99",
    "ButtonDataPrevPageMin": ".625 .94",
    "ButtonDataPrevPageMax": ".725 .99",
    "ButtonDataNextPageMin": ".75 .94",
    "ButtonDataNextPageMax": ".85 .99"
  },
  "GiveToUiConfig": { //Config for the give to UI
    "GiveToPlayersTextSize": 10,
    "GiveToPlayerPageTextSize": 12,
    "GiveToPlayersPerRow": 4,
    "GiveToPlayersPerPage": 72,
    "GiveToContainerMin": ".62 .15",
    "GiveToContainerMax": ".92 .9",
    "ButtonGiveToPrevPageMin": ".84 .01",
    "ButtonGiveToPrevPageMax": ".91 .06",
    "ButtonGiveToNextPageMin": ".92 .01",
    "ButtonGiveToNextPageMax": ".99 .06",
    "PlayerButtonStartPositionXMin": 0.01,
    "PlayerButtonStartPositionXMax": 0.24,
    "PlayerButtonStartPositionYMin": 0.95,
    "PlayerButtonStartPositionYMax": 0.99,
    "PlayerXSpacing": 0.25,
    "PlayerButtonYSpacing": 0.05
  },
  "ConfigVersion": "0.0.1", //Version of the config
  "Permission": false //Does the player need permission to use
}
```
