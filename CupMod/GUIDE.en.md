# Annihilated Cup v0.5.4

For Windows x64 and Steam Build 26705080. The game folder is the one containing `Annihilated.exe`. In Steam, use **Manage → Browse local files** to find it.

## Install and launch

1. Exit the game and extract the entire `Annihilated-Cup-v0.5.4.zip` archive. Double-click `Install.cmd` or `一键安装.cmd`. Both open the same installer. If it asks for a folder, select the game folder.
2. The installer downloads MelonLoader 0.7.3 and a portable .NET 6 runtime if needed. It installs the Cup mod, Room Retry v1.0.1 for F9 retries, and `Launch-Cup.cmd`.
3. For a cup run, keep Steam open and double-click `Launch-Cup.cmd` in the game folder. On the first launch, wait for the title screen while MelonLoader prepares the game assemblies. Start the game normally for regular play.
4. The Cup uses the language last saved in the base game. It displays Chinese with a Chinese game locale and English with every other locale. To switch, change the language in a normal game session, exit, and launch the Cup again.

If **Cup Setup** does not appear, confirm that you used `Launch-Cup.cmd` and that `Mods/AnnihilatedCup.dll` is in the game folder. You can check `MelonLoader/Latest.log` for the startup error.

## Run a match

1. In **Cup Setup**, enter the organizer's Cup ID, goal, battle mode, seed, time limit, starting floor, and any penalties or optional rules. A time limit of 0 is unlimited. **Random** generates a seed for practice.
2. **Target Ending** offers 23 goals. Select a companion and a specific ending branch, or choose **Prologue → Sacrifice Ritual** for the opening ritual. In **Target Boss**, choose one of 11 targets, then choose a story branch that reaches it. Both dungeon modes start timing in the first section of the chosen floor.
3. Select **Start Cup**. The timer stops when you reach the goal, run out of allowed deaths, or reach the time limit. The save screen, text input, fixed cutscenes, and dialogue or story waits being advanced by the game's skip feature pause the timer. Map choices, party setup, and combat count toward your time.
4. Cup runs use separate save profiles. Normal loading, quick loading, and rollback are disabled during a run. Exit the game and use `Launch-Cup.cmd` again to start another run. The last setup is saved in `UserData/AnnihilatedCup/cup.json`.

The story skip switch is on by default. Both goal modes automatically advance dialogue, cutscenes, and story choices along your selected branch. Turn it off to play dialogue and cutscenes normally and make the story choices yourself. Combat, map tiles, and party management are manual with either setting.

### Starting conditions and penalties

- **Starting Floor** defaults to 1. Each skipped floor adds 10 starting levels to the protagonist and chosen companion. The target determines the latest valid floor.
- **F9 Retry Limit** sets how often you may restart the current fight. Set it to 0 to disable F9. A successful retry counts once and adds the configured penalty. Failed retries do neither.
- **Deaths Allowed** sets the number of formal defeats you may recover from. Each formal defeat adds the configured penalty. Exceeding the allowance ends the run in defeat. Penalties count toward the time limit.
- **Starting Curse** and **Starting Stat Points** are granted before entering the dungeon. Starting stat points trigger a penalty-free opening death to reach the game's stat allocation menu. You can also enable this opening death by itself. The Sacrifice Ritual goal finishes before these options take effect, so use their defaults for that goal.

### Optional Rules

| Rule | Effect |
| --- | --- |
| **Knife Run** | Stealing a usable weapon opens its Drush attack bar. Make one attack. The weapon breaks on a hit or miss, or immediately if the enemy is already dead. |
| **Weapon Limit** | Leave it blank for unlimited weapons. With 0, you keep no weapons. Extra weapons turn into Curse. A weapon stolen with Drush may take the slot of the one it breaks during the same action. |
| **Dreak-Only Weapon Loot** | Keep weapons the protagonist steals from enemies using Dreak. Weapons from other sources turn into Curse. |
| **The training, the punishing missions nearly killed me.** | The protagonist and allies without a weapon-stealing move gain Dreak and can use it from either row. Weapons can be stolen before guard breaks, using the game's usual attack check. |
| **Guaranteed Bonfire Fusion** | Add a guaranteed weapon fusion to the bonfire's existing upgrade choices. |
| **Extra Action After Stealing a Weapon** | A successful steal grants the actor one extra action in the current round. This switch is off by default. |
| **Damage Taken Next Round After Weapon Steal** | The disarmed enemy takes the configured damage multiplier during the party's next round of actions. Defaults to ×2. A repeat steal refreshes the duration. |
| **Enemy Health / Enemy Attack** | Both multipliers default to ×1. The attack multiplier also applies to the move preview and to the predicted loss on the party's HP bars. |
| **Banned Character** | When that character would join a fight, the remaining character fights alone from the front row. The story route stays available. If the soldier is banned, the companion can use the soldier's Annihilate ability when the base game's conditions allow it. |
| **Take the Banned Character's Action** | The lone character also takes the absent ally's turn. The extra action for a successful steal can apply in the same round. |

## Submit and verify results

### Players

Record from the setup screen onward. When the timer stops, select **Export Cup Result** below the HUD. Submit the JSON from `UserData/AnnihilatedCup/exports` and the recording. The eight-character run code is shown in the HUD, filename, and JSON.

### Referees

1. Open `Cup-Referee.html` from the extracted ZIP in a local browser. The upper-right button switches between English and Simplified Chinese.
2. Select **Load Rules JSON** and choose the organizer's `cup.json` or a verified reference result. Review the settings, game build, and Cup version, then select **Apply Tournament Rules**.
3. Drop the submitted result files onto the page or select them in a batch. Choose a run code to review flags, penalties, and events. Match the same code to the recording.
4. Select **Export CSV** under **Provisional Rankings** before closing the page. Clears rank by elapsed time. Other eligible runs rank by furthest progress. Equal scores tie.

Players in the same match should use the same Cup version, game build, and rules. Their interfaces and result files may be in either language. To try the desk with sample data, load `examples/example-ritual.json` as the rules, apply them, then import the same file as a result.

## Uninstall

Exit the game and remove `Mods/AnnihilatedCup.dll` and `Launch-Cup.cmd` from the game folder. Keep `Mods/AnnihilatedRoomRetry.dll` if you still use standalone F9 retries. Cup settings and results are stored in `UserData/AnnihilatedCup`.
